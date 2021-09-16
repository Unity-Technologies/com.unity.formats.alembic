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

#include "aiTimeSampling.h"


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
    explicit aiContext(int uid = -1);
    ~aiContext();

    bool load(const char *path);

    const aiConfig& getConfig() const;
    void setConfig(const aiConfig &config);

    aiObject* getTopObject() const;
    void updateSamples(double time);

    Abc::IArchive getArchive() const;
    const std::string& getPath() const;
    int getUid() const;

    int getTimeSamplingCount() const;
    aiTimeSampling* getTimeSampling(int i);
    void getTimeRange(double& begin, double& end) const;

    int getTimeSamplingCount();
    int getTimeSamplingIndex(Abc::TimeSamplingPtr ts);

    bool getIsHDF5() const { return m_isHDF5; }
    const char* getApplication();

    template<class F>
    void eachNodes(const F &f);

private:
    static void gatherNodesRecursive(aiObject *n);
    void reset();

    std::string m_path;
    std::vector<std::istream*> m_streams;

    Abc::IArchive m_archive;
    std::unique_ptr<aiObject> m_top_node;
    std::vector<aiTimeSamplingPtr> m_timesamplings;
    int m_uid = 0;
    aiConfig m_config;

    bool m_isHDF5;
    std::string m_app; // Lazy initialized by getApplication
};

#include "aiObject.h"

template<class F>
inline void aiContext::eachNodes(const F &f)
{
    if (m_top_node)
        m_top_node->eachChildRecursive(f);
}
