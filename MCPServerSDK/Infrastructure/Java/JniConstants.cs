namespace MCPServerSDK.Infrastructure.Java;

public static class JniConstants
{
    // JNI version constants
    public const int JNI_VERSION_1_8 = 0x00010008;
    public const int JNI_VERSION_9 = 0x00090000;
    public const int JNI_VERSION_10 = 0x000a0000;
    public const int JNI_VERSION_19 = 0x00130000; // Java 19 (latest stable JNI version)
    
    // Return codes
    public const int JNI_OK = 0;
    public const int JNI_ERR = -1;
    public const int JNI_EDETACHED = -2;
    public const int JNI_EVERSION = -3;
    public const int JNI_ENOMEM = -4;
    public const int JNI_EEXIST = -5;
    public const int JNI_EINVAL = -6;
    
    // Common Java 17 library paths
    public static readonly string[] CommonJvmPaths = {
        "/usr/lib/jvm/java-17-openjdk-amd64/lib/server/libjvm.so",
        "/usr/lib/jvm/java-17-openjdk/lib/server/libjvm.so",
        "/usr/lib/jvm/java-17-oracle/lib/server/libjvm.so",
        "/usr/lib/jvm/default-java/lib/server/libjvm.so"
    };
}
