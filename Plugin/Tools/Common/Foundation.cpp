#include "pch.h"
#include "Foundation.h"


// logging
#ifdef _WIN32
    #define tvsnprintf(buf, count, format, va)    _vsnprintf(buf, count, format, va)
#else
    #define tvsnprintf(buf, count, format, va)    vsnprintf(buf, count, format, va)
#endif

void __stdcall tDefaultLogCallback(const char *message) { printf(message); }
tLogCallback g_log_callback = tDefaultLogCallback;

void tLogSetCallback(tLogCallback cb)
{
    g_log_callback = cb;
}

void tLog(const char *format, ...)
{
    const int Len = 1024 * 2;
    char buf[Len];
    va_list vl;
    va_start(vl, format);
    int r = tvsnprintf(buf, Len, format, vl);
    va_end(vl);
    if (g_log_callback) { g_log_callback(buf); }
}


// dll related

void tAddDLLSearchPath(const char *path_to_add)
{
#ifdef _WIN32
    std::string path;
    path.resize(1024 * 64);

    DWORD ret = ::GetEnvironmentVariableA("PATH", &path[0], (DWORD)path.size());
    path.resize(ret);
    path += ";";
    path += path_to_add;
    ::SetEnvironmentVariableA("PATH", path.c_str());

#else 
    std::string path = getenv("LD_LIBRARY_PATH");
    path += ";";
    path += path_to_add;
    setenv("LD_LIBRARY_PATH", path.c_str(), 1);
#endif
}

const char* tGetDirectoryOfCurrentModule()
{
    static std::string s_path;
    if (s_path.empty()) {
#ifdef _WIN32
        char buf[MAX_PATH];
        HMODULE mod = 0;
        ::GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCSTR)&tGetDirectoryOfCurrentModule, &mod);
        DWORD size = ::GetModuleFileNameA(mod, buf, sizeof(buf));
        for (int i = size - 1; i >= 0; --i) {
            if (buf[i] == '\\') {
                buf[i] = '\0';
                s_path = buf;
                break;
            }
        }
#else 
    // todo:
#endif
    }
    return s_path.c_str();
}


// timer

double tGetTime()
{
    using namespace std::chrono;
    return (double)duration_cast<nanoseconds>(steady_clock::now().time_since_epoch()).count() * 1e-6f;
}


// random

std::mt19937 g_rand;

void tRandSetSeed(uint32_t seed)
{
    g_rand.seed(seed);
}

float tRand()
{
    static std::uniform_real_distribution<float> s_dist(-1.0f, 1.0f);
    return s_dist(g_rand);
}

float3 tRand3()
{
    return float3(tRand(), tRand(), tRand());
}
