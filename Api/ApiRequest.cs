using System;
using Newtonsoft.Json;

namespace ComputerCompanion.Api;

public class ApiRequest<T>
{
    [JsonProperty("action")]
    public string Action { get; set; }

    [JsonProperty("data")]
    public T? Data { get; set; }

    [JsonProperty("requestId")]
    public string? RequestId { get; set; }

    [JsonIgnore]
    public string? RawContent { get; set; }

    public ApiRequest(string action, T? data = default)
    {
        Action = action;
        Data = data;
        RequestId = Guid.NewGuid().ToString();
    }

    public static ApiRequest<T> Parse(string json)
    {
        var request = JsonConvert.DeserializeObject<ApiRequest<T>>(json);
        if (request == null)
        {
            throw new ApiException("Invalid request format", ApiErrorCodes.BadRequest);
        }
        request.RawContent = json;
        return request;
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class ApiException : Exception
{
    public string ErrorCode { get; }

    public ApiException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public ApiException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public ApiResponse<TResult> ToResponse<TResult>()
    {
        return ApiResponse<TResult>.Fail(ErrorCode, Message);
    }
}