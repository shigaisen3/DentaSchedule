namespace DentaSchedule.BLL.Common;

public class ServiceResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ServiceResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ServiceResponse<T> FailureResult(string error)
    {
        return new ServiceResponse<T> { Success = false, Errors = new List<string> { error } };
    }

    public static ServiceResponse<T> FailureResult(List<string> errors)
    {
        return new ServiceResponse<T> { Success = false, Errors = errors };
    }
}

public class ServiceResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResponse SuccessResult(string? message = null)
    {
        return new ServiceResponse { Success = true, Message = message };
    }

    public static ServiceResponse FailureResult(string error)
    {
        return new ServiceResponse { Success = false, Errors = new List<string> { error } };
    }

    public static ServiceResponse FailureResult(List<string> errors)
    {
        return new ServiceResponse { Success = false, Errors = errors };
    }
}
