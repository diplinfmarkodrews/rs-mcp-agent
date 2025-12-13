using ReportServer.Abstraction.Exceptions;

namespace ReportServer.Abstraction.Contracts;

public class Result
{
    public bool IsSuccess { get; set; } 
    public SerializableException? Error { get; set; } 
    public string? Message { get; set; } = string.Empty;

    public static Result Fail(string message, Exception? error = null)
        => new Result
        {
            IsSuccess = false,
            Message = message,
            Error = error != null 
                ? new SerializableException(error)
                : new SerializableException(message)
        };
    public static Result Fail(Exception error)
        => new Result
        {
            IsSuccess = false,
            Message = error.Message,
            Error = new SerializableException(error)
                
        };
    public static Result Success(string? message = null)
        => new Result
        {
            IsSuccess = true,
            Message = message
        };
}

public class Result<T> : Result
{
    public T? Data { get; set; }
    public Result(Exception? exception)
    {
        IsSuccess = false;
        Message = exception?.Message ?? string.Empty;
        Error = exception != null ? new SerializableException(exception) : null;
    }
    public Result(string message)
    {
        IsSuccess = false;
        Message = message;
    }

    public Result(T data)
    {
        IsSuccess = true;
        Data = data;
    }
    public static new Result<T> Fail(Exception? error = null)
        => new Result<T>(error);
    
    public static new Result<T> Success(T? data)
        => new Result<T>(data);
}

