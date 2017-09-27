#pragma once
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
std::string ToString(const aiConfig &conf);

class aiContextManager
{
public:
    static aiContext* getContext(int uid);
    static void destroyContext(int uid);
    static void destroyContextsWithPath(const char* assetPath);
private:
    ~aiContextManager();
    std::map<int, aiContext*> m_contexts;
    static aiContextManager ms_instance;
};


class aiContext
{
public:
    explicit aiContext(int uid=-1);
    ~aiContext();
    
    bool load(const char *path);
    
    const aiConfig& getConfig() const;
    void setConfig(const aiConfig &config);

    aiObject* getTopObject() const;
    void destroyObject(aiObject *obj);

    float getStartTime() const;
    float getEndTime() const;
    void cacheAllSamples();
    void updateSamples(float time);

    Abc::IArchive getArchive() const;
    const std::string& getPath() const;
    int getUid() const;

    int getNumTimeSamplings();
    void getTimeSampling(int i, aiTimeSamplingData& dst);
    void copyTimeSampling(int i, aiTimeSamplingData& dst);
    int getTimeSamplingIndex(Abc::TimeSamplingPtr ts);

    template<class F>
    void eachNodes(const F &f);

    static std::string normalizePath(const char *path);
private:
    void reset();
    void gatherNodesRecursive(aiObject *n) const;

    std::string m_path;
    Abc::IArchive m_archive;
    std::unique_ptr<aiObject> m_top_node;
    aiTaskGroup m_tasks;
    double m_timeRange[2] = {-0.0, -0.0};
    uint64_t m_numFrames = 0;
    int m_uid = 0;
    aiConfig m_config;
};

#include "aiObject.h"

template<class F>
inline void aiContext::eachNodes(const F &f)
{
    if (m_top_node) {
        m_top_node->eachChildrenRecursive(f);
    }
}
