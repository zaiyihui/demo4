using Avalonia;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

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
        // 设置控制台编码为 UTF-8（Windows 平台）
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch
        {
            // 忽略编码设置失败，继续执行
        }

        // 设置默认文化为中文（简体）
        try
        {
            var zhCN = new CultureInfo("zh-CN");
            CultureInfo.DefaultThreadCurrentCulture = zhCN;
            CultureInfo.DefaultThreadCurrentUICulture = zhCN;
            CultureInfo.CurrentCulture = zhCN;
            CultureInfo.CurrentUICulture = zhCN;
        }
        catch
        {
            // 忽略文化设置失败，继续执行
        }

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
