#ifndef AlembicImporter_h
#define AlembicImporter_h

#include "pch.h"

#define aiWithDebugLog


#ifdef _WIN32
    #define aiWindows
#endif // _WIN32

#define aiCLinkage extern "C"
#ifdef _MSC_VER
    #define aiExport __declspec(dllexport)
#else
    #define aiExport
#endif

#ifdef aiWithDebugLog
void aiDebugLog(const char* fmt, ...);
#else  // aiWithDebugLog
#define aiDebugLog(...)
#endif // aiWithDebugLog


typedef Alembic::Abc::IObject   abcObject;
typedef Alembic::Abc::M44f      abcM44;
typedef Alembic::Abc::V3f       abcV3;
class   aiContext;
typedef aiContext* aiContextPtr;
typedef void(__stdcall *aiNodeEnumerator)(aiContextPtr ctx, abcObject *node, void *userdata);
struct aiV3 { float v[3]; };
struct aiM44 { float v[4][4]; };


aiCLinkage aiExport aiContextPtr    aiCreateContext();
aiCLinkage aiExport void            aiDestroyContext(aiContextPtr ctx);

aiCLinkage aiExport bool            aiLoad(aiContextPtr ctx, const char *path);
aiCLinkage aiExport abcObject*      aiGetTopObject(aiContextPtr ctx);
aiCLinkage aiExport void            aiEnumerateChild(aiContextPtr ctx, abcObject *node, aiNodeEnumerator e, void *userdata);
aiCLinkage aiExport void            aiSetCurrentObject(aiContextPtr ctx, abcObject *node);

aiCLinkage aiExport const char*     aiGetNameS(aiContextPtr ctx);
aiCLinkage aiExport const char*     aiGetFullNameS(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetNumChildren(aiContextPtr ctx);
aiCLinkage aiExport bool            aiHasXForm(aiContextPtr ctx);
aiCLinkage aiExport bool            aiHasPolyMesh(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiGetPosition(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiGetRotation(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiGetScale(aiContextPtr ctx);
aiCLinkage aiExport aiM44           aiGetMatrix(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetVertexCount(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetIndexCount(aiContextPtr ctx);
aiCLinkage aiExport void            aiCopyVertices(aiContextPtr ctx, abcV3 *vertices);
aiCLinkage aiExport void            aiCopyIndices(aiContextPtr ctx, int *indices, bool reverse);

#endif // AlembicImporter_h
