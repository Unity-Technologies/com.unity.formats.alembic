#define tCLinkage extern "C"
#ifdef _MSC_VER
    #define tExport __declspec(dllexport)
#else
    #define tExport __attribute__((visibility("default")))
#endif
#include "../Exporter/AlembicExporter.h"
#include "../Importer/AlembicImporter.h"

#define tLog(...)



class tPointsBuffer
{
public:
    void allocate(size_t size, bool alloc_velocities = false, bool alloc_ids = false);

    aiPointsData asImportData();
    aePointsData asExportData();

public:
    std::vector<abcV3> positions;
    std::vector<abcV3> velocities;
    std::vector<uint64_t> ids;
};

class tPolyMeshBuffer
{
public:
    void allocatePositions(size_t size, size_t index_size);
    // size of velocities is same as positions'
    void allocateVelocity(bool alloc);
    void allocateNormals(size_t size, size_t index_size = 0);
    void allocateUVs(size_t size, size_t index_size = 0);
    void allocateFaces(size_t size);

    aiPolyMeshData asImportData();
    aePolyMeshData asExportData();

public:
    std::vector<abcV3> positions;
    std::vector<abcV3> velocities;
    std::vector<abcV3> normals;
    std::vector<abcV2> uvs;
    std::vector<int> indices;
    std::vector<int> normal_indices;
    std::vector<int> uv_indices;
    std::vector<int> faces;
};


aeXFormData     tImportDataToExportData(const aiXFormData& idata);
aeCameraData    tImportDataToExportData(const aiCameraData& idata);
aePointsData    tImportDataToExportData(const aiPointsData& idata);
aePolyMeshData  tImportDataToExportData(const aiPolyMeshData& idata);

// default processors for tContext
void tSimpleCopyXForm(aiXForm *iobj, aeXForm *eobj);
void tSimpleCopyCamera(aiCamera *iobj, aeCamera *eobj);
void tSimpleCopyPoints(aiPoints *iobj, aePoints *eobj);
void tSimpleCopyPolyMesh(aiPolyMesh *iobj, aePolyMesh *eobj);


struct tContext
{
public:
    typedef std::function<void(aiXForm*, aeXForm*)>         XFormProcessor;
    typedef std::function<void(aiCamera*, aeCamera*)>       CameraProcessor;
    typedef std::function<void(aiPoints*, aePoints*)>       PointsProcessor;
    typedef std::function<void(aiPolyMesh*, aePolyMesh*)>   PolyMeshProcessor;

    tContext();
    ~tContext();
    void setArchives(aiContext *ictx, aeContext *ectx);
    void setExportConfig(const aeConfig &conf);
    void setXFormProcessor(const XFormProcessor& v);
    void setCameraProcessor(const CameraProcessor& v);
    void setPointsProcessor(const PointsProcessor& v);
    void setPolyMeshrocessor(const PolyMeshProcessor& v);

    aiObject* getIObject(aeObject *eobj);
    aeObject* getEObject(aiObject *iobj);

    void doExport();

private:
    void doExportImpl(aiObject *obj);

private:
    aiContext *m_ictx;
    aeContext *m_ectx;
    aeConfig m_econf;

    std::map<aiObject*, aeObject*> m_iemap;
    std::map<aeObject*, aiObject*> m_eimap;
    std::vector<aiObject*> m_istack;
    std::vector<aeObject*> m_estack;

    XFormProcessor      m_xfproc;
    CameraProcessor     m_camproc;
    PointsProcessor     m_pointsproc;
    PolyMeshProcessor   m_meshproc;
};
