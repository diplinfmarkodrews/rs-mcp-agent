namespace MCPServerSDK.Web.Models;

/// <summary>
/// Represents a report template
/// </summary>
public class ReportTemplate
{
    /// <summary>
    /// The template ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The template name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The template description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Required parameters for the template
    /// </summary>
    public List<ParameterDefinition> RequiredParameters { get; set; } = new List<ParameterDefinition>();
    
    /// <summary>
    /// Supported output formats
    /// </summary>
    public List<string> SupportedFormats { get; set; } = new List<string>();
}

/// <summary>
/// Represents a parameter definition for a report template
/// </summary>
public class ParameterDefinition
{
    /// <summary>
    /// The parameter name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The parameter description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The parameter type
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the parameter is required
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// The default value for the parameter
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of a report generation
/// </summary>
public class ReportResult
{
    /// <summary>
    /// Whether the report generation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The error message if the report generation failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// The report data as a byte array
    /// </summary>
    public byte[] ReportData { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// The MIME type of the report
    /// </summary>
    public string ReportMimeType { get; set; } = "application/octet-stream";
    
    /// <summary>
    /// The filename of the report
    /// </summary>
    public string ReportFilename { get; set; } = string.Empty;
}

/// <summary>
/// Report server related types and enums
/// </summary>
public static class ReportServer
{
    /// <summary>
    /// Output format enumeration for reports
    /// </summary>
    public enum OutputFormat
    {
        Pdf,
        Html,
        Excel,
        Word,
        Csv
    }

    /// <summary>
    /// Parameter type enumeration
    /// </summary>
    public enum ParameterType
    {
        String,
        Integer,
        Boolean,
        Date,
        Decimal
    }

    /// <summary>
    /// Report template definition nested class
    /// </summary>
    public class ReportTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ParameterDefinition> RequiredParameters { get; set; } = new List<ParameterDefinition>();
        public List<string> SupportedFormats { get; set; } = new List<string>();
    }

    /// <summary>
    /// Parameter definition nested class
    /// </summary>
    public class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ParameterType Type { get; set; }
        public bool Required { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Response containing available report templates
    /// </summary>
    public class ReportTemplatesResponse
    {
        /// <summary>
        /// List of available templates
        /// </summary>
        public List<ReportTemplate> Templates { get; set; } = new List<ReportTemplate>();
        
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
