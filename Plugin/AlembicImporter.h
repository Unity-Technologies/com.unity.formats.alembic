#ifndef AlembicImporter_h
#define AlembicImporter_h

#include "pch.h"



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
    void aiDebugLogImpl(const char* fmt, ...);
    #define aiDebugLog(...) aiDebugLogImpl(__VA_ARGS__)
    #ifdef aiWithVerboseDebugLog
        #define aiDebugLogVerbose(...) aiDebugLogImpl(__VA_ARGS__)
    #else
        #define aiDebugLogVerbose(...)
    #endif
#else
    #define aiDebugLog(...)
    #define aiDebugLogVerbose(...)
#endif


typedef Alembic::Abc::V2f       abcV2;
typedef Alembic::Abc::V3f       abcV3;
typedef Alembic::Abc::M44f      abcM44;
typedef Alembic::Abc::IObject   abcObject;
class   aiContext;
typedef aiContext* aiContextPtr;
typedef void(__stdcall *aiMetaDataEnumerator)(aiContextPtr ctx, const char *key, const char *value, void *userdata);
typedef void(__stdcall *aiNodeEnumerator)(aiContextPtr ctx, abcObject *node, void *userdata);
struct aiV2 { float v[2]; };
struct aiV3 { float v[3]; };
struct aiM44 { float v[4][4]; };

struct aiSplitedMeshInfo
{
    int num_faces;
    int num_indices;
    int num_vertices;

    int begin_face;
    int begin_index;
    int triangulated_index_count;
};


aiCLinkage aiExport aiContextPtr    aiCreateContext();
aiCLinkage aiExport void            aiDestroyContext(aiContextPtr ctx);

aiCLinkage aiExport bool            aiLoad(aiContextPtr ctx, const char *path);
aiCLinkage aiExport abcObject*      aiGetTopObject(aiContextPtr ctx);
aiCLinkage aiExport void            aiEnumerateMetaData(aiContextPtr ctx, abcObject *node, aiMetaDataEnumerator e, void *userdata);
aiCLinkage aiExport void            aiEnumerateChild(aiContextPtr ctx, abcObject *node, aiNodeEnumerator e, void *userdata);
aiCLinkage aiExport void            aiSetCurrentObject(aiContextPtr ctx, abcObject *node);
aiCLinkage aiExport void            aiSetCurrentTime(aiContextPtr ctx, float time);
aiCLinkage aiExport void            aiEnableTriangulate(aiContextPtr ctx, bool v);
aiCLinkage aiExport void            aiEnableReverseIndex(aiContextPtr ctx, bool v);

aiCLinkage aiExport const char*     aiGetNameS(aiContextPtr ctx);
aiCLinkage aiExport const char*     aiGetFullNameS(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetNumChildren(aiContextPtr ctx);

aiCLinkage aiExport bool            aiHasXForm(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiGetPosition(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiGetRotation(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiGetScale(aiContextPtr ctx);
aiCLinkage aiExport aiM44           aiGetMatrix(aiContextPtr ctx);

aiCLinkage aiExport bool            aiHasPolyMesh(aiContextPtr ctx);
aiCLinkage aiExport bool            aiIsNormalsIndexed(aiContextPtr ctx);
aiCLinkage aiExport bool            aiIsUVsIndexed(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetIndexCount(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetVertexCount(aiContextPtr ctx);
aiCLinkage aiExport void            aiCopyIndices(aiContextPtr ctx, int *dst);
aiCLinkage aiExport void            aiCopyVertices(aiContextPtr ctx, abcV3 *dst);
aiCLinkage aiExport bool            aiGetSplitedMeshInfo(aiContextPtr ctx, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices);
aiCLinkage aiExport void            aiCopySplitedIndices(aiContextPtr ctx, int *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiCopySplitedVertices(aiContextPtr ctx, abcV3 *dst, const aiSplitedMeshInfo *smi);

#endif // AlembicImporter_h
