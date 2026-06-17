using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComputerCompanion.Api;

public abstract class ApiController
{
    private readonly Dictionary<string, MethodInfo> _actions = new Dictionary<string, MethodInfo>();

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
            if (string.IsNullOrEmpty(request.Action))
            {
                return ApiResponse<object?>.BadRequest("缺少 action 参数");
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
                
                if (data != null && paramType != typeof(object))
                {
                    data = Convert.ChangeType(data, paramType);
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
        catch (Exception ex)
        {
            Program.Log($"[API] 请求处理异常: {ex.Message}");
            return ApiResponse<object?>.InternalError("服务器内部错误");
        }
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