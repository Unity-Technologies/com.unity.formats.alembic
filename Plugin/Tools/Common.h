#define tCLinkage extern "C"
#ifdef _MSC_VER
    #define tExport __declspec(dllexport)
#else
    #define tExport __attribute__((visibility("default")))
#endif
#include "../Exporter/AlembicExporter.h"
#include "../Importer/AlembicImporter.h"

struct tContext
{
public:
    typedef std::function<void(aiObject*, aeObject*)> IEEnumerator;

    tContext();
    ~tContext();
    void setArchives(aiContext *ictx, aeContext *ectx);

    aiObject* getIObject(aeObject *eobj);
    aeObject* getEObject(aiObject *iobj);

    void constructIETree(const IEEnumerator &cb);

private:
    void constructIETreeImpl(aiObject *obj, const IEEnumerator &cb);

private:
    aiContext *m_ictx;
    aeContext *m_ectx;
    std::map<aiObject*, aeObject*> m_iemap;
    std::map<aeObject*, aiObject*> m_eimap;
    std::vector<aiObject*> m_istack;
    std::vector<aeObject*> m_estack;
};
