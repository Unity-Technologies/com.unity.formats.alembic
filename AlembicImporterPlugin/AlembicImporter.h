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

enum aiNormalsMode
{
    NM_ReadFromFile = 0,
    NM_ComputeIfMissing,
    NM_AlwaysCompute,
    NM_Ignore
};
    
enum aiTangentsMode
{
    TM_None = 0,
    TM_Smooth,
    TM_Split
};

struct aiConfig
{
    bool swapHandedness;
    bool swapFaceWinding;
    aiNormalsMode normalsMode;
    aiTangentsMode tangentsMode;
    bool cacheTangentsSplits;
    float aspectRatio;
    bool forceUpdate;

    inline aiConfig()
        : swapHandedness(true)
        , swapFaceWinding(false)
        , normalsMode(NM_ComputeIfMissing)
        , tangentsMode(TM_None)
        , cacheTangentsSplits(true)
        , aspectRatio(-1.0f)
        , forceUpdate(false)
    {
    }
};

struct aiXFormData
{
    abcV3 translation;
    abcV4 rotation;
    abcV3 scale;
    bool inherits;

    inline aiXFormData()
        : translation(0.0f)
        , rotation(0.0f)
        , scale(1.0f)
        , inherits(false)
    {
    }
};

struct aiCameraData
{
    float nearClippingPlane;
    float farClippingPlane;
    float fieldOfView;
    float focusDistance;
    float focalLength;

    inline aiCameraData()
        : nearClippingPlane(0.0f)
        , farClippingPlane(0.0f)
        , fieldOfView(0.0f)
        , focusDistance(0.0f)
        , focalLength(0.0f)
    {
    }
};

struct aiMeshSummary
{
    int topologyVariance;
    uint32_t peakIndexCount;
    uint32_t peakVertexCount;

    inline aiMeshSummary()
        : topologyVariance(0)
        , peakIndexCount(0)
        , peakVertexCount(0)
    {
    }
};

struct aiMeshSampleSummary
{
    uint32_t splitCount;
    bool hasNormals;
    bool hasUVs;
    bool hasTangents;

    inline aiMeshSampleSummary()
        : splitCount(0)
        , hasNormals(false)
        , hasUVs(false)
        , hasTangents(false)
    {
    }
};

struct aiMeshSampleData
{
    abcV3 *positions;
    abcV3 *normals;
    abcV2 *uvs;
    abcV4 *tangents;

    inline aiMeshSampleData()
        : positions(0)
        , normals(0)
        , uvs(0)
        , tangents(0)
    {
    }
};

struct aiSubmeshSummary
{
    uint32_t index;
    uint32_t splitIndex;
    uint32_t splitSubmeshIndex;
    int32_t facesetIndex;
    uint32_t triangleCount;

    inline aiSubmeshSummary()
        : index(0)
        , splitIndex(0)
        , splitSubmeshIndex(0)
        , facesetIndex(-1)
        , triangleCount(0)
    {
    }
};

struct aiSubmeshData
{
    int *indices;

    inline aiSubmeshData()
        : indices(0)
    {
    }
};

struct aiFacesets
{
    int count;
    int *faceCounts;
    int *faceIndices;

    inline aiFacesets()
        : count(0)
        , faceCounts(0)
        , faceIndices(0)
    {
    }
};


#ifdef _WIN32
typedef void (__stdcall *aiNodeEnumerator)(aiObject *node, void *userData);
typedef void (__stdcall *aiConfigCallback)(void *csObj, aiConfig *config);
typedef void (__stdcall *aiSampleCallback)(void *csObj, aiSampleBase *sample, bool topologyChanged);
#else
typedef void (*aiNodeEnumerator)(aiObject *node, void *userData);
typedef void (*aiConfigCallback)(void *csObj, aiConfig *config);
typedef void (*aiSampleCallback)(void *csObj, aiSampleBase *sample, bool topologyChanged);
#endif


aiCLinkage aiExport void            aiEnableFileLog(bool on, const char *path);

aiCLinkage aiExport void            aiCleanup();
aiCLinkage aiExport aiContext*      aiCreateContext(int uid);
aiCLinkage aiExport void            aiDestroyContext(aiContext* ctx);

aiCLinkage aiExport bool            aiLoad(aiContext* ctx, const char *path);
aiCLinkage aiExport void            aiSetConfig(aiContext* ctx, const aiConfig* conf);
aiCLinkage aiExport float           aiGetStartTime(aiContext* ctx);
aiCLinkage aiExport float           aiGetEndTime(aiContext* ctx);
aiCLinkage aiExport aiObject*       aiGetTopObject(aiContext* ctx);
aiCLinkage aiExport void            aiDestroyObject(aiContext* ctx, aiObject* obj);

aiCLinkage aiExport void            aiUpdateSamples(aiContext* ctx, float time, bool useThreads);
aiCLinkage aiExport void            aiSetTimeRangeToKeepSamples(aiContext* ctx, float time, float range);
aiCLinkage aiExport void            aiErasePastSamples(aiContext* ctx, float time, float range);
aiCLinkage aiExport void            aiUpdateSamplesBegin(aiContext* ctx, float time);
aiCLinkage aiExport void            aiUpdateSamplesEnd(aiContext* ctx);

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);

aiCLinkage aiExport void            aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg);
aiCLinkage aiExport void            aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg);
aiCLinkage aiExport const aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, float time);
aiCLinkage aiExport const aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, float time);
aiCLinkage aiExport float           aiSampleGetTime(aiSampleBase* sample);

aiCLinkage aiExport bool            aiHasXForm(aiObject* obj);
aiCLinkage aiExport aiXForm*        aiGetXForm(aiObject* obj);
aiCLinkage aiExport void            aiXFormGetData(aiXFormSample* sample, aiXFormData *outData);

aiCLinkage aiExport bool            aiHasPolyMesh(aiObject* obj);
aiCLinkage aiExport aiPolyMesh*     aiGetPolyMesh(aiObject* obj);
aiCLinkage aiExport void            aiPolyMeshGetSummary(aiPolyMesh* schema, aiMeshSummary* summary);
aiCLinkage aiExport void            aiPolyMeshGetSampleSummary(aiPolyMeshSample* sample, aiMeshSampleSummary* summary, bool forceRefresh);
aiCLinkage aiExport int             aiPolyMeshGetVertexBufferLength(aiPolyMeshSample* sample, int splitIndex);
aiCLinkage aiExport void            aiPolyMeshFillVertexBuffer(aiPolyMeshSample* sample, int splitIndex, aiMeshSampleData* data);
aiCLinkage aiExport int             aiPolyMeshPrepareSubmeshes(aiPolyMeshSample* sample, const aiFacesets* facesets);
aiCLinkage aiExport int             aiPolyMeshGetSplitSubmeshCount(aiPolyMeshSample* sample, int splitIndex);
aiCLinkage aiExport bool            aiPolyMeshGetNextSubmesh(aiPolyMeshSample* sample, aiSubmeshSummary* summary);
aiCLinkage aiExport void            aiPolyMeshFillSubmeshIndices(aiPolyMeshSample* sample, const aiSubmeshSummary* summary, aiSubmeshData* data);

aiCLinkage aiExport bool            aiHasCamera(aiObject* obj);
aiCLinkage aiExport aiCamera*       aiGetCamera(aiObject* obj);
aiCLinkage aiExport void            aiCameraGetData(aiCameraSample* sample, aiCameraData *outData);

#endif // AlembicImporter_h
