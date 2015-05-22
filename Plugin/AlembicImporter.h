#ifndef AlembicImporter_h
#define AlembicImporter_h

#include "pch.h"




using namespace Alembic;

typedef Abc::V2f       abcV2;
typedef Abc::V3f       abcV3;
typedef Abc::M44f      abcM44;
typedef Abc::IObject   abcObject;
struct  aiCameraParams;
class   aiContext;
typedef aiContext* aiContextPtr;
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
aiCLinkage aiExport void            aiEnumerateChild(aiContextPtr ctx, abcObject *node, aiNodeEnumerator e, void *userdata);
aiCLinkage aiExport void            aiSetCurrentObject(aiContextPtr ctx, abcObject *node);
aiCLinkage aiExport void            aiSetCurrentTime(aiContextPtr ctx, float time);
aiCLinkage aiExport void            aiEnableReverseX(aiContextPtr ctx, bool v);
aiCLinkage aiExport void            aiEnableTriangulate(aiContextPtr ctx, bool v);
aiCLinkage aiExport void            aiEnableReverseIndex(aiContextPtr ctx, bool v);

aiCLinkage aiExport const char*     aiGetNameS(aiContextPtr ctx);
aiCLinkage aiExport const char*     aiGetFullNameS(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiGetNumChildren(aiContextPtr ctx);

aiCLinkage aiExport bool            aiHasXForm(aiContextPtr ctx);
aiCLinkage aiExport bool            aiXFormGetInherits(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiXFormGetPosition(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiXFormGetAxis(aiContextPtr ctx);
aiCLinkage aiExport float           aiXFormGetAngle(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiXFormGetRotation(aiContextPtr ctx);
aiCLinkage aiExport aiV3            aiXFormGetScale(aiContextPtr ctx);
aiCLinkage aiExport aiM44           aiXFormGetMatrix(aiContextPtr ctx);

aiCLinkage aiExport bool            aiHasPolyMesh(aiContextPtr ctx);
aiCLinkage aiExport bool            aiPolyMeshIsTopologyConstant(aiContextPtr ctx);
aiCLinkage aiExport bool            aiPolyMeshIsTopologyConstantTriangles(aiContextPtr ctx);
aiCLinkage aiExport bool            aiPolyMeshHasNormals(aiContextPtr ctx);
aiCLinkage aiExport bool            aiPolyMeshHasUVs(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiPolyMeshGetIndexCount(aiContextPtr ctx);
aiCLinkage aiExport uint32_t        aiPolyMeshGetVertexCount(aiContextPtr ctx);
aiCLinkage aiExport void            aiPolyMeshCopyIndices(aiContextPtr ctx, int *dst);
aiCLinkage aiExport void            aiPolyMeshCopyVertices(aiContextPtr ctx, abcV3 *dst);
aiCLinkage aiExport void            aiPolyMeshCopyNormals(aiContextPtr ctx, abcV3 *dst);
aiCLinkage aiExport void            aiPolyMeshCopyUVs(aiContextPtr ctx, abcV2 *dst);
aiCLinkage aiExport bool            aiPolyMeshGetSplitedMeshInfo(aiContextPtr ctx, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices);
aiCLinkage aiExport void            aiPolyMeshCopySplitedIndices(aiContextPtr ctx, int *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshCopySplitedVertices(aiContextPtr ctx, abcV3 *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshCopySplitedNormals(aiContextPtr ctx, abcV3 *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshCopySplitedUVs(aiContextPtr ctx, abcV2 *dst, const aiSplitedMeshInfo *smi);

aiCLinkage aiExport bool            aiHasCurves(aiContextPtr ctx);

aiCLinkage aiExport bool            aiHasPoints(aiContextPtr ctx);

aiCLinkage aiExport bool            aiHasCamera(aiContextPtr ctx);
aiCLinkage aiExport void            aiCameraGetParams(aiContextPtr ctx, aiCameraParams *o_params);

aiCLinkage aiExport bool            aiHasMaterial(aiContextPtr ctx);

#endif // AlembicImporter_h
