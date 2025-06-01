using System;
using System.Text.Json.Serialization;

namespace ReportServerPort.Contracts;

public class Result
{
    public bool IsSuccess { get; set; } 
    public SerializableException? Error { get; set; } 
    public string Message { get; set; } = string.Empty;
}

public class Result<T> : Result
{
    public T? Data { get; set; }
    public Result(Exception exception)
    {
        IsSuccess = false;
        Error = new SerializableException(exception);
    }
    public Result(string message) 
    { 
        IsSuccess = false;
        Message = message;
    }
        
    public Result(T data)
    {
        Data = data;
    }
}

