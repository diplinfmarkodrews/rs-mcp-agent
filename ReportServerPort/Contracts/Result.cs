using System;
using System.Text.Json.Serialization;
using ReportServerPort.Exceptions;

namespace ReportServerPort.Contracts;

public class Result
{
    public bool IsSuccess { get; set; } 
    public SerializableException? Error { get; set; } 
    public string Message { get; set; } = string.Empty;

    public static Result Fail(string message, Exception? error = null)
        => new Result
        {
            IsSuccess = false,
            Message = message,
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
    public Result(Exception exception)
    {
        IsSuccess = false;
        Message = exception.Message;
        Error = new SerializableException(exception);
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
}

