#include <algorithm>
#include <map>
#include <set>
#include <vector>
#include <deque>
#include <memory>
#include <thread>
#include <mutex>
#include <condition_variable>
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
#else
    #define DebugLog(...)
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

typedef Imath::V2f      abcV2;
typedef Imath::V3f      abcV3;
typedef Imath::V3d      abcV3d;
typedef Imath::V4f      abcV4;
typedef Imath::M44f     abcM44;
typedef Imath::M44d     abcM44d;
typedef Imath::Box3f    abcBox;
typedef Imath::Box3d    abcBoxd;
typedef Abc::chrono_t   abcChrono;
typedef Abc::ISampleSelector abcSampleSelector;
