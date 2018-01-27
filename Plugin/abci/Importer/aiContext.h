#pragma once
using abcObject = AbcGeom::IObject;
using abcXForm = AbcGeom::IXform;
using abcCamera = AbcGeom::ICamera;
using abcPolyMesh = AbcGeom::IPolyMesh;
using abcPoints = AbcGeom::IPoints;
using abcProperties = AbcGeom::ICompoundProperty;

using abcBoolProperty = Abc::IBoolProperty;
using abcIntProperty = Abc::IInt32Property;
using abcUIntProperty = Abc::IUInt32Property;
using abcFloatProperty = Abc::IFloatProperty;
using abcFloat2Property = Abc::IV2fProperty;
using abcFloat3Property = Abc::IV3fProperty;
using abcFloat4Property = Abc::IC4fProperty;
using abcFloat4x4Property = Abc::IM44fProperty;

using abcBoolArrayProperty = Abc::IBoolArrayProperty;
using abcIntArrayProperty = Abc::IInt32ArrayProperty;
using abcUIntArrayProperty = Abc::IUInt32ArrayProperty;
using abcFloatArrayProperty = Abc::IFloatArrayProperty;
using abcFloat2ArrayProperty = Abc::IV2fArrayProperty;
using abcFloat3ArrayProperty = Abc::IV3fArrayProperty;
using abcFloat4ArrayProperty = Abc::IC4fArrayProperty;
using abcFloat4x4ArrayProperty = Abc::IM44fArrayProperty;

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
    int getFrameCount() const;
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
    static void gatherNodesRecursive(aiObject *n);
    void reset();

    std::string m_path;
    Abc::IArchive m_archive;
    std::unique_ptr<aiObject> m_top_node;
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
