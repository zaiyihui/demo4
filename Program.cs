using Avalonia;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ComputerCompanion.Models;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ComputerCompanion;

sealed class Program
{
    public const string OverlayModeArg = "--overlay";
    private static string? _logPath;
    private static int _logSequence;
    private static readonly object _logLock = new object();

    /// <summary>
    /// 当前进程是否以管理员权限运行
    /// </summary>
    public static bool IsRunningAsAdmin { get; private set; }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle, int tokenInformationClass,
        IntPtr tokenInformation, int tokenInformationLength,
        out int returnLength);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool DuplicateTokenEx(
        IntPtr existingTokenHandle, uint desiredAccess,
        IntPtr tokenAttributes, int impersonationLevel,
        int tokenType, out IntPtr newTokenHandle);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetShellWindow();

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ShellExecuteWithInfo(ref SHELLEXECUTEINFO info);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        public string lpVerb;
        public string lpFile;
        public string lpParameters;
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    private const int TOKEN_QUERY = 0x0008;
    private const int TokenElevation = 20;
    private const uint SEE_MASK_NOCLOSEPROCESS = 0x00000040;

    /// <summary>
    /// 检查当前进程是否具有管理员权限
    /// </summary>
    private static bool CheckAdminPrivilege()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 以管理员身份重启程序
    /// </summary>
    public static void RestartAsAdmin()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            var info = new SHELLEXECUTEINFO
            {
                cbSize = System.Runtime.InteropServices.Marshal.SizeOf<SHELLEXECUTEINFO>(),
                fMask = SEE_MASK_NOCLOSEPROCESS,
                lpVerb = "runas",
                lpFile = exePath,
                lpParameters = string.Empty,
                lpDirectory = Environment.CurrentDirectory,
                nShow = 1 // SW_SHOWNORMAL
            };
            ShellExecuteWithInfo(ref info);
        }
        catch (Exception ex)
        {
            Log($"[权限] 重启为管理员失败: {ex.Message}");
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        InitDiagnostics();

        try
        {
            Log("[启动] 程序已启动");
            Log($"[启动] 运行目录: {AppContext.BaseDirectory}");
            Log($"[启动] 运行时: {RuntimeInformation.FrameworkDescription}");
            Log($"[启动] 平台: {RuntimeInformation.OSDescription}");
            Log($"[启动] 处理器: {Environment.ProcessorCount} 核心");

            var totalMemory = GC.GetTotalMemory(false);
            Log($"[启动] 初始内存: {FormatBytes(totalMemory)}");

            // 管理员权限检测
            IsRunningAsAdmin = CheckAdminPrivilege();
            Log($"[启动] 管理员权限: {IsRunningAsAdmin}");
            if (!IsRunningAsAdmin)
            {
                Log("[启动] 警告: 未以管理员身份运行，传感器数据可能不完整，ETW FPS 监控不可用");
            }

            InitEncoding();
            InitCulture();
            EnsureAngleOrFallback();

            App.IsOverlayMode = Array.Exists(args, a => a == OverlayModeArg);
            Log($"[启动] 悬浮窗模式: {App.IsOverlayMode}");

            Log("[启动] 启动 Avalonia 桌面生命周期");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            Log("[退出] 程序正常退出");
        }
        catch (Exception ex)
        {
            Log($"[致命错误] {ex.GetType().Name}: {ex.Message}");
            Log(ex.StackTrace ?? "无堆栈信息");
            try
            {
                var errorDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ComputerCompanion");
                Directory.CreateDirectory(errorDir);
                File.WriteAllText(
                    Path.Combine(errorDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{ex}\n\n{ex.StackTrace}");
            }
            catch { }
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    static Program()
    {
        LiveCharts.Configure(config =>
            config
                .AddSkiaSharp()
                .HasMap<ChartPoint>((point, index) => new(index, point.Value))
        );
    }

    private static void InitDiagnostics()
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var defaultLogDir = GetDefaultDataPath();
            Directory.CreateDirectory(defaultLogDir);
            _logPath = Path.Combine(defaultLogDir, "runtime.log");

            if (File.Exists(_logPath) && new FileInfo(_logPath).Length > 10 * 1024 * 1024)
            {
                File.Delete(_logPath);
            }
        }
        catch { }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (ex != null)
        {
            Log($"[未处理异常] 严重程度: {e.IsTerminating}");
            Log($"[未处理异常] {ex.GetType().Name}: {ex.Message}");
            Log(ex.StackTrace ?? "无堆栈信息");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            Log($"[任务异常] 异常数量: {e.Exception.InnerExceptions.Count}");
            foreach (var ex in e.Exception.InnerExceptions)
            {
                Log($"[任务异常] {ex.GetType().Name}: {ex.Message}");
            }
            e.SetObserved();
        }
        catch { }
    }

    public static string GetDefaultDataPath()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "ComputerCompanion");
        }
        catch
        {
            return Directory.GetCurrentDirectory();
        }
    }

    public static void UpdateLogPath(string newLogDir)
    {
        try
        {
            if (!string.IsNullOrEmpty(newLogDir))
            {
                Directory.CreateDirectory(newLogDir);
                var newLogPath = Path.Combine(newLogDir, "runtime.log");
                
                if (_logPath != newLogPath)
                {
                    _logPath = newLogPath;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[日志] 更新日志路径失败: {ex.Message}");
        }
    }

    internal static void Log(string message)
    {
        try
        {
            var sequence = Interlocked.Increment(ref _logSequence);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{sequence}] {message}";
            
            lock (_logLock)
            {
                if (_logPath != null)
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
            }
            
            Console.WriteLine(line);
            System.Diagnostics.Debug.WriteLine(line);
        }
        catch { }
    }

    private static void InitEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch (Exception ex)
        {
            Log($"[编码] UTF-8 设置失败: {ex.Message}");
        }
    }

    private static void InitCulture()
    {
        try
        {
            var zhCN = new CultureInfo("zh-CN");
            CultureInfo.DefaultThreadCurrentCulture = zhCN;
            CultureInfo.DefaultThreadCurrentUICulture = zhCN;
            CultureInfo.CurrentCulture = zhCN;
            CultureInfo.CurrentUICulture = zhCN;
        }
        catch (Exception ex)
        {
            Log($"[文化] 中文设置失败: {ex.Message}");
        }
    }

    private static void EnsureAngleOrFallback()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            var baseDir = AppContext.BaseDirectory;
            var files = Directory.GetFiles(baseDir, "*libgles*.dll", SearchOption.TopDirectoryOnly);
            bool hasAngle = files.Length > 0;

            Log($"[渲染] 检测到 ANGLE DLL: {(hasAngle ? string.Join(", ", files) : "未找到")}");

            if (!hasAngle)
            {
                Log("[渲染] ANGLE 缺失，启用 Direct2D 回退渲染");
                Environment.SetEnvironmentVariable("AVALONIA_GL_RENDERER", "direct2d");
                Environment.SetEnvironmentVariable("AVALONIA_NO_ANGLE", "1");
            }
            else
            {
                Log("[渲染] 使用 ANGLE OpenGL 渲染");
            }
        }
        catch (Exception ex)
        {
            Log($"[渲染] 探测失败: {ex.Message}，回退到 Direct2D");
            Environment.SetEnvironmentVariable("AVALONIA_GL_RENDERER", "direct2d");
            Environment.SetEnvironmentVariable("AVALONIA_NO_ANGLE", "1");
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}