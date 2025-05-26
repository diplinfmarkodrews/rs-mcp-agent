using System.Runtime.InteropServices;

namespace MCPServerSDK.Infrastructure.Java;

/// <summary>
/// Provides JNI method calls for Java interop
/// </summary>
public static class JniMethods
{
    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr FindClass(IntPtr env, string name);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetMethodID(IntPtr env, IntPtr clazz, string name, string sig);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr NewStringUTF(IntPtr env, string utf);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr NewObject(IntPtr env, IntPtr clazz, IntPtr methodID, params IntPtr[] args);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr CallObjectMethod(IntPtr env, IntPtr obj, IntPtr methodID, params IntPtr[] args);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern bool CallBooleanMethod(IntPtr env, IntPtr obj, IntPtr methodID, params IntPtr[] args);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetObjectClass(IntPtr env, IntPtr obj);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetFieldID(IntPtr env, IntPtr clazz, string name, string sig);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetObjectField(IntPtr env, IntPtr obj, IntPtr fieldID);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern bool GetBooleanField(IntPtr env, IntPtr obj, IntPtr fieldID);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetByteArrayElements(IntPtr env, IntPtr array, bool isCopy);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern void ReleaseByteArrayElements(IntPtr env, IntPtr array, IntPtr elems, int mode);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern int GetStringUTFLength(IntPtr env, IntPtr str);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetStringUTFChars(IntPtr env, IntPtr str, bool isCopy);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern void ReleaseStringUTFChars(IntPtr env, IntPtr str, IntPtr chars);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr NewByteArray(IntPtr env, int len);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern void SetByteArrayRegion(IntPtr env, IntPtr array, int start, int len, byte[] buf);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern void GetByteArrayRegion(IntPtr env, IntPtr array, int start, int len, byte[] buf);

    [DllImport("jvm", CallingConvention = CallingConvention.StdCall)]
    public static extern int GetArrayLength(IntPtr env, IntPtr array);
}
