using System.Runtime.InteropServices;
using MCPServerSDK.Models;
using static MCPServerSDK.Models.ReportServer;
using Microsoft.Extensions.Logging;

namespace MCPServerSDK.Infrastructure.Java;

/// <summary>
/// Helper methods for JNI operations
/// </summary>
public static class JniHelper
{
    /// <summary>
    /// Creates a Java HashMap from a C# Dictionary
    /// </summary>
    public static IntPtr CreateJavaMap(IntPtr env, Dictionary<string, string> dictionary)
    {
        // Find the HashMap class
        var hashMapClass = JniMethods.FindClass(env, "java/util/HashMap");
        if (hashMapClass == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not find HashMap class");
        }
        
        // Get the HashMap constructor
        var hashMapCtor = JniMethods.GetMethodID(env, hashMapClass, "<init>", "()V");
        if (hashMapCtor == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not find HashMap constructor");
        }
        
        // Create a new HashMap
        var hashMap = JniMethods.NewObject(env, hashMapClass, hashMapCtor);
        if (hashMap == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not create HashMap");
        }
        
        // Get the put method
        var putMethod = JniMethods.GetMethodID(env, hashMapClass, "put", "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");
        if (putMethod == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not find HashMap.put method");
        }
        
        // Add the entries to the HashMap
        foreach (var entry in dictionary)
        {
            var keyJni = JniMethods.NewStringUTF(env, entry.Key);
            var valueJni = JniMethods.NewStringUTF(env, entry.Value);
            
            JniMethods.CallObjectMethod(env, hashMap, putMethod, keyJni, valueJni);
        }
        
        return hashMap;
    }
    
    /// <summary>
    /// Gets a C# string from a JNI string
    /// </summary>
    public static string? GetStringFromJniString(IntPtr env, IntPtr jniString)
    {
        if (jniString == IntPtr.Zero)
        {
            return null;
        }
        
        var utf8Chars = JniMethods.GetStringUTFChars(env, jniString, false);
        if (utf8Chars == IntPtr.Zero)
        {
            return null;
        }
        
        var result = Marshal.PtrToStringUTF8(utf8Chars);
        
        JniMethods.ReleaseStringUTFChars(env, jniString, utf8Chars);
        
        return result;
    }
    
    /// <summary>
    /// Gets a C# byte array from a JNI byte array
    /// </summary>
    public static byte[] GetByteArrayFromJniByteArray(IntPtr env, IntPtr jniByteArray)
    {
        if (jniByteArray == IntPtr.Zero)
        {
            return Array.Empty<byte>();
        }
        
        var length = JniMethods.GetArrayLength(env, jniByteArray);
        if (length <= 0)
        {
            return Array.Empty<byte>();
        }
        
        var result = new byte[length];
        JniMethods.GetByteArrayRegion(env, jniByteArray, 0, length, result);
        
        return result;
    }
    
    /// <summary>
    /// Converts a Java ReportResult to a C# ReportResult
    /// </summary>
    public static ReportResult ConvertJavaReportResultToCSharp(IntPtr env, IntPtr javaReportResult)
    {
        var result = new ReportResult();
        
        // Get the ReportResult class
        var reportResultClass = JniMethods.GetObjectClass(env, javaReportResult);
        
        // Get field IDs
        var successFieldId = JniMethods.GetFieldID(env, reportResultClass, "success", "Z");
        var errorMessageFieldId = JniMethods.GetFieldID(env, reportResultClass, "errorMessage", "Ljava/lang/String;");
        var reportDataFieldId = JniMethods.GetFieldID(env, reportResultClass, "reportData", "[B");
        var reportMimeTypeFieldId = JniMethods.GetFieldID(env, reportResultClass, "reportMimeType", "Ljava/lang/String;");
        var reportFilenameFieldId = JniMethods.GetFieldID(env, reportResultClass, "reportFilename", "Ljava/lang/String;");
        
        // Get success field
        result.Success = JniMethods.GetBooleanField(env, javaReportResult, successFieldId);
        
        // Get errorMessage field
        var errorMessageJni = JniMethods.GetObjectField(env, javaReportResult, errorMessageFieldId);
        if (errorMessageJni != IntPtr.Zero)
        {
            result.ErrorMessage = GetStringFromJniString(env, errorMessageJni) ?? string.Empty;
        }
        
        // Get reportData field
        var reportDataJni = JniMethods.GetObjectField(env, javaReportResult, reportDataFieldId);
        result.ReportData = reportDataJni != IntPtr.Zero ? GetByteArrayFromJniByteArray(env, reportDataJni) : Array.Empty<byte>();
        
        // Get reportMimeType field
        var reportMimeTypeJni = JniMethods.GetObjectField(env, javaReportResult, reportMimeTypeFieldId);
        if (reportMimeTypeJni != IntPtr.Zero)
        {
            result.ReportMimeType = GetStringFromJniString(env, reportMimeTypeJni) ?? "application/octet-stream";
        }
        
        // Get reportFilename field
        var reportFilenameJni = JniMethods.GetObjectField(env, javaReportResult, reportFilenameFieldId);
        if (reportFilenameJni != IntPtr.Zero)
        {
            result.ReportFilename = GetStringFromJniString(env, reportFilenameJni) ?? string.Empty;
        }
        
        return result;
    }
    
