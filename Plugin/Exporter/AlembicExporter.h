#ifndef AlembicExporter_h
#define AlembicExporter_h

#include <cstdint>

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
        #pragma comment(lib, "AlembicExporter.lib")
    #else
    #endif

#ifndef AlembicImporter_h
    struct abcV2
    {
        float x, y;

        abcV2() {}
        abcV2(float _x, float _y) : x(_x), y(_y) {}
    };

    struct abcV3
    {
        float x, y, z;

        abcV3() {}
        abcV3(float _x, float _y, float _z) : x(_x), y(_y), z(_z) {}
    };

    struct abcV4
    {
        float x, y, z, w;

        abcV4() {}
        abcV4(float _x, float _y, float _z, float _w) : x(_x), y(_y), w(_w) {}
    };

    struct abcSampleSelector
    {
        uint64_t m_requestedIndex;
        double m_requestedTime;
        int m_requestedTimeIndexType;
    };
#endif // AlembicImporter_h
#endif // aeImpl

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

struct aeXFormData;
struct aePointsData;
struct aePolyMeshData;
struct aeCameraData;


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
    aePropertyType_Unknown,

    // scalar types
    aePropertyType_Bool,
    aePropertyType_Int,
    aePropertyType_UInt,
    aePropertyType_Float,
    aePropertyType_Float2,
    aePropertyType_Float3,
    aePropertyType_Float4,
    aePropertyType_Float4x4,

    // array types
    aePropertyType_BoolArray,
    aePropertyType_IntArray,
    aePropertyType_UIntArray,
    aePropertyType_FloatArray,
    aePropertyType_Float2Array,
    aePropertyType_Float3Array,
    aePropertyType_Float4Array,
    aePropertyType_Float4x4Array,

    aePropertyType_ScalarTypeBegin = aePropertyType_Bool,
    aePropertyType_ScalarTypeEnd = aePropertyType_Float4x4,

    aePropertyType_ArrayTypeBegin = aePropertyType_BoolArray,
    aePropertyType_ArrayTypeEnd = aePropertyType_Float4x4Array,
};

struct aeConfig
{
    aeArchiveType archiveType;
    aeTimeSamplingType timeSamplingType;
    float startTime;    // start time on Alembic.
    float frameRate;    // frame rate on Alembic. relevant only if timeSamplingType is uniform
    aeXFromType xformType;
    bool swapHandedness; // swap rhs <-> lhs
    bool swapFaces; // swap triangle indices
    float scale;

    aeConfig()
        : archiveType(aeArchiveType_Ogawa)
        , timeSamplingType(aeTimeSamplingType_Uniform)
        , startTime(0.0f)
        , frameRate(30.0f)
        , xformType(aeXFromType_TRS)
        , swapHandedness(false)
        , swapFaces(false)
        , scale(1.0f)
    {
    }
};


struct aeXFormData
{
    abcV3 translation;
    abcV3 rotationAxis;
    float rotationAngle;
    abcV3 scale;
    bool inherits;

    inline aeXFormData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotationAxis(0.0f, 1.0f, 0.0f)
        , rotationAngle(0.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(false)
    {
    }
};

struct aePolyMeshData
{
    const abcV3 *positions;
    const abcV3 *velocities;    // can be null. if not null, must be same size of positions
    const abcV3 *normals;       // can be null
    const abcV2 *uvs;           // can be null

    const int *indices;
    const int *normalIndices;   // if null, assume same as indices
    const int *uvIndices;       // if null, assume same as indices
    const int *faces;           // if null, assume all faces are triangles

    int positionCount;
    int normalCount;            // if 0, assume same as positionCount
    int uvCount;                // if 0, assume same as positionCount

    int indexCount;
    int normalIndexCount;       // if 0, assume same as indexCount
    int uvIndexCount;           // if 0, assume same as indexCount
    int faceCount;              // only relevant if faces is not null

