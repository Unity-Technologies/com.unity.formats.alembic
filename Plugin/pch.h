#include <algorithm>
#include <map>
#include <set>
#include <vector>
#include <deque>
#include <memory>
#include <thread>
#include <mutex>
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

#define aiImpl
#define aeImpl
#define aeDebugLog(...) 



#if defined(aiDebug) || defined(aiDebugLog)
    #define DebugLog(...) aiLogger::Debug(__VA_ARGS__)
#else
    #define DebugLog(...)
#endif

#ifdef _WIN32
#include <windows.h>

#ifndef aiNoAutoLink
#pragma comment(lib, "AlembicAbc.lib")
#pragma comment(lib, "AlembicAbcCollection.lib")
#pragma comment(lib, "AlembicAbcCoreAbstract.lib")
#pragma comment(lib, "AlembicAbcCoreFactory.lib")
#pragma comment(lib, "AlembicAbcCoreHDF5.lib")
#pragma comment(lib, "AlembicAbcCoreOgawa.lib")
#pragma comment(lib, "AlembicAbcGeom.lib")
#pragma comment(lib, "AlembicAbcMaterial.lib")
#pragma comment(lib, "AlembicOgawa.lib")
#pragma comment(lib, "AlembicUtil.lib")
#pragma comment(lib, "libhdf5.lib")
#pragma comment(lib, "libhdf5_hl.lib")
#pragma comment(lib, "Half.lib")
#pragma comment(lib, "Iex-2_2.lib")
#pragma comment(lib, "IexMath-2_2.lib")
#endif // aiNoAutoLink

#ifdef aiSupportD3D11
#include <d3d11.h>
#endif // aiSupportD3D11

#ifdef aiSupportD3D9
#include <d3d9.h>
#endif // aiSupportD3D9

#endif // _WIN32

using namespace Alembic;

#define aiPI 3.14159265f

typedef Imath::V2f      abcV2;
typedef Imath::V3f      abcV3;
typedef Imath::V4f      abcV4;
typedef Imath::M44f     abcM44;
typedef Imath::M44d     abcM44d;
typedef Imath::Box3f    abcBox;
typedef Imath::Box3d    abcBoxd;
typedef Abc::chrono_t   abcChrono;
typedef Abc::ISampleSelector abcSampleSelector;
