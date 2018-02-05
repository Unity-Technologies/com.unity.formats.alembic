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
#include <type_traits>
#include <Alembic/AbcCoreAbstract/All.h>
#include <Alembic/AbcCoreHDF5/All.h>
#include <Alembic/AbcCoreOgawa/All.h>
#include <Alembic/Abc/ErrorHandler.h>
#include <Alembic/AbcGeom/All.h>
#include <Alembic/AbcMaterial/All.h>

#define abciImpl
#define abciDebugLog(...) 

#if defined(abciDebug) || defined(abciDebugLog)
    #define DebugLog(...) aiLogger::Debug(__VA_ARGS__)
    #define DebugWarning(...) aiLogger::Warning(__VA_ARGS__)
    #define DebugError(...) aiLogger::Error(__VA_ARGS__)
#else
    #define DebugLog(...)
#define DebugWarning(...)
#define DebugError(...)
#endif

#ifdef _WIN32
    #define NOMINMAX
    #include <windows.h>
    #include <ppl.h>
    #pragma warning(disable: 4996)
    #pragma warning(disable: 4190)
#endif // _WIN32

using namespace Alembic;

#define aiPI 3.14159265f

using abcV2 = Imath::V2f;
using abcV3 = Imath::V3f;
using abcV3d = Imath::V3d;
using abcV4 = Imath::V4f;
using abcC4 = Imath::C4f;
using abcM44 = Imath::M44f;
using abcM44d = Imath::M44d;
using abcBox = Imath::Box3f;
using abcBoxd = Imath::Box3d;
using abcChrono = Abc::chrono_t;
using abcSampleSelector = Abc::ISampleSelector;