    aePolyMeshData()
        : positions(nullptr), velocities(nullptr), normals(nullptr), uvs(nullptr)
        , indices(nullptr), normalIndices(nullptr), uvIndices(nullptr), faces(nullptr)
        , positionCount(0), normalCount(0), uvCount(0)
        , indexCount(0), normalIndexCount(0), uvIndexCount(0), faceCount(0)
    {}
};

struct aePointsData
{
    const abcV3 *positions;
    const abcV3 *velocities;    // can be null
    const uint64_t *ids;        // can be null
    int count;

    inline aePointsData()
        : positions(nullptr)
        , velocities(nullptr)
        , ids(nullptr)
        , count(0)
    {
    }
};

struct aeCameraData
{
    float nearClippingPlane;
    float farClippingPlane;
    float fieldOfView; // degree. relevant only if focusDistance==0.0
    float aspectRatio;

    float focalLength; // if 0.0f, automatically computed by aperture and fieldOfView. alembic's default value is 0.035f.
    float focusDistance;
    float aperture;

    inline aeCameraData()
        : nearClippingPlane(0.3f)
        , farClippingPlane(1000.0f)
        , fieldOfView(60.0f)
        , aspectRatio(16.0f / 9.0f)
        , focalLength(0.0f)
        , focusDistance(5.0f)
        , aperture(2.4f)
    {
    }
};



aeCLinkage aeExport aeContext*      aeCreateContext(const aeConfig *conf);
aeCLinkage aeExport void            aeDestroyContext(aeContext* ctx);

aeCLinkage aeExport bool            aeOpenArchive(aeContext* ctx, const char *path);
aeCLinkage aeExport aeObject*       aeGetTopObject(aeContext* ctx);
aeCLinkage aeExport int             aeAddTimeSampling(aeContext* ctx, float start_time);
aeCLinkage aeExport void            aeAddTime(aeContext* ctx, float time, int tsi = 0); // relevant only if timeSamplingType is acyclic

aeCLinkage aeExport void            aeDeleteObject(aeObject *obj);
aeCLinkage aeExport aeXForm*        aeNewXForm(aeObject *parent, const char *name, int tsi = 0);
aeCLinkage aeExport aePoints*       aeNewPoints(aeObject *parent, const char *name, int tsi = 0);
aeCLinkage aeExport aePolyMesh*     aeNewPolyMesh(aeObject *parent, const char *name, int tsi = 0);
aeCLinkage aeExport aeCamera*       aeNewCamera(aeObject *parent, const char *name, int tsi = 0);

aeCLinkage aeExport int             aeGetNumChildren(aeObject *obj);
aeCLinkage aeExport aeObject*       aeGetChild(aeObject *obj, int i);
aeCLinkage aeExport aeObject*       aeGetParent(aeObject *obj);
aeCLinkage aeExport aeXForm*        aeAsXForm(aeObject *obj);
aeCLinkage aeExport aePoints*       aeAsPoints(aeObject *obj);
aeCLinkage aeExport aePolyMesh*     aeAsPolyMesh(aeObject *obj);
aeCLinkage aeExport aeCamera*       aeAsCamera(aeObject *obj);

aeCLinkage aeExport int             aeGetNumSamples(aeObject *obj);
aeCLinkage aeExport void            aeSetFromPrevious(aeObject *obj);

aeCLinkage aeExport void            aeXFormWriteSample(aeXForm *obj, const aeXFormData *data);
aeCLinkage aeExport void            aePointsWriteSample(aePoints *obj, const aePointsData *data);
aeCLinkage aeExport void            aePolyMeshWriteSample(aePolyMesh *obj, const aePolyMeshData *data);
aeCLinkage aeExport void            aeCameraWriteSample(aeCamera *obj, const aeCameraData *data);

aeCLinkage aeExport aeProperty*     aeNewProperty(aeObject *parent, const char *name, aePropertyType type);
aeCLinkage aeExport void            aePropertyWriteArraySample(aeProperty *prop, const void *data, int num_data);
aeCLinkage aeExport void            aePropertyWriteScalarSample(aeProperty *prop, const void *data);

#endif // AlembicExporter_h
