using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ComputerCompanion.Overlay;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        EnsureAngleOrFallback();
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// 探测运行目录下是否存在 Avalonia ANGLE 原生依赖(av_libGLESv2.dll / libEGL.dll)。
    /// 若缺失，则通过环境变量让 Avalonia 回退到 Win32 / Direct2D 渲染路径，
    /// 从而避免抛出 "Unable to load DLL 'av_libGLESv2.dll'" 异常。
    /// </summary>
    private static void EnsureAngleOrFallback()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            var baseDir = AppContext.BaseDirectory;
            bool hasAngle = FileExists(Path.Combine(baseDir, "av_libGLESv2.dll"))
                            || FileExists(Path.Combine(baseDir, "libEGL.dll"));

            if (!hasAngle)
            {
                Environment.SetEnvironmentVariable("AVALONIA_GL_RENDERER", "direct2d");
                Environment.SetEnvironmentVariable("AVALONIA_NO_ANGLE", "1");
            }
        }
        catch
        {
            // 兜底：任何探测异常也不影响主流程启动。
        }
    }

    private static bool FileExists(string path)
    {
        try { return File.Exists(path); }
        catch { return false; }
    }
}
