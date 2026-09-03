namespace ComputerCompanion.Models;

/// <summary>
/// 皮肤/布局预设模型
/// </summary>
public class SkinPreset
{
    public string Name { get; set; } = "default";
    public string DisplayName { get; set; } = "默认";
    public string BackgroundColor { get; set; } = "#1a1a2eee";
    public string TextColor { get; set; } = "#76B900";
    public string AccentColor { get; set; } = "#00db78";
    public string SecondaryColor { get; set; } = "#4ecdc4";
    public int FontSize { get; set; } = 14;
    public double BackgroundOpacity { get; set; } = 0.9;
    public int CornerRadius { get; set; } = 14;
    public string Layout { get; set; } = "standard"; // minimal/standard/complete

    /// <summary>
    /// 内置皮肤预设
    /// </summary>
    public static SkinPreset[] BuiltInPresets => new[]
    {
        new SkinPreset
        {
            Name = "minimal",
            DisplayName = "极简",
            BackgroundColor = "#2b2b2bee",
            TextColor = "#d0d0d0",
            AccentColor = "#a0a0a0",
            SecondaryColor = "#808080",
            FontSize = 12,
            BackgroundOpacity = 0.8,
            CornerRadius = 8,
            Layout = "minimal"
        },
        new SkinPreset
        {
            Name = "tech",
            DisplayName = "科技",
            BackgroundColor = "#0a0a0fee",
            TextColor = "#76B900",
            AccentColor = "#00db78",
            SecondaryColor = "#4ecdc4",
            FontSize = 14,
            BackgroundOpacity = 0.9,
            CornerRadius = 14,
            Layout = "standard"
        },
        new SkinPreset
        {
            Name = "retro",
            DisplayName = "复古",
            BackgroundColor = "#1a1308ee",
            TextColor = "#ffb300",
            AccentColor = "#ff8f00",
            SecondaryColor = "#ffca28",
            FontSize = 15,
            BackgroundOpacity = 0.92,
            CornerRadius = 6,
            Layout = "standard"
        },
        new SkinPreset
        {
            Name = "night",
            DisplayName = "暗夜",
            BackgroundColor = "#0d0019ee",
            TextColor = "#b388ff",
            AccentColor = "#7c4dff",
            SecondaryColor = "#651fff",
            FontSize = 14,
            BackgroundOpacity = 0.94,
            CornerRadius = 16,
            Layout = "complete"
        }
    };
}
