using Avalonia;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace ComputerCompanion;

sealed class Program
{
    public const string OverlayModeArg = "--overlay";
    private static string? _logPath;

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

    private static void InitDiagnostics()
    {
        try
        {
            var defaultLogDir = GetDefaultDataPath();
            Directory.CreateDirectory(defaultLogDir);
            _logPath = Path.Combine(defaultLogDir, "runtime.log");

            // 限制日志文件大小
            if (File.Exists(_logPath) && new FileInfo(_logPath).Length > 10 * 1024 * 1024)
            {
                File.Delete(_logPath);
            }
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
                    // 如果旧日志文件存在且新日志文件不存在，复制旧日志
                    if (File.Exists(_logPath) && !File.Exists(newLogPath))
                    {
                        File.Copy(_logPath, newLogPath);
                    }
                    _logPath = newLogPath;
                    Log($"[日志] 日志路径已更新为: {_logPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[日志] 更新日志路径失败: {ex.Message}");
        }
    }

    internal static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            if (_logPath != null)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
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
}
