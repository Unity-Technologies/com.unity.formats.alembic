#ifndef AlembicImporter_h
#define AlembicImporter_h

//// options
//#define aiWithTBB

#include "pch.h"



#ifdef _WIN32
typedef void (__stdcall *aiNodeEnumerator)(aiObject *node, void *userData);
#else
typedef void (*aiNodeEnumerator)(aiObject *node, void *userData);
#endif

struct aiV2 { float v[2]; };
struct aiV3 { float v[3]; };
struct aiM44 { float v[4][4]; };


struct aiSubmeshInfo
{
    int index;
    int splitIndex;
    int splitSubmeshIndex;
    int triangleCount;
    int facesetIndex;
};

struct aiFacesets
{
    int count;
    int *faceCounts;
    int *faceIndices;
};

aiCLinkage aiExport void            aiEnableFileLog(bool on, const char *path);

aiCLinkage aiExport aiContext*      aiCreateContext(int uid);
aiCLinkage aiExport void            aiDestroyContext(aiContext* ctx);

aiCLinkage aiExport bool            aiLoad(aiContext* ctx, const char *path);
aiCLinkage aiExport float           aiGetStartTime(aiContext* ctx);
aiCLinkage aiExport float           aiGetEndTime(aiContext* ctx);
aiCLinkage aiExport aiObject*       aiGetTopObject(aiContext* ctx);

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);
aiCLinkage aiExport uint32_t        aiGetNumChildren(aiObject* obj);
aiCLinkage aiExport void            aiSetCurrentTime(aiObject* obj, float time);

aiCLinkage aiExport void            aiEnableTriangulate(aiObject* obj, bool v);

aiCLinkage aiExport void            aiSwapHandedness(aiObject* obj, bool v);
aiCLinkage aiExport bool            aiIsHandednessSwapped(aiObject* obj);

aiCLinkage aiExport void            aiSwapFaceWinding(aiObject* obj, bool v);
aiCLinkage aiExport bool            aiIsFaceWindingSwapped(aiObject* obj);

aiCLinkage aiExport void            aiSetNormalsMode(aiObject* obj, int m);
aiCLinkage aiExport int             aiGetNormalsMode(aiObject* obj);

aiCLinkage aiExport void            aiSetTangentsMode(aiObject* obj, int m);
aiCLinkage aiExport int             aiGetTangentsMode(aiObject* obj);

aiCLinkage aiExport void            aiCacheTangentsSplits(aiObject* obj, bool v);
aiCLinkage aiExport bool            aiAreTangentsSplitsCached(aiObject* obj);

aiCLinkage aiExport bool            aiHasXForm(aiObject* obj);
aiCLinkage aiExport bool            aiXFormGetInherits(aiObject* obj);
aiCLinkage aiExport aiV3            aiXFormGetPosition(aiObject* obj);
aiCLinkage aiExport aiV3            aiXFormGetAxis(aiObject* obj);
aiCLinkage aiExport float           aiXFormGetAngle(aiObject* obj);
aiCLinkage aiExport aiV3            aiXFormGetRotation(aiObject* obj);
aiCLinkage aiExport aiV3            aiXFormGetScale(aiObject* obj);
aiCLinkage aiExport aiM44           aiXFormGetMatrix(aiObject* obj);

aiCLinkage aiExport bool            aiHasPolyMesh(aiObject* obj);
aiCLinkage aiExport int             aiPolyMeshGetTopologyVariance(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshHasNormals(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshHasUVs(aiObject* obj);
aiCLinkage aiExport uint32_t        aiPolyMeshGetSplitCount(aiObject *obj, bool force_refresh);
aiCLinkage aiExport uint32_t        aiPolyMeshGetVertexBufferLength(aiObject* obj, uint32_t splitIndex);
aiCLinkage aiExport void            aiPolyMeshFillVertexBuffer(aiObject* obj, uint32_t splitIndex, abcV3 *positions, abcV3 *normals, abcV2 *uvs, abcV4 *tangents);
aiCLinkage aiExport uint32_t        aiPolyMeshPrepareSubmeshes(aiObject* obj, const aiFacesets* facesets);
aiCLinkage aiExport uint32_t        aiPolyMeshGetSplitSubmeshCount(aiObject* obj, uint32_t splitIndex);
aiCLinkage aiExport bool            aiPolyMeshGetNextSubmesh(aiObject* obj, aiSubmeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshFillSubmeshIndices(aiObject* obj, int *dst, const aiSubmeshInfo *smi);

aiCLinkage aiExport bool            aiHasCamera(aiObject* obj);
aiCLinkage aiExport void            aiCameraGetParams(aiObject* obj, aiCameraParams *params);

#endif // AlembicImporter_h
