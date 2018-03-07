using System;
using System.Runtime.InteropServices;

namespace UTJ.Alembic
{

    /// <summary>
    /// Package Manager for Unity 2017.3 doesn't support native plugins, so we
    /// have to load the module on our own.
    ///
    /// The usual idiom is:
    ///    [DllImport("abci")] public static float aiGetStartTime(aiContext ctx);
    /// You need to replace that with:
    ///    public delegate float aiGetStartTimeDelegate(aiContext ctx);
    //     public static aiGetStartTimeDelegate aiGetStartTime = ModuleResolver.Resolve<aiGetStartTimeDelegate>("aiGetStartTime");
    /// </summary>
    static class ModuleImporter
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        [DllImport("libdl")] static extern IntPtr dlopen(string filename, int flags);
        [DllImport("libdl")] static extern IntPtr dlsym(IntPtr handle, string symbol);
        const int RTLD_LAZY = 1;
        const int RTLD_NOW = 2;
        const int RTLD_LOCAL = 4;
        const int RTLD_GLOBAL = 8;
        const int RTLD_FIRST = 256;
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        [DllImport("__Internal")] static extern IntPtr dlopen(string filename, int flags);
        [DllImport("__Internal")] static extern IntPtr dlsym(IntPtr handle, string symbol);

// dlfcn.h
// #define RTLD_LAZY       0x00001 /* Lazy function call binding.  */
// #define RTLD_NOW        0x00002 /* Immediate function call binding.  */
// #define RTLD_BINDING_MASK   0x3 /* Mask of binding time value.  */
// #define RTLD_NOLOAD     0x00004 /* Do not load the object.  */
// #define RTLD_DEEPBIND   0x00008 /* Use deep binding.  */
//
// /* If the following bit is set in the MODE argument to `dlopen',
//    the symbols of the loaded object and its dependencies are made
//       visible as if the object were linked directly into the program.  */
//       #define RTLD_GLOBAL     0x00100
//
//       /* Unix98 demands the following flag which is the inverse to RTLD_GLOBAL.
//          The implementation does this by default and so we can define the
//             value to zero.  */
//             #define RTLD_LOCAL      0

        const int RTLD_LAZY = 1;
        const int RTLD_NOW = 2;
        const int RTLD_LOCAL = 0;
        const int RTLD_GLOBAL = 256;
        const int RTLD_FIRST = 0;
#endif

# if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        const string libpath_in_unity = "Packages/com.unity.alembic/Runtime/Plugins/x86_64/abci.bundle/Contents/MacOS/abci";
# else // linux
        const string libpath_in_unity = "Packages/com.unity.alembic/Runtime/Plugins/x86_64/libabci.so";
# endif
        static string libpath { get { return System.IO.Path.GetFullPath(libpath_in_unity); } }
        static IntPtr s_moduleHandle = dlopen(libpath, RTLD_LAZY|RTLD_LOCAL|RTLD_FIRST);
#endif

        internal static T Resolve<T>(string name) where T: class {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
            if(s_moduleHandle == (IntPtr)0) { throw new System.ArgumentException(string.Format("abci not found in `{0}'", libpath)); }
            var funHandle = dlsym(s_moduleHandle, name);
            if(funHandle == (IntPtr)0) { throw new System.ArgumentException(string.Format("symbol `{0}' not found in abci", name)); }
            return Marshal.GetDelegateForFunctionPointer(funHandle, typeof(T)) as T;
#else // Windows: TODO!
            return null;
#endif
        }
    }

}