    /// <summary>
    /// Converts a Java List<ReportTemplate> to a C# List<ReportTemplate>
    /// </summary>
    public static List<ReportServer.ReportTemplate> ConvertJavaReportTemplateListToCSharp(IntPtr env, IntPtr javaList)
    {
        var result = new List<ReportServer.ReportTemplate>();
        
        // Get the List class
        var listClass = JniMethods.GetObjectClass(env, javaList);
        
        // Get the size method and get method IDs
        var sizeMethodId = JniMethods.GetMethodID(env, listClass, "size", "()I");
        var getMethodId = JniMethods.GetMethodID(env, listClass, "get", "(I)Ljava/lang/Object;");
        
        if (sizeMethodId == IntPtr.Zero || getMethodId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not find List methods");
        }
        
        // Get the size of the list
        var size = CallIntMethod(env, javaList, sizeMethodId);
        
        // Get each ReportTemplate from the list
        for (int i = 0; i < size; i++)
        {
            var javaTemplate = JniMethods.CallObjectMethod(env, javaList, getMethodId, (IntPtr)i);
            if (javaTemplate != IntPtr.Zero)
            {
                result.Add(ConvertJavaReportTemplateToCSharp(env, javaTemplate));
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Converts a Java ReportTemplate to a C# ReportTemplate
    /// </summary>
    private static ReportServer.ReportTemplate ConvertJavaReportTemplateToCSharp(IntPtr env, IntPtr javaTemplate)
    {
        var template = new ReportServer.ReportTemplate();
        
        // Get the ReportTemplate class
        var templateClass = JniMethods.GetObjectClass(env, javaTemplate);
        
        // Get field IDs
        var idFieldId = JniMethods.GetFieldID(env, templateClass, "id", "Ljava/lang/String;");
        var nameFieldId = JniMethods.GetFieldID(env, templateClass, "name", "Ljava/lang/String;");
        var descriptionFieldId = JniMethods.GetFieldID(env, templateClass, "description", "Ljava/lang/String;");
        var requiredParametersFieldId = JniMethods.GetFieldID(env, templateClass, "requiredParameters", "Ljava/util/List;");
        var supportedFormatsFieldId = JniMethods.GetFieldID(env, templateClass, "supportedFormats", "Ljava/util/List;");
        
        // Get fields
        var idJni = JniMethods.GetObjectField(env, javaTemplate, idFieldId);
        var nameJni = JniMethods.GetObjectField(env, javaTemplate, nameFieldId);
        var descriptionJni = JniMethods.GetObjectField(env, javaTemplate, descriptionFieldId);
        var requiredParametersJni = JniMethods.GetObjectField(env, javaTemplate, requiredParametersFieldId);
        var supportedFormatsJni = JniMethods.GetObjectField(env, javaTemplate, supportedFormatsFieldId);
        
        // Convert fields
        if (idJni != IntPtr.Zero) template.Id = GetStringFromJniString(env, idJni) ?? string.Empty;
        if (nameJni != IntPtr.Zero) template.Name = GetStringFromJniString(env, nameJni) ?? string.Empty;
        if (descriptionJni != IntPtr.Zero) template.Description = GetStringFromJniString(env, descriptionJni) ?? string.Empty;
        if (requiredParametersJni != IntPtr.Zero) template.RequiredParameters = ConvertJavaParameterDefinitionListToCSharp(env, requiredParametersJni);
        if (supportedFormatsJni != IntPtr.Zero) template.SupportedFormats = GetStringListFromJniList(env, supportedFormatsJni);
        
        return template;
    }
    
    /// <summary>
    /// Gets a C# List<string> from a JNI List<String>
    /// </summary>
    private static List<string> GetStringListFromJniList(IntPtr env, IntPtr jniList)
    {
        var result = new List<string>();
        
        // Get the List class
        var listClass = JniMethods.GetObjectClass(env, jniList);
        
        // Get the size method and get method IDs
        var sizeMethodId = JniMethods.GetMethodID(env, listClass, "size", "()I");
        var getMethodId = JniMethods.GetMethodID(env, listClass, "get", "(I)Ljava/lang/Object;");
        
        if (sizeMethodId == IntPtr.Zero || getMethodId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not find List methods");
        }
        
        // Get the size of the list
        var size = CallIntMethod(env, jniList, sizeMethodId);
        
        // Get each string from the list
        for (int i = 0; i < size; i++)
        {
            var javaString = JniMethods.CallObjectMethod(env, jniList, getMethodId, (IntPtr)i);
            if (javaString != IntPtr.Zero)
            {
                var str = GetStringFromJniString(env, javaString);
                if (str != null)
                {
                    result.Add(str);
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Converts a Java List<ParameterDefinition> to a C# List<ParameterDefinition>
    /// </summary>
    private static List<ReportServer.ParameterDefinition> ConvertJavaParameterDefinitionListToCSharp(IntPtr env, IntPtr javaList)
    {
        var result = new List<ReportServer.ParameterDefinition>();
        
        // Get the List class
        var listClass = JniMethods.GetObjectClass(env, javaList);
        
        // Get the size method and get method IDs
        var sizeMethodId = JniMethods.GetMethodID(env, listClass, "size", "()I");
        var getMethodId = JniMethods.GetMethodID(env, listClass, "get", "(I)Ljava/lang/Object;");
        
        if (sizeMethodId == IntPtr.Zero || getMethodId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not find List methods");
        }
        
        // Get the size of the list
        var size = CallIntMethod(env, javaList, sizeMethodId);
        
        // Get each ParameterDefinition from the list
        for (int i = 0; i < size; i++)
        {
            var javaParameter = JniMethods.CallObjectMethod(env, javaList, getMethodId, (IntPtr)i);
            if (javaParameter != IntPtr.Zero)
            {
                result.Add(ConvertJavaParameterDefinitionToCSharp(env, javaParameter));
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Converts a Java ParameterDefinition to a C# ParameterDefinition
    /// </summary>
    private static ReportServer.ParameterDefinition ConvertJavaParameterDefinitionToCSharp(IntPtr env, IntPtr javaParameter)
    {
        var parameter = new ReportServer.ParameterDefinition();
        
        // Get the ParameterDefinition class
        var parameterClass = JniMethods.GetObjectClass(env, javaParameter);
        
        // Get field IDs
        var nameFieldId = JniMethods.GetFieldID(env, parameterClass, "name", "Ljava/lang/String;");
        var descriptionFieldId = JniMethods.GetFieldID(env, parameterClass, "description", "Ljava/lang/String;");
        var typeFieldId = JniMethods.GetFieldID(env, parameterClass, "type", "Ljava/lang/String;");
        var requiredFieldId = JniMethods.GetFieldID(env, parameterClass, "required", "Z");
        var defaultValueFieldId = JniMethods.GetFieldID(env, parameterClass, "defaultValue", "Ljava/lang/String;");
        
        // Get fields
        var nameJni = JniMethods.GetObjectField(env, javaParameter, nameFieldId);
        var descriptionJni = JniMethods.GetObjectField(env, javaParameter, descriptionFieldId);
        var typeJni = JniMethods.GetObjectField(env, javaParameter, typeFieldId);
        var defaultValueJni = JniMethods.GetObjectField(env, javaParameter, defaultValueFieldId);
        
        // Convert fields
        if (nameJni != IntPtr.Zero) parameter.Name = GetStringFromJniString(env, nameJni) ?? string.Empty;
        if (descriptionJni != IntPtr.Zero) parameter.Description = GetStringFromJniString(env, descriptionJni) ?? string.Empty;
        if (typeJni != IntPtr.Zero) parameter.Type = ReportServer.ParameterType.String; // Default to String type
        parameter.Required = JniMethods.GetBooleanField(env, javaParameter, requiredFieldId);
        if (defaultValueJni != IntPtr.Zero) parameter.DefaultValue = GetStringFromJniString(env, defaultValueJni) ?? string.Empty;
        
        return parameter;
    }
    
    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    private static extern int CallIntMethod(IntPtr env, IntPtr obj, IntPtr methodID, params IntPtr[] args);
}
