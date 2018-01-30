#pragma once
using abcObject = AbcGeom::IObject;
using abcXform = AbcGeom::IXform;
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


class aiAsync
{
public:
    virtual ~aiAsync() {}
    virtual void prepare() = 0;
    virtual void run() = 0;
    virtual void wait() = 0;
};


class aiContextManager
{
public:
    static aiContext* getContext(int uid);
    static void destroyContext(int uid);
    static void destroyContextsWithPath(const char* assetPath);

private:
    ~aiContextManager();

    using ContextPtr = std::unique_ptr<aiContext>;
    std::map<int, ContextPtr> m_contexts;
    static aiContextManager s_instance;
};


class aiContext
{
public:
    static std::string normalizePath(const char *path);

public:
    explicit aiContext(int uid=-1);
    ~aiContext();
    
    bool load(const char *path);
    
    const aiConfig& getConfig() const;
    void setConfig(const aiConfig &config);

    aiObject* getTopObject() const;
    void updateSamples(double time);

    Abc::IArchive getArchive() const;
    const std::string& getPath() const;
    int getUid() const;

    int getTimeRangeCount() const;
    void getTimeRange(int tsi, aiTimeRange& dst) const;

    int getTimeSamplingCount();
    int getTimeSamplingIndex(Abc::TimeSamplingPtr ts);

    template<class F>
    void eachNodes(const F &f);

    void queueAsync(aiAsync& task);
    void waitAsync();

private:
    static void gatherNodesRecursive(aiObject *n);
    void reset();

    std::string m_path;
    Abc::IArchive m_archive;
    std::unique_ptr<aiObject> m_top_node;
    aiTimeRange m_time_range_unified;
    std::vector<aiTimeRange> m_time_ranges;
    int m_uid = 0;
    aiConfig m_config;

    std::vector<aiAsync*> m_async_tasks;
    std::future<void> m_async_future;
};

#include "aiObject.h"

template<class F>
inline void aiContext::eachNodes(const F &f)
{
    if (m_top_node) {
        m_top_node->eachChildrenRecursive(f);
    }
}
