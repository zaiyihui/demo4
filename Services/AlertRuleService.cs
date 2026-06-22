using System;
using System.Collections.Generic;
using System.Linq;
using ComputerCompanion.Core.Models;

namespace ComputerCompanion.Services;

/// <summary>
/// 告警严重级别
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// 信息级别
    /// </summary>
    Info,
    
    /// <summary>
    /// 警告级别
    /// </summary>
    Warning,
    
    /// <summary>
    /// 错误级别
    /// </summary>
    Error,
    
    /// <summary>
    /// 严重级别
    /// </summary>
    Critical
}

/// <summary>
/// 告警规则类型
/// </summary>
public enum AlertRuleType
{
    /// <summary>
    /// 阈值规则 - 当指标超过/低于某个阈值时触发
    /// </summary>
    Threshold,
    
    /// <summary>
    /// 趋势规则 - 根据指标变化趋势触发
    /// </summary>
    Trend,
    
    /// <summary>
    /// 异常规则 - 检测异常模式
    /// </summary>
    Anomaly
}

/// <summary>
/// 比较操作符
/// </summary>
public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    EqualTo,
    NotEqualTo,
    GreaterThanOrEqual,
    LessThanOrEqual
}

/// <summary>
/// 告警规则实体
/// </summary>
public class AlertRule
{
    /// <summary>
    /// 规则唯一标识
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 监控的指标类型
    /// </summary>
    public string MetricType { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则类型
    /// </summary>
    public AlertRuleType RuleType { get; set; }
    
    /// <summary>
    /// 比较操作符
    /// </summary>
    public ComparisonOperator Comparison { get; set; }
    
    /// <summary>
    /// 阈值
    /// </summary>
    public double Threshold { get; set; }
    
    /// <summary>
    /// 严重级别
    /// </summary>
    public AlertSeverity Severity { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 触发延迟（秒）- 持续满足条件指定时间后才触发
    /// </summary>
    public int TriggerDelaySeconds { get; set; } = 0;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 告警规则服务接口
/// </summary>
public interface IAlertRuleService
{
    /// <summary>
    /// 获取所有规则
    /// </summary>
    List<AlertRule> GetAllRules();
    
    /// <summary>
    /// 根据ID获取规则
    /// </summary>
    AlertRule? GetRule(Guid id);
    
    /// <summary>
    /// 添加规则
    /// </summary>
    void AddRule(AlertRule rule);
    
    /// <summary>
    /// 更新规则
    /// </summary>
    void UpdateRule(AlertRule rule);
    
    /// <summary>
    /// 删除规则
    /// </summary>
    void DeleteRule(Guid id);
    
    /// <summary>
    /// 切换规则启用状态
    /// </summary>
    void ToggleRule(Guid id);
    
    /// <summary>
    /// 评估规则是否触发
    /// </summary>
    bool EvaluateRule(AlertRule rule, double currentValue);
    
    /// <summary>
    /// 获取所有启用的规则
    /// </summary>
    List<AlertRule> GetEnabledRules();
    
    /// <summary>
    /// 获取指定指标类型的规则
    /// </summary>
    List<AlertRule> GetRulesByMetricType(string metricType);
}

public class AlertRuleService : IAlertRuleService
{
    private readonly List<AlertRule> _rules = new List<AlertRule>();
    private readonly ISettingsService _settingsService;

    public AlertRuleService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadRules();
        InitializeDefaultRules();
    }

    private void InitializeDefaultRules()
    {
        if (_rules.Any()) return;

        _rules.Add(new AlertRule
        {
            Name = "CPU 使用率过高",
            Description = "当CPU使用率超过90%时触发告警",
            MetricType = "CpuUsagePercent",
            RuleType = AlertRuleType.Threshold,
            Comparison = ComparisonOperator.GreaterThan,
            Threshold = 90,
            Severity = AlertSeverity.Warning,
            IsEnabled = true
        });

        _rules.Add(new AlertRule
        {
            Name = "内存使用率过高",
            Description = "当内存使用率超过85%时触发告警",
            MetricType = "MemoryUsagePercent",
            RuleType = AlertRuleType.Threshold,
            Comparison = ComparisonOperator.GreaterThan,
            Threshold = 85,
            Severity = AlertSeverity.Warning,
            IsEnabled = true
        });

        _rules.Add(new AlertRule
        {
            Name = "CPU 温度过高",
            Description = "当CPU温度超过85度时触发告警",
            MetricType = "CpuTemperature",
            RuleType = AlertRuleType.Threshold,
            Comparison = ComparisonOperator.GreaterThan,
            Threshold = 85,
            Severity = AlertSeverity.Error,
            IsEnabled = true
        });

        SaveRules();
    }

    public List<AlertRule> GetAllRules()
    {
        return _rules.ToList();
    }

    public AlertRule? GetRule(Guid id)
    {
        return _rules.FirstOrDefault(r => r.Id == id);
    }

    public void AddRule(AlertRule rule)
    {
        rule.Id = Guid.NewGuid();
        rule.CreatedAt = DateTime.Now;
        rule.UpdatedAt = DateTime.Now;
        _rules.Add(rule);
        SaveRules();
    }

    public void UpdateRule(AlertRule rule)
    {
        var existing = _rules.FirstOrDefault(r => r.Id == rule.Id);
        if (existing != null)
        {
            existing.Name = rule.Name;
            existing.Description = rule.Description;
            existing.MetricType = rule.MetricType;
            existing.RuleType = rule.RuleType;
            existing.Comparison = rule.Comparison;
            existing.Threshold = rule.Threshold;
            existing.Severity = rule.Severity;
            existing.IsEnabled = rule.IsEnabled;
            existing.TriggerDelaySeconds = rule.TriggerDelaySeconds;
            existing.UpdatedAt = DateTime.Now;
            SaveRules();
        }
    }

    public void DeleteRule(Guid id)
    {
        _rules.RemoveAll(r => r.Id == id);
        SaveRules();
    }

    public void ToggleRule(Guid id)
    {
        var rule = _rules.FirstOrDefault(r => r.Id == id);
        if (rule != null)
        {
            rule.IsEnabled = !rule.IsEnabled;
            rule.UpdatedAt = DateTime.Now;
            SaveRules();
        }
    }

    public bool EvaluateRule(AlertRule rule, double currentValue)
    {
        if (!rule.IsEnabled) return false;

        return rule.Comparison switch
        {
            ComparisonOperator.GreaterThan => currentValue > rule.Threshold,
            ComparisonOperator.LessThan => currentValue < rule.Threshold,
            ComparisonOperator.EqualTo => Math.Abs(currentValue - rule.Threshold) < 0.001,
            ComparisonOperator.NotEqualTo => Math.Abs(currentValue - rule.Threshold) >= 0.001,
            ComparisonOperator.GreaterThanOrEqual => currentValue >= rule.Threshold,
            ComparisonOperator.LessThanOrEqual => currentValue <= rule.Threshold,
            _ => false
        };
    }

    public List<AlertRule> GetEnabledRules()
    {
        return _rules.Where(r => r.IsEnabled).ToList();
    }

    public List<AlertRule> GetRulesByMetricType(string metricType)
    {
        if (string.IsNullOrWhiteSpace(metricType))
            throw new ArgumentException("指标类型不能为空", nameof(metricType));
        
        return _rules.Where(r => string.Equals(r.MetricType, metricType, StringComparison.OrdinalIgnoreCase))
                     .ToList();
    }

    private void LoadRules()
    {
        try
        {
            var rules = _settingsService.LoadAlertRules();
            if (rules != null)
            {
                _rules.Clear();
                _rules.AddRange(rules);
            }
        }
        catch
        {
        }
    }

    private void SaveRules()
    {
        try
        {
            _settingsService.SaveAlertRules(_rules);
        }
        catch
        {
        }
    }
}
