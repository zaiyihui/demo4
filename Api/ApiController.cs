using System;
using System.Collections.Generic;
using System.Reflection;
using ComputerCompanion.Services;

namespace ComputerCompanion.Api;

public abstract class ApiController
{
    private readonly Dictionary<string, MethodInfo> _actions = new Dictionary<string, MethodInfo>();
    private const int MaxActionNameLength = 100;
    private const int MaxDataStringLength = 10000;

    protected ApiController()
    {
        RegisterActions();
    }

    private void RegisterActions()
    {
        var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var method in methods)
        {
            var actionAttr = method.GetCustomAttribute<ApiActionAttribute>();
            if (actionAttr != null)
            {
                var actionName = actionAttr.Name ?? method.Name;
                _actions[actionName.ToLower()] = method;
                Program.Log($"[API] 注册动作: {actionName} -> {method.Name}");
            }
        }
    }

    public ApiResponse<object?> HandleRequest(ApiRequest<object> request)
    {
        try
        {
            // 输入验证
            var validationResult = ValidateRequest(request);
            if (!validationResult.IsValid)
            {
                return ApiResponse<object?>.BadRequest(string.Join(", ", validationResult.Errors));
            }

            if (!_actions.TryGetValue(request.Action.ToLower(), out var method))
            {
                return ApiResponse<object?>.NotFound($"未知动作: {request.Action}");
            }

            var parameters = method.GetParameters();
            object?[] args;

            if (parameters.Length == 0)
            {
                args = Array.Empty<object>();
            }
            else if (parameters.Length == 1)
            {
                var paramType = parameters[0].ParameterType;
                var data = request.Data;
                
                // 验证参数数据
                if (data != null)
                {
                    var dataValidation = ValidateData(data);
                    if (!dataValidation.IsValid)
                    {
                        return ApiResponse<object?>.BadRequest(string.Join(", ", dataValidation.Errors));
                    }
                }
                
                if (data != null && paramType != typeof(object))
                {
                    try
                    {
                        data = Convert.ChangeType(data, paramType);
                    }
                    catch (Exception)
                    {
                        return ApiResponse<object?>.BadRequest($"参数类型转换失败: 期望 {paramType.Name}");
                    }
                }
                args = new[] { data };
            }
            else
            {
                return ApiResponse<object?>.BadRequest("不支持多个参数的动作");
            }

            var result = method.Invoke(this, args);
            
            if (result is ApiResponse<object?> response)
            {
                return response;
            }
            
            return ApiResponse<object?>.Ok(result, "操作成功");
        }
        catch (ApiException ex)
        {
            return ApiResponse<object?>.Fail(ex.ErrorCode, ex.Message);
        }
        catch (TargetInvocationException ex)
        {
            Program.Log($"[API] 请求处理异常: {ex.InnerException?.Message ?? ex.Message}");
            return ApiResponse<object?>.InternalError("服务器内部错误");
        }
        catch (Exception ex)
        {
            Program.Log($"[API] 请求处理异常: {ex.Message}");
            return ApiResponse<object?>.InternalError("服务器内部错误");
        }
    }

    /// <summary>
    /// 验证请求
    /// </summary>
    private ValidationResult ValidateRequest(ApiRequest<object> request)
    {
        var result = new ValidationResult();

        // 验证 Action
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            result.AddError("缺少 action 参数");
            return result;
        }

        if (request.Action.Length > MaxActionNameLength)
        {
            result.AddError($"action 参数长度超过限制 ({MaxActionNameLength})");
        }

        // 检查危险字符
        if (InputValidator.ContainsDangerousChars(request.Action))
        {
            result.AddError("action 参数包含非法字符");
        }

        return result;
    }

    /// <summary>
    /// 验证数据参数
    /// </summary>
    private ValidationResult ValidateData(object data)
    {
        var result = new ValidationResult();

        if (data is string strData)
        {
            if (strData.Length > MaxDataStringLength)
            {
                result.AddError($"数据长度超过限制 ({MaxDataStringLength})");
            }

            if (InputValidator.ContainsSqlInjection(strData))
            {
                result.AddError("数据包含潜在的SQL注入内容");
            }

            if (InputValidator.ContainsXss(strData))
            {
                result.AddError("数据包含潜在的XSS攻击内容");
            }
        }

        return result;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ApiActionAttribute : Attribute
{
    public string? Name { get; }

    public ApiActionAttribute(string? name = null)
    {
        Name = name;
    }
}