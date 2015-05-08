#ifndef UnityAlembicImporter_h
#define UnityAlembicImporter_h

#include "pch.h"

#define uaiWithDebugLog


#ifdef _WIN32
    #define uaiWindows
#endif // _WIN32

#define uaiCLinkage extern "C"
#ifdef _MSC_VER
    #define uaiExport __declspec(dllexport)
#else
    #define uaiExport
#endif

#ifdef uaiWithDebugLog
void uaiDebugLog(const char* fmt, ...);
#else  // uaiWithDebugLog
#define uaiDebugLog(...)
#endif // uaiWithDebugLog


typedef Alembic::Abc::IObject   abcObject;
typedef Alembic::Abc::M44f      abcM44;
typedef Alembic::Abc::V3f       abcV3;
typedef void(__stdcall *uaiNodeEnumerator)(int, abcObject*);
struct uaiV3 { float v[3]; };
struct uaiM44 { float v[4][4]; };


uaiCLinkage uaiExport int           uaiCreateContext();
uaiCLinkage uaiExport void          uaiDestroyContext(int ctx);

uaiCLinkage uaiExport bool          uaiLoad(int ctx, const char *path);
uaiCLinkage uaiExport abcObject*    uaiGetTopObject(int ctx);
uaiCLinkage uaiExport void          uaiEnumerateChild(int ctx, abcObject *obj, uaiNodeEnumerator e);
uaiCLinkage uaiExport void          uaiSetCurrentObject(int ctx, abcObject *obj);

uaiCLinkage uaiExport const char*   uaiGetName(int ctx);
uaiCLinkage uaiExport const char*   uaiGetFullName(int ctx);
uaiCLinkage uaiExport uint32_t      uaiGetNumChildren(int ctx);
uaiCLinkage uaiExport bool          uaiHasXForm(int ctx);
uaiCLinkage uaiExport bool          uaiIsPolyMesh(int ctx);
uaiCLinkage uaiExport uaiV3         uaiGetPosition(int ctx);
uaiCLinkage uaiExport uaiV3         uaiGetRotation(int ctx);
uaiCLinkage uaiExport uaiV3         uaiGetScale(int ctx);
uaiCLinkage uaiExport uaiM44        uaiGetMatrix(int ctx);
uaiCLinkage uaiExport uint32_t      uaiGetVertexCount(int ctx);
uaiCLinkage uaiExport uint32_t      uaiGetIndexCount(int ctx);
uaiCLinkage uaiExport void          uaiCopyMeshData(int ctx, abcV3 *vertices, int *indices);

#endif // UnityAlembicImporter_h
