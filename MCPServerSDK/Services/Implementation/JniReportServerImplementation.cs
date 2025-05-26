using MCPServerSDK.Infrastructure.Java;
using MCPServerSDK.Models;
using Microsoft.Extensions.Logging;
using static MCPServerSDK.Infrastructure.Java.JniMethods;
using static MCPServerSDK.Infrastructure.Java.JniConstants;
using static MCPServerSDK.Infrastructure.Java.JniHelper;

namespace MCPServerSDK.Services.Implementation;

internal class JniReportServerImplementation : IDisposable
{
    private readonly ILogger<JniReportServerImplementation> _logger;
    private readonly JniEnvironment _jniEnv;
    private readonly IntPtr _reportServerClientHandle;
    private readonly IntPtr _bridgeClassHandle;
    private readonly IntPtr _generateReportMethodId;
    private readonly IntPtr _getTemplatesMethodId;
    private readonly IntPtr _checkHealthMethodId;

    public JniReportServerImplementation(ILoggerFactory logger, string serverAddress)
    {
        _logger = logger.CreateLogger<JniReportServerImplementation>();
        
        try
        {
            // Initialize JVM and get JNI environment
            _jniEnv = new JniEnvironment(logger.CreateLogger<JniEnvironment>());
            
            // Find the ReportServerJniBridge class
            _bridgeClassHandle = JniMethods.FindClass(_jniEnv.JniEnv, "com/example/reportserver/bridge/ReportServerJniBridge");
            if (_bridgeClassHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find ReportServerJniBridge class");
            }
            
            // Get constructor and method IDs
            var constructorMethodId = JniMethods.GetMethodID(_jniEnv.JniEnv, _bridgeClassHandle, "<init>", "(Ljava/lang/String;)V");
            _generateReportMethodId = JniMethods.GetMethodID(_jniEnv.JniEnv, _bridgeClassHandle, "generateReport", 
                "(Ljava/lang/String;Ljava/util/Map;Ljava/lang/String;Z)Lcom/example/reportserver/bridge/ReportResult;");
            _getTemplatesMethodId = JniMethods.GetMethodID(_jniEnv.JniEnv, _bridgeClassHandle, "getAvailableReportTemplates", 
                "()Ljava/util/List;");
            _checkHealthMethodId = JniMethods.GetMethodID(_jniEnv.JniEnv, _bridgeClassHandle, "checkHealth", 
                "()Z");
            
            if (constructorMethodId == IntPtr.Zero || _generateReportMethodId == IntPtr.Zero || 
                _getTemplatesMethodId == IntPtr.Zero || _checkHealthMethodId == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find required methods in ReportServerJniBridge");
            }
            
            // Create ReportServerJniBridge instance
            var serverAddressJni = JniMethods.NewStringUTF(_jniEnv.JniEnv, serverAddress);
            _reportServerClientHandle = JniMethods.NewObject(_jniEnv.JniEnv, _bridgeClassHandle, constructorMethodId, serverAddressJni);
            
            if (_reportServerClientHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create ReportServerJniBridge instance");
            }
        }
        catch (Exception)
        {
            // Clean up any resources that were created
            Dispose();
            throw;
        }
    }
    
    public ReportResult GenerateReport(
        string templateId,
        Dictionary<string, string> parameters,
        string outputFormat,
        bool includeCharts)
    {
        var templateIdJni = JniMethods.NewStringUTF(_jniEnv.JniEnv, templateId);
        var outputFormatJni = JniMethods.NewStringUTF(_jniEnv.JniEnv, outputFormat);
        var paramMapJni = JniHelper.CreateJavaMap(_jniEnv.JniEnv, parameters);
        
        var resultJni = JniMethods.CallObjectMethod(_jniEnv.JniEnv, _reportServerClientHandle, _generateReportMethodId,
            templateIdJni, paramMapJni, outputFormatJni, includeCharts ? (IntPtr)1 : IntPtr.Zero);
        
        if (resultJni == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to generate report - null result returned");
        }
        
        return JniHelper.ConvertJavaReportResultToCSharp(_jniEnv.JniEnv, resultJni);
    }
    
    public List<ReportServer.ReportTemplate> GetAvailableReportTemplates()
    {
        var templatesListJni = JniMethods.CallObjectMethod(_jniEnv.JniEnv, _reportServerClientHandle, _getTemplatesMethodId);
        
        if (templatesListJni == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to retrieve report templates - null result returned");
        }
        
        return JniHelper.ConvertJavaReportTemplateListToCSharp(_jniEnv.JniEnv, templatesListJni);
    }
    
    public bool CheckHealth()
    {
        return JniMethods.CallBooleanMethod(_jniEnv.JniEnv, _reportServerClientHandle, _checkHealthMethodId);
    }

    public void Dispose()
    {
        // Clean up JNI resources
        if (_reportServerClientHandle != IntPtr.Zero && _jniEnv.JniEnv != IntPtr.Zero)
        {
            // Delete global references
            // Note: In a real implementation, we would use DeleteGlobalRef here
            _logger.LogInformation("Disposing JNI resources");
        }
        
        // Dispose JNI environment
        _jniEnv?.Dispose();
    }
}
