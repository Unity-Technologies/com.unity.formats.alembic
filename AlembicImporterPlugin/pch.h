#include <algorithm>
#include <map>
#include <memory>
#include <thread>
#include <mutex>
#include <functional>
#include <Alembic/AbcCoreAbstract/All.h>
#include <Alembic/AbcCoreHDF5/All.h>
#include <Alembic/AbcCoreOgawa/All.h>
#include <Alembic/Abc/ErrorHandler.h>
#include <Alembic/AbcGeom/All.h>
#include <Alembic/AbcMaterial/All.h>

#ifdef _WIN32
#define aiWindows
#endif // _WIN32

#define aiCLinkage extern "C"
#ifdef _MSC_VER
#define aiExport __declspec(dllexport)
#else
#define aiExport __attribute__((visibility("default")))
#endif

#ifdef aiDebug
void aiDebugLogImpl(const char* fmt, ...);
#define aiDebugLog(...) aiDebugLogImpl(__VA_ARGS__)
#ifdef aiVerboseDebug
#define aiDebugLogVerbose(...) aiDebugLogImpl(__VA_ARGS__)
#else
#define aiDebugLogVerbose(...)
#endif
#else
#define aiDebugLog(...)
#define aiDebugLogVerbose(...)
#endif


#ifdef aiWindows
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

#endif // aiWindows

using namespace Alembic;

typedef Imath::V2f      abcV2;
typedef Imath::V3f      abcV3;
typedef Imath::V4f      abcV4;
typedef Imath::M44f     abcM44;
typedef Abc::IObject    abcObject;
struct  aiCameraParams;
class   aiObject;
class   aiXForm;
class   aiPolyMesh;
class   aiPoints;
class   aiCurves;
class   aiCamera;
class   aiMaterial;
class   aiContext;
