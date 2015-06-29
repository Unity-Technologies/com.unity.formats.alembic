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
typedef void(__stdcall *aiNodeEnumerator)(aiObject *node, void *userdata);
typedef void (__stdcall *aiSampleCallback)(void *csobj, aiSampleBase *sample);
#else
typedef void (*aiNodeEnumerator)(aiObject *node, void *userdata);
typedef void(*aiSampleCallback)(void *csobj, aiSampleBase *sample);
#endif


aiCLinkage aiExport void            aiCleanup();

aiCLinkage aiExport aiContext*      aiCreateContext();
aiCLinkage aiExport void            aiDestroyContext(aiContext* ctx);

aiCLinkage aiExport bool            aiLoad(aiContext* ctx, const char *path);
aiCLinkage aiExport void            aiSetImportConfig(aiContext* ctx, const aiImportConfig *conf);
aiCLinkage aiExport void            aiDebugDump(aiContext* ctx);
aiCLinkage aiExport float           aiGetStartTime(aiContext* ctx);
aiCLinkage aiExport float           aiGetEndTime(aiContext* ctx);
aiCLinkage aiExport aiObject*       aiGetTopObject(aiContext* ctx);
aiCLinkage aiExport void            aiUpdateSamples(aiContext* ctx, float time);
aiCLinkage aiExport void            aiUpdateSamplesBegin(aiContext* ctx, float time);
aiCLinkage aiExport void            aiUpdateSamplesEnd(aiContext* ctx);
aiCLinkage aiExport void            aiSetTimeRangeToKeepSamples(aiContext *ctx, float time, float range);
aiCLinkage aiExport void            aiErasePastSamples(aiContext* ctx, float time, float range);

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userdata);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);

aiCLinkage aiExport void            aiSchemaSetCallback(aiSchemaBase* schema, aiSampleCallback cb, void *arg);
aiCLinkage aiExport const aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, float time);
aiCLinkage aiExport const aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, float time);
aiCLinkage aiExport uint32_t        aiSampleGetTime(aiSampleBase* sample);


aiCLinkage aiExport bool            aiHasXForm(aiObject* obj);
aiCLinkage aiExport aiXForm*        aiGetXForm(aiObject* obj);
aiCLinkage aiExport void            aiXFormGetData(aiXFormSample* sample, aiXFormData *o_data);

aiCLinkage aiExport bool            aiHasPolyMesh(aiObject* obj);
aiCLinkage aiExport aiPolyMesh*     aiGetPolyMesh(aiObject* obj);
aiCLinkage aiExport void            aiPolyMeshGetSchemaSummary(aiPolyMesh* schema, aiPolyMeshSchemaSummary *o_summary);
aiCLinkage aiExport void            aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiPolyMeshSampleSummary *o_summary);
aiCLinkage aiExport bool            aiPolyMeshGetSplitedMeshInfo(aiPolyMeshSample* sample, aiSplitedMeshData *o_smi, const aiSplitedMeshData *prev, int max_vertices);
aiCLinkage aiExport void            aiPolyMeshCopySplitedMesh(aiPolyMeshSample* sample, aiSplitedMeshData *smi);
aiCLinkage aiExport void            aiPolyMeshCopyToTexture(aiPolyMeshSample* sample, aiTextureMeshData *dst);

aiCLinkage aiExport bool            aiHasCamera(aiObject* obj);
aiCLinkage aiExport aiCamera*       aiGetCamera(aiObject* obj);
aiCLinkage aiExport void            aiCameraGetData(aiCameraSample* sample, aiCameraData *o_data);

#endif // AlembicImporter_h
