using System;
using Newtonsoft.Json;

namespace ComputerCompanion.Api;

public class ApiResponse<T>
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("data")]
    public T? Data { get; set; }

    [JsonProperty("error")]
    public ApiError? Error { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("requestId")]
    public string? RequestId { get; set; }

    private ApiResponse(bool success, T? data, ApiError? error, string? message)
    {
        Success = success;
        Data = data;
        Error = error;
        Message = message;
        Timestamp = DateTime.UtcNow;
        RequestId = Guid.NewGuid().ToString();
    }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>(true, data, null, message);
    }

    public static ApiResponse<T> Ok(string message)
    {
        return new ApiResponse<T>(true, default, null, message);
    }

    public static ApiResponse<T> Fail(ApiError error)
    {
        return new ApiResponse<T>(false, default, error, null);
    }

    public static ApiResponse<T> Fail(string errorCode, string message)
    {
        return new ApiResponse<T>(false, default, new ApiError(errorCode, message), null);
    }

    public static ApiResponse<T> NotFound(string message = "资源未找到")
    {
        return new ApiResponse<T>(false, default, new ApiError("NOT_FOUND", message), null);
    }

    public static ApiResponse<T> BadRequest(string message = "请求参数错误")
    {
        return new ApiResponse<T>(false, default, new ApiError("BAD_REQUEST", message), null);
    }

    public static ApiResponse<T> Unauthorized(string message = "未授权访问")
    {
        return new ApiResponse<T>(false, default, new ApiError("UNAUTHORIZED", message), null);
    }

    public static ApiResponse<T> InternalError(string message = "服务器内部错误")
    {
        return new ApiResponse<T>(false, default, new ApiError("INTERNAL_ERROR", message), null);
    }
}

public class ApiError
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("details")]
    public string? Details { get; set; }

    public ApiError(string code, string message, string? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }
}

public static class ApiErrorCodes
{
    public const string Success = "SUCCESS";
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string InternalError = "INTERNAL_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Timeout = "TIMEOUT";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}