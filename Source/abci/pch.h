#include <algorithm>
#include <numeric>
#include <map>
#include <set>
#include <vector>
#include <deque>
#include <memory>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <future>
#include <functional>
#include <limits>
#include <sstream>
#include <fstream>
#include <type_traits>
#include <locale>
#include <codecvt>
#include <Alembic/AbcCoreAbstract/All.h>
#include <Alembic/AbcCoreOgawa/All.h>
#include <Alembic/Abc/ErrorHandler.h>
#include <Alembic/AbcGeom/All.h>
#include <Alembic/AbcMaterial/All.h>

#define abciImpl

#if defined(aiDebug)
void aiLogPrint(const char* fmt, ...);
void aiLogPrint(const wchar_t* fmt, ...);
    #define DebugLog(...)       aiLogPrint("abci Log: " __VA_ARGS__)
    #define DebugWarning(...)   aiLogPrint("abci Warning: " __VA_ARGS__)
    #define DebugError(...)     aiLogPrint("abci Error: "  __VA_ARGS__)
    #define DebugLogW(...)      aiLogPrint(L"abci Log: " __VA_ARGS__)
    #define DebugWarningW(...)  aiLogPrint(L"abci Warning: " __VA_ARGS__)
    #define DebugErrorW(...)    aiLogPrint(L"abci Error: "  __VA_ARGS__)
#else
    #define DebugLog(...)
    #define DebugWarning(...)
    #define DebugError(...)
    #define DebugLogW(...)
    #define DebugWarningW(...)
    #define DebugErrorW(...)
#endif

#ifdef _WIN32
    #define NOMINMAX
    #include <windows.h>
    #include <ppl.h>
    #pragma warning(disable: 4996)
    #pragma warning(disable: 4190)
#endif // _WIN32

using namespace Alembic;

using abcV2 = Imath::V2f;
using abcV3 = Imath::V3f;
using abcV4 = Imath::V4f;
using abcC3 = Imath::C3f;
using abcC4 = Imath::C4f;
using abcM44 = Imath::M44f;
using abcM44d = Imath::M44d;
using abcBoxd = Imath::Box3d;
using abcChrono = Abc::chrono_t;
using abcSampleSelector = Abc::ISampleSelector;
