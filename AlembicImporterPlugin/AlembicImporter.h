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
    int32_t normalsMode;
    int32_t tangentsMode;
    bool cacheTangentsSplits;
    float aspectRatio;
    bool forceUpdate;
    bool useThreads;
    int32_t cacheSamples;

    inline aiConfig()
        : swapHandedness(true)
        , swapFaceWinding(false)
        , normalsMode(NM_ComputeIfMissing)
        , tangentsMode(TM_None)
        , cacheTangentsSplits(true)
        , aspectRatio(-1.0f)
        , forceUpdate(false)
        , useThreads(true)
        , cacheSamples(0)

    {
    }

    aiConfig(const aiConfig&) = default;
    aiConfig& operator=(const aiConfig&) = default;

    inline std::string toString() const
    {
        std::ostringstream oss;

        oss << "{swapHandedness: " << (swapHandedness ? "true" : "false");
        oss << ", swapFaceWinding: " << (swapFaceWinding ? "true" : "false");
        oss << ", normalsMode: " << (normalsMode == NM_ReadFromFile
                                     ? "read_from_file"
                                     : (normalsMode == NM_ComputeIfMissing
                                        ? "compute_if_missing"
                                        : (normalsMode == NM_AlwaysCompute
                                           ? "always_compute"
                                           : "ignore")));
        oss << ", tangentsMode: " << (tangentsMode == TM_None
                                      ? "none"
                                      : (tangentsMode == TM_Smooth
                                         ? "smooth"
                                         : "split"));
        oss << ", cacheTangentsSplits: " << (cacheTangentsSplits ? "true" : "false");
        oss << ", aspectRatio: " << aspectRatio;
        oss << ", forceUpdate: " << (forceUpdate ? "true" : "false") << "}";
        
        return oss.str();
    }
};

struct aiXFormData
{
    abcV3 translation;
    abcV4 rotation;
    abcV3 scale;
    bool inherits;

    inline aiXFormData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotation(0.0f, 0.0f, 0.0f, 1.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(false)
    {
    }

    aiXFormData(const aiXFormData&) = default;
    aiXFormData& operator=(const aiXFormData&) = default;
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

    aiCameraData(const aiCameraData&) = default;
    aiCameraData& operator=(const aiCameraData&) = default;
};

struct aiMeshSummary
{
    int32_t topologyVariance;

    inline aiMeshSummary()
        : topologyVariance(0)
    {
    }

    aiMeshSummary(const aiMeshSummary&) = default;
    aiMeshSummary& operator=(const aiMeshSummary&) = default;
};

struct aiMeshSampleSummary
{
    int32_t splitCount;
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

    aiMeshSampleSummary(const aiMeshSampleSummary&) = default;
    aiMeshSampleSummary& operator=(const aiMeshSampleSummary&) = default;
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

    aiMeshSampleData(const aiMeshSampleData&) = default;
    aiMeshSampleData& operator=(const aiMeshSampleData&) = default;
};

struct aiSubmeshSummary
{
    int32_t index;
    int32_t splitIndex;
    int32_t splitSubmeshIndex;
    int32_t facesetIndex;
    int32_t triangleCount;

    inline aiSubmeshSummary()
        : index(0)
        , splitIndex(0)
        , splitSubmeshIndex(0)
        , facesetIndex(-1)
        , triangleCount(0)
    {
    }

    aiSubmeshSummary(const aiSubmeshSummary&) = default;
    aiSubmeshSummary& operator=(const aiSubmeshSummary&) = default;
};

struct aiSubmeshData
{
    int32_t *indices;

    inline aiSubmeshData()
        : indices(0)
    {
    }

    aiSubmeshData(const aiSubmeshData&) = default;
    aiSubmeshData& operator=(const aiSubmeshData&) = default;
};

struct aiFacesets
{
    int32_t count;
    int32_t *faceCounts;
    int32_t *faceIndices;

    inline aiFacesets()
        : count(0)
        , faceCounts(0)
        , faceIndices(0)
    {
    }

    aiFacesets(const aiFacesets&) = default;
    aiFacesets& operator=(const aiFacesets&) = default;
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

aiCLinkage aiExport void            aiUpdateSamples(aiContext* ctx, float time);

aiCLinkage aiExport void            aiEnumerateChild(aiObject *obj, aiNodeEnumerator e, void *userData);
aiCLinkage aiExport const char*     aiGetNameS(aiObject* obj);
aiCLinkage aiExport const char*     aiGetFullNameS(aiObject* obj);

aiCLinkage aiExport void            aiSchemaSetSampleCallback(aiSchemaBase* schema, aiSampleCallback cb, void* arg);
aiCLinkage aiExport void            aiSchemaSetConfigCallback(aiSchemaBase* schema, aiConfigCallback cb, void* arg);
aiCLinkage aiExport const aiSampleBase* aiSchemaUpdateSample(aiSchemaBase* schema, float time);
aiCLinkage aiExport const aiSampleBase* aiSchemaGetSample(aiSchemaBase* schema, float time);

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
