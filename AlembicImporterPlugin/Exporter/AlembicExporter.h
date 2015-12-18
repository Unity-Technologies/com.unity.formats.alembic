#ifndef AlembicExporter_h
#define AlembicExporter_h

#define aeCLinkage extern "C"

#ifdef aeImpl
    #ifdef _MSC_VER
        #define aeExport __declspec(dllexport)
    #else
        #define aeExport __attribute__((visibility("default")))
    #endif
#else
    #ifdef _MSC_VER
        #define aeExport __declspec(dllimport)
        #pragma commnet(lib, "AlembicExporter.lib")
    #else
    #endif

    struct abcV2
    {
        float x, y;

        abcV2() {}
        abcV2(float _x, float _y) : x(_x), y(_y) {}
    };

    struct abcV3
    {
        float x, y, x;

        abcV2() {}
        abcV2(float _x, float _y, float _z) : x(_x), y(_y), z(_z) {}
     };
#endif

struct aeConfig;

class aeContext;
class aeObject;
class aeXForm;
class aePoints;
class aePolyMesh;
class aeCamera;
struct aeXFormSampleData;
struct aePointsSampleData;
struct aePolyMeshSampleData;
struct aeCameraSampleData;


enum aeArchiveType
{
    aeArchiveType_HDF5,
    aeArchiveType_Ogawa,
};

enum aeTimeSamplingType
{
    aeTimeSamplingType_Uniform,
    aeTimeSamplingType_Acyclic,
};

struct aeConfig
{
    aeArchiveType archiveType;
    aeTimeSamplingType timeSamplingType;
    float startTime;     // relevant only if timeSamplingType is uniform
    float timePerSample; // relevant only if timeSamplingType is uniform
    bool swapHandedness; // swap rhs <-> lhs

    aeConfig()
        : archiveType(aeArchiveType_Ogawa)
        , timeSamplingType(aeTimeSamplingType_Uniform)
        , startTime(0.0f)
        , timePerSample(1.0f / 30.0f)
        , swapHandedness(true)
    {
    }
};


struct aeXFormSampleData
{
    abcV3 translation;
    abcV3 rotationAxis;
    float rotationAngle;
    abcV3 scale;
    bool inherits;

    inline aeXFormSampleData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotationAxis(0.0f, 1.0f, 0.0f)
        , rotationAngle(0.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(false)
    {
    }
};

struct aePolyMeshSampleData
{
    abcV3 *positions;
    abcV3 *normals; // can be null
    abcV2 *uvs; // can be null
    int *indices;
    int *faces; // can be null. assume all faces are triangles if null

    int vertexCount;
    int indexCount;
    int faceCount;

    aePolyMeshSampleData()
        : positions(nullptr)
        , normals(nullptr)
        , uvs(nullptr)
        , indices(nullptr)
        , faces(nullptr)
        , vertexCount(0)
        , indexCount(0)
        , faceCount(0)
    {
    }
};

struct aePointsSampleData
{
    abcV3 *positions;
    int count;

    inline aePointsSampleData()
        : positions(nullptr)
        , count(0)
    {
    }
};

struct aeCameraSampleData
{
    float nearClippingPlane;
    float farClippingPlane;
    float fieldOfView;
    float focusDistance;
    float focalLength;

    inline aeCameraSampleData()
        : nearClippingPlane(0.0f)
        , farClippingPlane(0.0f)
        , fieldOfView(0.0f)
        , focusDistance(0.0f)
        , focalLength(0.0f)
    {
    }
};



aeCLinkage aeExport aeContext*      aeCreateContext(const aeConfig *conf);
aeCLinkage aeExport void            aeDestroyContext(aeContext* ctx);

aeCLinkage aeExport bool            aeOpenArchive(aeContext* ctx, const char *path);
aeCLinkage aeExport aeObject*       aeGetTopObject(aeContext* ctx);
aeCLinkage aeExport void            aeAddTime(aeContext* ctx, float time); // relevant only if timeSamplingType is acyclic

aeCLinkage aeExport aeXForm*        aeNewXForm(aeObject *parent, const char *name);
aeCLinkage aeExport aePoints*       aeNewPoints(aeObject *parent, const char *name);
aeCLinkage aeExport aePolyMesh*     aeNewPolyMesh(aeObject *parent, const char *name);
aeCLinkage aeExport aeCamera*       aeNewCamera(aeObject *parent, const char *name);
aeCLinkage aeExport void            aeXFormWriteSample(aeXForm *obj, const aeXFormSampleData *data);
aeCLinkage aeExport void            aePointsWriteSample(aePoints *obj, const aePointsSampleData *data);
aeCLinkage aeExport void            aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshSampleData *data);
aeCLinkage aeExport void            aeCameraWriteSample(aeCamera *obj, const aeCameraSampleData *data);


#endif // AlembicExporter_h
