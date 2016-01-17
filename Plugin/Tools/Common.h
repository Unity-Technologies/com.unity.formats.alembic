#define tCLinkage extern "C"
#ifdef _MSC_VER
    #define tExport __declspec(dllexport)
#else
    #define tExport __attribute__((visibility("default")))
#endif
#include "../Exporter/AlembicExporter.h"
#include "../Importer/AlembicImporter.h"

// default processors for tContext
void tSimpleCopyXForm(aiXForm *iobj, aeXForm *eobj);
void tSimpleCopyCamera(aiCamera *iobj, aeCamera *eobj);
void tSimpleCopyPolyMesh(aiPolyMesh *iobj, aePolyMesh *eobj);
void tSimpleCopyPoints(aiPoints *iobj, aePoints *eobj);

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
    std::map<aiObject*, aeObject*> m_iemap;
    std::map<aeObject*, aiObject*> m_eimap;
    std::vector<aiObject*> m_istack;
    std::vector<aeObject*> m_estack;

    XFormProcessor      m_xfproc;
    CameraProcessor     m_camproc;
    PointsProcessor     m_pointsproc;
    PolyMeshProcessor   m_meshproc;
};
