using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ComputerCompanion.Services;

/// <summary>
/// 输入验证帮助类
/// 提供统一的输入验证方法，防止注入攻击和无效输入
/// </summary>
public static class InputValidator
{
    private static readonly HashSet<char> DangerousChars = new HashSet<char> { '|', '&', ';', '<', '>', '`', '$', '(', ')', '!', '@', '#', '%', '^', '*', '+' };
    private static readonly Regex SqlInjectionPattern = new Regex(@"('|""|;|--|\/\*|\*\/|xp_|sp_|exec\s|execute\s|union\s|select\s|insert\s|update\s|delete\s|drop\s|create\s|alter\s)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex XssPattern = new Regex(@"(<script|javascript:|on\w+\s*=|<iframe|<object|<embed|<link|<style|<form|<input|<button|<a\s+href)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 验证字符串是否为空或仅包含空白字符
    /// </summary>
    public static bool IsNullOrWhiteSpace(string? input)
    {
        return string.IsNullOrWhiteSpace(input);
    }

    /// <summary>
    /// 验证字符串长度是否在指定范围内
    /// </summary>
    public static bool IsValidLength(string? input, int minLength, int maxLength)
    {
        if (input == null) return minLength == 0;
        return input.Length >= minLength && input.Length <= maxLength;
    }

    /// <summary>
    /// 检测字符串是否包含危险字符
    /// </summary>
    public static bool ContainsDangerousChars(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return input.Any(c => DangerousChars.Contains(c));
    }

    /// <summary>
    /// 检测字符串是否包含潜在的SQL注入内容
    /// </summary>
    public static bool ContainsSqlInjection(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return SqlInjectionPattern.IsMatch(input);
    }

    /// <summary>
    /// 检测字符串是否包含潜在的XSS攻击内容
    /// </summary>
    public static bool ContainsXss(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return XssPattern.IsMatch(input);
    }

    /// <summary>
    /// 验证路径是否安全
    /// </summary>
    public static bool IsValidPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        
        if (path.Contains("..")) return false;
        if (path.Length > 260) return false;
        
        try
        {
            var fullPath = System.IO.Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证文件名是否安全
    /// </summary>
    public static bool IsValidFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        if (fileName.IndexOfAny(invalidChars) >= 0) return false;
        if (fileName.Length > 255) return false;
        
        return true;
    }

    /// <summary>
    /// 清理输入字符串，移除危险字符
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        
        var result = input;
        foreach (var c in DangerousChars)
        {
            result = result.Replace(c.ToString(), string.Empty);
        }
        
        return result.Trim();
    }

    /// <summary>
    /// 验证整数是否在指定范围内
    /// </summary>
    public static bool IsInRange(int value, int min, int max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// 验证浮点数是否在指定范围内
    /// </summary>
    public static bool IsInRange(double value, double min, double max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// 验证枚举值是否有效
    /// </summary>
    public static bool IsValidEnum<T>(int value) where T : struct, Enum
    {
        return Enum.IsDefined(typeof(T), value);
    }

    /// <summary>
    /// 验证枚举值是否有效
    /// </summary>
    public static bool IsValidEnum<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Enum.TryParse<T>(value, true, out _);
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    public static ValidationResult Success() => new ValidationResult { IsValid = true };
    
    public static ValidationResult Failure(params string[] errors) => new ValidationResult 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };

    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
}
