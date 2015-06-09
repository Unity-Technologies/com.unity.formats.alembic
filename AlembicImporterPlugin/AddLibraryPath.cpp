#include <string>

#ifdef _MSC_VER
#   define alpWindows
#endif // _MSC_VER

#ifdef alpWindows
#include <windows.h>
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif


extern "C" DLLEXPORT void AddLibraryPath()
{
    // dummy
}

#ifdef alpWindows
BOOL WINAPI DllMain(HINSTANCE module_handle, DWORD reason_for_call, LPVOID reserved)
{
    if (reason_for_call == DLL_PROCESS_ATTACH)
    {
        static bool s_is_first = true;
        if (s_is_first) {
            s_is_first = false;

            std::string path;
            path.resize(1024 * 64);

            DWORD ret = ::GetEnvironmentVariableA("PATH", &path[0], path.size());
            path.resize(ret);
            {
                char path_to_this_module[MAX_PATH];
                HMODULE mod = 0;
                ::GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCSTR)&AddLibraryPath, &mod);
                DWORD size = ::GetModuleFileNameA(mod, path_to_this_module, sizeof(path_to_this_module));
                for (int i = size - 1; i >= 0; --i) {
                    if (path_to_this_module[i] == '\\') {
                        path_to_this_module[i] = '\0';
                        break;
                    }
                }
                path += ";";
                path += path_to_this_module;
            }
            ::SetEnvironmentVariableA("PATH", path.c_str());
        }
    }
    else if (reason_for_call == DLL_PROCESS_DETACH)
    {
    }
    return TRUE;
}

// "DllMain already defined in MSVCRT.lib" 対策
#ifdef _X86_
extern "C" { int _afxForceUSRDLL; }
#else
extern "C" { int __afxForceUSRDLL; }
#endif
#else // alpWindows

#include <dlfcn.h>
#include <cstdlib>

#ifdef __APPLE__
const char *gVarName = "DYLD_LIBRARY_PATH";
#else
const char *gVarName = "LD_LIBRARY_PATH";
#endif

__attribute__((constructor)) void AddLibraryPath_init(void)
{
   Dl_info info;
   const void* addr = (const void*) &AddLibraryPath;
   
   if (dladdr(addr, &info) != 0)
   {
      std::string thisPath = info.dli_fname;
      
      size_t p = thisPath.rfind('/');
      
      if (p != std::string::npos)
      {
         thisPath = thisPath.substr(0, p);
      }
      
      std::string searchPath;
      
      char *val = getenv(gVarName);
      
      if (val)
      {
         searchPath = val;
      }
      
      if (searchPath.length() > 0 && searchPath[searchPath.length()-1] != ':')
      {
         searchPath += ":";
      }
      
      searchPath += thisPath;
      
      setenv(gVarName, searchPath.c_str(), 1);
   }
}

#endif // alpWindows
