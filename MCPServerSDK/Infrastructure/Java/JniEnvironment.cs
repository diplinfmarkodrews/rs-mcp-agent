using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace MCPServerSDK.Infrastructure.Java;

/// <summary>
/// Manages JNI initialization and cleanup
/// </summary>
public class JniEnvironment : IDisposable
{
    private readonly ILogger<JniEnvironment> _logger;
    private readonly string _jvmLibPath;
    private readonly string _jniWrapperPath;
    private IntPtr _jvmHandle = IntPtr.Zero;
    private IntPtr _jniEnv = IntPtr.Zero;

    public IntPtr JniEnv => _jniEnv;
    public bool IsInitialized => _jvmHandle != IntPtr.Zero && _jniEnv != IntPtr.Zero;

    public JniEnvironment(ILogger<JniEnvironment> logger, string? jvmLibPath = null, string? jniWrapperPath = null)
    {
        _logger = logger;
        _jvmLibPath = jvmLibPath ?? DetectJvmLibraryPath();
        _jniWrapperPath = jniWrapperPath ?? "./java-wrapper.jar";
        InitializeJavaVirtualMachine();
    }

    private string DetectJvmLibraryPath()
    {
        // Try to find JVM library path automatically
        foreach (var path in JniConstants.CommonJvmPaths)
        {
            if (File.Exists(path))
            {
                _logger.LogInformation("Found JVM library at: {Path}", path);
                return path;
            }
        }

        // Fallback to JAVA_HOME if set
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrEmpty(javaHome))
        {
            var jvmPath = Path.Combine(javaHome, "lib", "server", "libjvm.so");
            if (File.Exists(jvmPath))
            {
                _logger.LogInformation("Found JVM library via JAVA_HOME: {Path}", jvmPath);
                return jvmPath;
            }
        }

        throw new InvalidOperationException("Could not locate JVM library. Please ensure Java 17 is installed and JAVA_HOME is set.");
    }

    private void InitializeJavaVirtualMachine()
    {
        try
        {
            _logger.LogInformation("Initializing Java Virtual Machine");
            
            // Check if JVM is already created
            int nVMs = 0;
            JNI_GetCreatedJavaVMs(out _jvmHandle, 1, out nVMs);
            
            if (nVMs == 0)
            {
                // Create JVM initialization arguments
                var vmArgs = new JavaVMInitArgs();
                vmArgs.version = JNI_VERSION_1_8;
                
                // Set up class path to include our JNI wrapper
                string classPath = $"-Djava.class.path={_jniWrapperPath}";
                
                // Add the classpath option
                vmArgs.nOptions = 1;
                var options = new JavaVMOption[1];
                options[0] = new JavaVMOption { optionString = Marshal.StringToCoTaskMemAnsi(classPath) };
                
                var optionsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<JavaVMOption>() * options.Length);
                for (int i = 0; i < options.Length; i++)
                {
                    Marshal.StructureToPtr(options[i], optionsPtr + i * Marshal.SizeOf<JavaVMOption>(), false);
                }
                
                vmArgs.options = optionsPtr;
                
                // Create the JVM
                var argsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<JavaVMInitArgs>());
                Marshal.StructureToPtr(vmArgs, argsPtr, false);
                
                int result = JNI_CreateJavaVM(out _jvmHandle, out _jniEnv, argsPtr);
                
                // Clean up allocated memory
                Marshal.FreeHGlobal(argsPtr);
                Marshal.FreeHGlobal(optionsPtr);
                
                if (result != 0 || _jvmHandle == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"Failed to create Java VM, error code: {result}");
                }
            }
            else
            {
                // TODO: Get current thread's JNI environment pointer
                // In a real implementation, we would attach the current thread to the JVM
                // and get the JNI environment pointer
            }
            
            _logger.LogInformation("Java Virtual Machine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Java Virtual Machine");
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            if (_jvmHandle != IntPtr.Zero)
            {
                // TODO: Destroy JVM if this is the last reference
                _jvmHandle = IntPtr.Zero;
                _jniEnv = IntPtr.Zero;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing JNI environment");
        }
    }

    #region JNI P/Invoke Declarations

    private const int JNI_VERSION_1_8 = 0x00010008;

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    private static extern int JNI_CreateJavaVM(out IntPtr pJvm, out IntPtr pEnv, IntPtr pArgs);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    private static extern int JNI_GetCreatedJavaVMs(out IntPtr pJvm, int jSize, out int nVMs);

    [StructLayout(LayoutKind.Sequential)]
    private struct JavaVMOption
    {
        public IntPtr optionString;
        public IntPtr extraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JavaVMInitArgs
    {
        public int version;
        public int nOptions;
        public IntPtr options;
        public bool ignoreUnrecognized;
    }

    #endregion
}
