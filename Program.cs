using Avalonia;
using System;
using System.Linq;

namespace ComputerCompanion;

sealed class Program
{
    // 命令行参数常量
    public const string OverlayModeArg = "--overlay";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // 检查是否以悬浮窗模式启动
        App.IsOverlayMode = args.Contains(OverlayModeArg);
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
