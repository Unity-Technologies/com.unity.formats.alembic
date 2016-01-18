#ifndef aiContext_h
#define aiContext_h

#include "aiThreadPool.h"
#include "aiMisc.h"

typedef AbcGeom::IObject            abcObject;
typedef AbcGeom::IXform             abcXForm;
typedef AbcGeom::ICamera            abcCamera;
typedef AbcGeom::IPolyMesh          abcPolyMesh;
typedef AbcGeom::IPoints            abcPoints;
typedef AbcGeom::ICompoundProperty  abcProperties;

typedef Abc::IBoolProperty          abcBoolProperty;
typedef Abc::IInt32Property         abcIntProperty;
typedef Abc::IUInt32Property        abcUIntProperty;
typedef Abc::IFloatProperty         abcFloatProperty;
typedef Abc::IV2fProperty           abcFloat2Property;
typedef Abc::IV3fProperty           abcFloat3Property;
typedef Abc::IC4fProperty           abcFloat4Property;
typedef Abc::IM44fProperty          abcFloat4x4Property;

typedef Abc::IBoolArrayProperty     abcBoolArrayProperty;
typedef Abc::IInt32ArrayProperty    abcIntArrayProperty;
typedef Abc::IUInt32ArrayProperty   abcUIntArrayProperty;
typedef Abc::IFloatArrayProperty    abcFloatArrayProperty;
typedef Abc::IV2fArrayProperty      abcFloat2ArrayProperty;
typedef Abc::IV3fArrayProperty      abcFloat3ArrayProperty;
typedef Abc::IC4fArrayProperty      abcFloat4ArrayProperty;
typedef Abc::IM44fArrayProperty     abcFloat4x4ArrayProperty;


class aiObject;

class aiContext
{
public:
    typedef std::function<void ()> task_t;

    static aiContext* create(int uid);
    static void destroy(aiContext* ctx);

public:
    aiContext(int uid=-1);
    ~aiContext();
    
    bool load(const char *path);
    
    const aiConfig& getConfig() const;
    void setConfig(const aiConfig &config);

    aiObject* getTopObject();
    void destroyObject(aiObject *obj);

    float getStartTime() const;
    float getEndTime() const;

    void updateSamples(float time);
    void updateSamplesBegin(float time);
    void updateSamplesEnd();

    void enqueueTask(const task_t &task);
    void waitTasks();

    Abc::IArchive getArchive() const;
    const std::string& getPath() const;
    int getUid() const;

    int getNumTimeSamplings();
    void getTimeSampling(int i, aiTimeSamplingData& dst);
    void copyTimeSampling(int i, aiTimeSamplingData& dst);
    int getTimeSamplingIndex(Abc::TimeSamplingPtr ts);

    template<class F>
    void eachNodes(const F &f)
    {
        m_top_node->eachChildrenRecursive(f);
    }

private:
    std::string normalizePath(const char *path) const;
    void reset();
    void gatherNodesRecursive(aiObject *n);

private:
    std::string m_path;
    Abc::IArchive m_archive;
    std::unique_ptr<aiObject> m_top_node;
    aiTaskGroup m_tasks;
    double m_timeRange[2];
    int m_uid;
    aiConfig m_config;
};



#endif // aiContext_h
