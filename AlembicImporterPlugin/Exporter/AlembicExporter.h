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

class aeContext;
#ifdef aeImpl
    class aeObject;
#else
    typedef void aeObject; // force make upper-castable
#endif
class aeXForm;    // : aeObject
class aePoints;   // : aeObject
class aePolyMesh; // : aeObject
class aeCamera;   // : aeObject
class aeProperty;
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

enum aeXFromType
{
    aeXFromType_Matrix,
    aeXFromType_TRS,
};

enum aePropertyType
{
    // array types
    aePropertyType_FloatArray,
    aePropertyType_IntArray,
    aePropertyType_BoolArray,
    aePropertyType_Vec2Array,
    aePropertyType_Vec3Array,
    aePropertyType_Vec4Array,
    aePropertyType_Mat44Array,

    // scalar types
    aePropertyType_Float,
    aePropertyType_Int,
    aePropertyType_Bool,
    aePropertyType_Vec2,
    aePropertyType_Vec3,
    aePropertyType_Vec4,
    aePropertyType_Mat44,
};

struct aeConfig
{
    aeArchiveType archiveType;
    aeTimeSamplingType timeSamplingType;
    float startTime;     // relevant only if timeSamplingType is uniform
    float timePerSample; // relevant only if timeSamplingType is uniform
    aeXFromType xformType;
    bool swapHandedness; // swap rhs <-> lhs

    aeConfig()
        : archiveType(aeArchiveType_Ogawa)
        , timeSamplingType(aeTimeSamplingType_Uniform)
        , startTime(0.0f)
        , timePerSample(1.0f / 30.0f)
        , xformType(aeXFromType_TRS)
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
    const abcV3 *positions;
    const abcV3 *normals; // can be null
    const abcV2 *uvs; // can be null
    const int *indices;
    const int *faces; // can be null. assume all faces are triangles if null

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
    const abcV3 *positions;
    const uint64_t *ids; // can be null
    int count;

    inline aePointsSampleData()
        : positions(nullptr)
        , ids(nullptr)
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

aeCLinkage aeExport aeProperty*     aeNewProperty(aeObject *parent, const char *name, aePropertyType type);
aeCLinkage aeExport void            aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data);
aeCLinkage aeExport void            aePropertyWriteScalarSample(aeProperty *prop, const void *data);

#endif // AlembicExporter_h
