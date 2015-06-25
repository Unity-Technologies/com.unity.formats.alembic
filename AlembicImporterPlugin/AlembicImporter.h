#ifndef AlembicImporter_h
#define AlembicImporter_h

// options:
// graphics device options are relevant only if aiSupportTextureMesh is defined
// 
// #define aiSupportTextureMesh
//  #define aiSupportD3D11
//  #define aiSupportD3D9
//  #define aiSupportOpenGL
// 
// #define aiWithTBB


#include "pch.h"



#ifdef _WIN32
typedef void (__stdcall *aiNodeEnumerator)(aiObject *node, void *userdata);
#else
typedef void (*aiNodeEnumerator)(aiObject *node, void *userdata);
#endif

// to shut up compiler warnings
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


aiCLinkage aiExport aiContext*      aiCreateContext();
aiCLinkage aiExport void            aiDestroyContext(aiContext* ctx);

aiCLinkage aiExport bool            aiLoad(aiContext* ctx, const char *path);
aiCLinkage aiExport void            aiDebugDump(aiContext* ctx);
aiCLinkage aiExport float           aiGetStartTime(aiContext* ctx);
aiCLinkage aiExport float           aiGetEndTime(aiContext* ctx);
aiCLinkage aiExport aiObject*       aiGetTopObject(aiContext* ctx);
aiCLinkage aiExport void            aiWaitTasks(aiContext* ctx);

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userdata);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);
aiCLinkage aiExport uint32_t        aiGetNumChildren(aiObject* obj);
aiCLinkage aiExport void            aiSetCurrentTime(aiObject* obj, float time);
aiCLinkage aiExport void            aiEnableReverseX(aiObject* obj, bool v);
aiCLinkage aiExport void            aiEnableTriangulate(aiObject* obj, bool v);
aiCLinkage aiExport void            aiEnableReverseIndex(aiObject* obj, bool v);

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
aiCLinkage aiExport bool            aiPolyMeshIsTopologyConstantTriangles(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshHasNormals(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshHasUVs(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshHasVelocities(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshIsNormalIndexed(aiObject* obj);
aiCLinkage aiExport bool            aiPolyMeshIsUVIndexed(aiObject* obj);
aiCLinkage aiExport uint32_t        aiPolyMeshGetIndexCount(aiObject* obj);
aiCLinkage aiExport uint32_t        aiPolyMeshGetVertexCount(aiObject* obj);
aiCLinkage aiExport uint32_t        aiPolyMeshGetPeakIndexCount(aiObject* obj);     // トポロジが変化する場合のインデックス/頂点数の最大値
aiCLinkage aiExport uint32_t        aiPolyMeshGetPeakVertexCount(aiObject* obj);    // 
aiCLinkage aiExport void            aiPolyMeshCopyIndices(aiObject* obj, int *dst);
aiCLinkage aiExport void            aiPolyMeshCopyVertices(aiObject* obj, abcV3 *dst);
aiCLinkage aiExport void            aiPolyMeshCopyNormals(aiObject* obj, abcV3 *dst);
aiCLinkage aiExport void            aiPolyMeshCopyUVs(aiObject* obj, abcV2 *dst);
aiCLinkage aiExport bool            aiPolyMeshGetSplitedMeshInfo(aiObject* obj, aiSplitedMeshInfo *o_smi, const aiSplitedMeshInfo *prev, int max_vertices);
aiCLinkage aiExport void            aiPolyMeshCopySplitedIndices(aiObject* obj, int *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshCopySplitedVertices(aiObject* obj, abcV3 *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshCopySplitedNormals(aiObject* obj, abcV3 *dst, const aiSplitedMeshInfo *smi);
aiCLinkage aiExport void            aiPolyMeshCopySplitedUVs(aiObject* obj, abcV2 *dst, const aiSplitedMeshInfo *smi);

#ifdef aiSupportTextureMesh

// Unity の Mesh の vertices や normal の更新は信じられないくらい遅く、これで毎フレームでかいモデルを更新するのは現実的ではない。
// ID3D11Buffer* などのネイティブグラフィック API のリソースを直接更新することで回避したい…、が、
// 現在 Unity がネイティブリソースへのアクセス手段を提供しているものは Texture 一族だけである。
// このため、テクスチャにモデルデータを格納して頂点シェーダで変形させるというややこしい手段を用いる。
// 汚い手段だが Mesh を更新するよりは数十倍のオーダーで速い。

struct aiTextureMeshData
{
    // in
    int tex_width;

    // out
    int index_count;
    int vertex_count;
    int is_normal_indexed;
    int is_uv_indexed;
    int pad;
    void *tex_indices;
    void *tex_vertices;
    void *tex_normals;
    void *tex_uvs;
    void *tex_velocities;
};
aiCLinkage aiExport void            aiPolyMeshCopyToTexture(aiObject* obj, aiTextureMeshData *dst);
aiCLinkage aiExport void            aiPolyMeshBeginCopyToTexture(aiObject* obj, aiTextureMeshData *dst);
aiCLinkage aiExport void            aiPolyMeshEndCopyDataToTexture(aiObject* obj, aiTextureMeshData *dst);

#endif // aiSupportTextureMesh


aiCLinkage aiExport bool            aiHasCurves(aiObject* obj);

aiCLinkage aiExport bool            aiHasPoints(aiObject* obj);

aiCLinkage aiExport bool            aiHasCamera(aiObject* obj);
aiCLinkage aiExport void            aiCameraGetParams(aiObject* obj, aiCameraParams *o_params);

aiCLinkage aiExport bool            aiHasLight(aiObject* obj);

aiCLinkage aiExport bool            aiHasMaterial(aiObject* obj);

#endif // AlembicImporter_h
