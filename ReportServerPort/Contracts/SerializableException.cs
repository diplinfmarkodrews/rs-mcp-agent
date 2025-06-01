using Newtonsoft.Json;

namespace ReportServerPort.Contracts;

// Inline SerializableException definition to avoid resolution issues
[Serializable]
public class SerializableException : Exception
{
    // Custom properties that will be serialized
    public new string StackTrace { get; set; } = string.Empty;
    public string ExceptionSource { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    
    // Custom property for inner exception since InnerException is read-only
    public SerializableException? SerializableInnerException { get; set; }
    
    public SerializableException(Exception exception) : base(exception?.Message ?? "Unknown error") 
    { 
        // Copy only serializable properties from the original exception
        if (exception != null)
        {
            StackTrace = exception.StackTrace ?? string.Empty;
            ExceptionSource = exception.Source ?? string.Empty;
            ExceptionMessage = exception.Message ?? string.Empty;
            
            // Handle inner exception (but avoid circular references)
            if (exception.InnerException != null)
            {
                SerializableInnerException = new SerializableException(exception.InnerException);
            }
        }
    }
    
    // Hide problematic properties from serialization using JsonIgnore
    [JsonIgnore] 
    public new System.Reflection.MethodBase TargetSite => base.TargetSite;
    
    [JsonIgnore]
    public new Exception InnerException => base.InnerException;

    protected SerializableException() : base() { }
    
    protected SerializableException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}