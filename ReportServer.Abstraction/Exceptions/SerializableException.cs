using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ReportServer.Abstraction.Exceptions;

// Inline SerializableException definition to avoid resolution issues
[Serializable]
public class SerializableException : Exception
{
    // Custom properties that will be serialized
    public new string StackTrace { get; set; } = string.Empty;
    public string ExceptionSource { get; set; } = string.Empty;
    
    
    // Custom property for inner exception since InnerException is read-only
    public SerializableException? SerializableInnerException { get; set; }
    
    // Parameterless constructor required for System.Text.Json deserialization
    public SerializableException() : base() 
    { 
    }
    
    public SerializableException(Exception exception) : base(exception?.Message ?? "Unknown error") 
    { 
        // Copy only serializable properties from the original exception
        if (exception != null)
        {
            StackTrace = exception.StackTrace ?? string.Empty;
            ExceptionSource = exception.Source ?? string.Empty;
            
            // Handle inner exception (but avoid circular references)
            if (exception.InnerException != null)
            {
                SerializableInnerException = new SerializableException(exception.InnerException);
            }
        }
    }
    
    public SerializableException(string message) : base(message) 
    { 
    }
    
    // Hide problematic properties from serialization using both JsonIgnore attributes
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public new System.Reflection.MethodBase TargetSite => base.TargetSite;
    
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public new Exception InnerException => base.InnerException;
    
    protected SerializableException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}