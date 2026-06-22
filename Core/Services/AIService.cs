using ComputerCompanion.Core.Abstractions;
using ComputerCompanion.Core.Models;
using ComputerCompanion.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComputerCompanion.Core.Services;

/// <summary>
/// AI服务 - 实现智能建议、硬件分析、异常预测
/// </summary>
public class AIService : ServiceBase, IAIService
{
    private readonly IPerformanceMonitorService _performanceMonitor;
    private readonly IHardwareMonitorService _hardwareMonitor;

    private PrivacyLevel _currentPrivacyLevel = PrivacyLevel.High;
    private readonly List<AISuggestion> _cachedSuggestions = new();
    private DateTime _lastSuggestionUpdate = DateTime.MinValue;

    public PrivacyLevel CurrentPrivacyLevel
    {
        get => _currentPrivacyLevel;
        set => _currentPrivacyLevel = value;
    }

    public event EventHandler<AISuggestion>? SuggestionGenerated;
    public event EventHandler<AnomalyPrediction>? AnomalyPredicted;

    public AIService(
        IPerformanceMonitorService performanceMonitor,
        IHardwareMonitorService hardwareMonitor)
    {
        _performanceMonitor = performanceMonitor;
        _hardwareMonitor = hardwareMonitor;
    }

    public override Task InitializeAsync()
    {
        base.InitializeAsync();
        Program.Log($"[AI] AI服务已初始化，隐私级别: {_currentPrivacyLevel}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取智能建议
    /// </summary>
    public async Task<IEnumerable<AISuggestion>> GetSuggestionsAsync()
    {
        // 每5分钟更新一次建议
        if ((DateTime.UtcNow - _lastSuggestionUpdate).TotalMinutes < 5 && _cachedSuggestions.Count > 0)
        {
            return _cachedSuggestions;
        }

        var suggestions = new List<AISuggestion>();

        try
        {
            // 分析性能数据
            var metrics = _performanceMonitor.CurrentMetrics;

            // 基于CPU使用率建议
            if (metrics.CpuUsagePercent > 80)
            {
                suggestions.Add(new AISuggestion
                {
                    Title = "CPU使用率过高",
                    Description = "您的CPU使用率持续较高。建议关闭不必要的后台程序或降低游戏画质设置。",
                    Category = SuggestionCategory.Performance,
                    Confidence = Math.Min(metrics.CpuUsagePercent / 100, 0.95),
                    ActionId = "optimize_cpu"
                });
            }

            // 基于内存使用率建议
            if (metrics.MemoryUsagePercent > 85)
            {
                suggestions.Add(new AISuggestion
                {
                    Title = "内存使用率较高",
                    Description = "可用内存不足，建议关闭不需要的应用程序或增加物理内存。",
                    Category = SuggestionCategory.Performance,
                    Confidence = Math.Min(metrics.MemoryUsagePercent / 100, 0.9),
                    ActionId = "optimize_memory"
                });
            }

            // 基于GPU温度建议
            if (metrics.GpuTemperature > 80)
            {
                suggestions.Add(new AISuggestion
                {
                    Title = "GPU温度过高",
                    Description = "显卡温度偏高，建议清理散热器灰尘或改善机箱风道。",
                    Category = SuggestionCategory.Maintenance,
                    Confidence = 0.85,
                    ActionId = "check_cooling"
                });
            }

            // 基于FPS建议
            if (metrics.Fps > 0 && metrics.Fps < 60)
            {
                suggestions.Add(new AISuggestion
                {
                    Title = "游戏帧率偏低",
                    Description = $"当前帧率 {metrics.Fps:F0} FPS，建议降低游戏画质或升级显卡。",
                    Category = SuggestionCategory.Performance,
                    Confidence = 0.9,
                    ActionId = "optimize_fps"
                });
            }

            // 基于使用习惯的个性化建议
            suggestions.AddRange(await GetPersonalizedSuggestionsAsync());

            _cachedSuggestions.Clear();
            _cachedSuggestions.AddRange(suggestions);
            _lastSuggestionUpdate = DateTime.UtcNow;

            foreach (var suggestion in suggestions)
            {
                SuggestionGenerated?.Invoke(this, suggestion);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[AI] 生成建议失败: {ex.Message}");
        }

        return suggestions;
    }

    /// <summary>
    /// 获取个性化建议
    /// </summary>
    private async Task<List<AISuggestion>> GetPersonalizedSuggestionsAsync()
    {
        var suggestions = new List<AISuggestion>();

        // 获取历史数据进行分析
        var cpuHistory = _performanceMonitor.GetHistoricalMetrics("CpuUsagePercent", TimeSpan.FromHours(24));
        var memoryHistory = _performanceMonitor.GetHistoricalMetrics("MemoryUsagePercent", TimeSpan.FromHours(24));

        if (cpuHistory.Any())
        {
            var avgCpu = cpuHistory.Average(m => m.Value);
            var peakCpu = cpuHistory.Max(m => m.Value);

            // 如果峰值远高于平均值，可能是间歇性问题
            if (peakCpu > avgCpu * 2 && peakCpu > 70)
            {
                suggestions.Add(new AISuggestion
                {
                    Title = "检测到CPU使用波动",
                    Description = "您的CPU使用存在较大波动，可能是某些应用程序在后台运行。建议检查启动项。",
                    Category = SuggestionCategory.Productivity,
                    Confidence = 0.75,
                    ActionId = "check_startup"
                });
            }
        }

        // 检查是否长时间未进行维护
        suggestions.Add(new AISuggestion
        {
            Title = "建议进行系统维护",
            Description = "定期进行系统维护可以保持电脑良好运行状态。建议清理临时文件并检查磁盘健康。",
            Category = SuggestionCategory.Maintenance,
            Confidence = 0.6,
            ActionId = "maintenance"
        });

        await Task.CompletedTask;
        return suggestions;
    }

    /// <summary>
    /// 分析硬件状态
    /// </summary>
    public async Task<HardwareAnalysis> AnalyzeHardwareAsync()
    {
        var analysis = new HardwareAnalysis();

        try
        {
            var metrics = _performanceMonitor.CurrentMetrics;

            // 分析整体健康状态
            var componentHealths = new List<ComponentHealth>();

            // CPU健康分析
            var cpuHealth = AnalyzeCpuHealth(metrics);
            componentHealths.Add(cpuHealth);

            // GPU健康分析
            var gpuHealth = AnalyzeGpuHealth(metrics);
            componentHealths.Add(gpuHealth);

            // 内存健康分析
            var memoryHealth = AnalyzeMemoryHealth(metrics);
            componentHealths.Add(memoryHealth);

            analysis.Components = componentHealths;

            // 计算整体健康状态
            var avgHealth = componentHealths.Average(c => c.HealthScore);
            analysis.OverallHealth = avgHealth switch
            {
                >= 90 => HealthStatus.Excellent,
                >= 75 => HealthStatus.Good,
                >= 60 => HealthStatus.Fair,
                >= 40 => HealthStatus.Poor,
                _ => HealthStatus.Critical
            };

            // 生成建议
            foreach (var component in componentHealths.Where(c => c.Status != HealthStatus.Excellent))
            {
                foreach (var issue in component.Issues)
                {
                    analysis.Recommendations.Add($"[{component.ComponentName}] {issue}");
                }
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[AI] 硬件分析失败: {ex.Message}");
        }

        await Task.CompletedTask;
        return analysis;
    }

    private ComponentHealth AnalyzeCpuHealth(PerformanceMetrics metrics)
    {
        var health = new ComponentHealth { ComponentName = "CPU" };

        var healthScore = 100.0;

        // 温度影响
        if (metrics.CpuTemperature > 90)
        {
            health.Issues.Add("温度过高，可能需要清理散热器");
            healthScore -= 30;
        }
        else if (metrics.CpuTemperature > 80)
        {
            health.Issues.Add("温度偏高，建议监控");
            healthScore -= 15;
        }

        // 使用率影响
        if (metrics.CpuUsagePercent > 95)
        {
            health.Issues.Add("CPU持续满载，可能影响响应速度");
            healthScore -= 20;
        }
        else if (metrics.CpuUsagePercent > 80)
        {
            health.Issues.Add("CPU使用率较高");
            healthScore -= 10;
        }

        health.HealthScore = Math.Max(0, healthScore);
        health.Status = healthScore >= 90 ? HealthStatus.Excellent :
                        healthScore >= 75 ? HealthStatus.Good :
                        healthScore >= 60 ? HealthStatus.Fair :
                        healthScore >= 40 ? HealthStatus.Poor :
                        HealthStatus.Critical;

        return health;
    }

    private ComponentHealth AnalyzeGpuHealth(PerformanceMetrics metrics)
    {
        var health = new ComponentHealth { ComponentName = "GPU" };

        if (metrics.GpuTemperature == 0)
        {
            health.Status = HealthStatus.Good;
            health.HealthScore = 100;
            health.Issues.Add("未检测到独立显卡或显卡监控不可用");
            return health;
        }

        var healthScore = 100.0;

        // 温度影响
        if (metrics.GpuTemperature > 85)
        {
            health.Issues.Add("GPU温度过高，可能导致降频或重启");
            healthScore -= 35;
        }
        else if (metrics.GpuTemperature > 75)
        {
            health.Issues.Add("GPU温度偏高");
            healthScore -= 15;
        }

        // 显存使用率影响
        if (metrics.GpuMemoryUsedMB > 0 && metrics.GpuMemoryTotalMB > 0)
        {
            var vramUsage = (metrics.GpuMemoryUsedMB / metrics.GpuMemoryTotalMB) * 100;
            if (vramUsage > 95)
            {
                health.Issues.Add("显存几乎用尽，可能导致游戏崩溃");
                healthScore -= 20;
            }
        }

        health.HealthScore = Math.Max(0, healthScore);
        health.Status = healthScore >= 90 ? HealthStatus.Excellent :
                        healthScore >= 75 ? HealthStatus.Good :
                        healthScore >= 60 ? HealthStatus.Fair :
                        HealthStatus.Poor;

        return health;
    }

    private ComponentHealth AnalyzeMemoryHealth(PerformanceMetrics metrics)
    {
        var health = new ComponentHealth { ComponentName = "Memory" };

        var healthScore = 100.0;

        if (metrics.MemoryUsagePercent > 95)
        {
            health.Issues.Add("内存严重不足，系统可能出现卡顿");
            healthScore -= 40;
        }
        else if (metrics.MemoryUsagePercent > 85)
        {
            health.Issues.Add("内存使用率较高");
            healthScore -= 20;
        }
        else if (metrics.MemoryUsagePercent > 70)
        {
            health.Issues.Add("内存使用率偏高，建议关注");
            healthScore -= 10;
        }

        health.HealthScore = Math.Max(0, healthScore);
        health.Status = healthScore >= 90 ? HealthStatus.Excellent :
                        healthScore >= 75 ? HealthStatus.Good :
                        healthScore >= 60 ? HealthStatus.Fair :
                        HealthStatus.Poor;

        return health;
    }

    /// <summary>
    /// 预测异常
    /// </summary>
    public async Task<IEnumerable<AnomalyPrediction>> PredictAnomaliesAsync()
    {
        var predictions = new List<AnomalyPrediction>();

        try
        {
            var metrics = _performanceMonitor.CurrentMetrics;

            // 获取历史数据
            var cpuHistory = _performanceMonitor.GetHistoricalMetrics("CpuUsagePercent", TimeSpan.FromHours(1));
            var memoryHistory = _performanceMonitor.GetHistoricalMetrics("MemoryUsagePercent", TimeSpan.FromHours(1));

            // CPU温度预测
            if (metrics.CpuTemperature > 75)
            {
                var trend = CalculateTrend(cpuHistory.Select(h => h.Value).ToList());
                if (trend > 0.5) // 上升趋势
                {
                    predictions.Add(new AnomalyPrediction
                    {
                        ComponentName = "CPU",
                        Type = AnomalyType.Temperature,
                        Probability = Math.Min(metrics.CpuTemperature / 100, 0.9),
                        PredictedTime = DateTime.UtcNow.AddMinutes(10),
                        Description = "CPU温度呈上升趋势，预计10分钟内可能超过安全阈值",
                        Mitigations = new List<string>
                        {
                            "降低CPU负载",
                            "检查散热器",
                            "改善机箱通风"
                        }
                    });
                }
            }

            // 内存泄漏检测
            if (memoryHistory.Count() >= 60) // 至少1小时数据
            {
                var memoryGrowth = CalculateGrowthRate(memoryHistory.Select(h => h.Value).ToList());
                if (memoryGrowth > 0.1) // 增长率超过10%
                {
                    predictions.Add(new AnomalyPrediction
                    {
                        ComponentName = "Memory",
                        Type = AnomalyType.Memory,
                        Probability = Math.Min(memoryGrowth, 0.85),
                        PredictedTime = DateTime.UtcNow.AddMinutes(30),
                        Description = "检测到内存使用呈持续增长趋势，可能存在内存泄漏",
                        Mitigations = new List<string>
                        {
                            "重启相关应用程序",
                            "检查是否有内存泄漏的程序",
                            "考虑升级内存"
                        }
                    });
                }
            }

            // 性能下降预测
            var fpsHistory = _performanceMonitor.GetHistoricalMetrics("Fps", TimeSpan.FromMinutes(30));
            if (fpsHistory.Count() >= 30)
            {
                var fpsTrend = CalculateTrend(fpsHistory.Select(h => h.Value).ToList());
                if (fpsTrend < -0.3) // 下降趋势
                {
                    predictions.Add(new AnomalyPrediction
                    {
                        ComponentName = "GPU",
                        Type = AnomalyType.Performance,
                        Probability = 0.7,
                        PredictedTime = DateTime.UtcNow.AddMinutes(15),
                        Description = "游戏性能呈下降趋势，可能与温度或后台进程有关",
                        Mitigations = new List<string>
                        {
                            "检查GPU温度",
                            "关闭不必要的后台程序",
                            "清理磁盘空间"
                        }
                    });
                }
            }

            foreach (var prediction in predictions)
            {
                AnomalyPredicted?.Invoke(this, prediction);
            }
        }
        catch (Exception ex)
        {
            Program.Log($"[AI] 异常预测失败: {ex.Message}");
        }

        await Task.CompletedTask;
        return predictions;
    }

    /// <summary>
    /// 计算趋势（正数表示上升，负数表示下降）
    /// </summary>
    private double CalculateTrend(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        // 简单线性回归
        var n = values.Count;
        var xMean = (n - 1) / 2.0;
        var yMean = values.Average();

        var numerator = 0.0;
        var denominator = 0.0;

        for (int i = 0; i < n; i++)
        {
            numerator += (i - xMean) * (values[i] - yMean);
            denominator += (i - xMean) * (i - xMean);
        }

        var slope = denominator != 0 ? numerator / denominator : 0;

        // 归一化到 -1 到 1 之间
        var maxSlope = yMean * 0.1; // 假设10%的变化是显著的
        return Math.Max(-1, Math.Min(1, slope / maxSlope));
    }

    /// <summary>
    /// 计算增长率
    /// </summary>
    private double CalculateGrowthRate(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        // 使用前半部分和后半部分的平均值比较
        var half = values.Count / 2;
        var firstHalf = values.Take(half).Average();
        var secondHalf = values.Skip(half).Average();

        if (firstHalf == 0)
            return 0;

        return (secondHalf - firstHalf) / firstHalf;
    }

    /// <summary>
    /// 处理自然语言命令
    /// </summary>
    public async Task<string> ProcessCommandAsync(string command)
    {
        try
        {
            command = command.ToLowerInvariant().Trim();

            // 意图识别
            if (command.Contains("温度") || command.Contains("temperature"))
            {
                var metrics = _performanceMonitor.CurrentMetrics;
                return $"当前温度：CPU {metrics.CpuTemperature:F0}°C，GPU {metrics.GpuTemperature:F0}°C";
            }

            if (command.Contains("使用率") || command.Contains("usage"))
            {
                var metrics = _performanceMonitor.CurrentMetrics;
                return $"当前使用率：CPU {metrics.CpuUsagePercent:F0}%，内存 {metrics.MemoryUsagePercent:F0}%";
            }

            if (command.Contains("fps") || command.Contains("帧率"))
            {
                var metrics = _performanceMonitor.CurrentMetrics;
                return $"当前帧率：{metrics.Fps:F0} FPS";
            }

            if (command.Contains("建议") || command.Contains("suggest"))
            {
                var suggestions = await GetSuggestionsAsync();
                var topSuggestion = suggestions.FirstOrDefault();
                return topSuggestion != null
                    ? $"{topSuggestion.Title}：{topSuggestion.Description}"
                    : "目前没有发现需要关注的建议";
            }

            if (command.Contains("健康") || command.Contains("health"))
            {
                var analysis = await AnalyzeHardwareAsync();
                return $"硬件健康状态：{analysis.OverallHealth}。{analysis.Recommendations.FirstOrDefault() ?? "一切正常"}";
            }

            return "抱歉，我无法理解您的命令。您可以询问温度、使用率、帧率、建议或健康状态。";
        }
        catch (Exception ex)
        {
            Program.Log($"[AI] 处理命令失败: {ex.Message}");
            return "处理命令时发生错误，请稍后重试。";
        }
    }
}
