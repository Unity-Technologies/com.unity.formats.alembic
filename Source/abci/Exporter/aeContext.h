#pragma once

using abcTimeampling = Abc::TimeSampling;
using abcTimeamplingPtr = Abc::TimeSamplingPtr;
using abcObject = AbcGeom::OObject;
using abcXform = AbcGeom::OXform;
using abcCamera = AbcGeom::OCamera;
using abcPolyMesh = AbcGeom::OPolyMesh;
using abcFaceSet = AbcGeom::OFaceSet;
using abcPoints = AbcGeom::OPoints;
using abcProperties = AbcGeom::OCompoundProperty;

using abcBoolProperty = Abc::OBoolProperty;
using abcIntProperty = Abc::OInt32Property;
using abcUIntProperty = Abc::OUInt32Property;
using abcFloatProperty = Abc::OFloatProperty;
using abcFloat2Property = Abc::OV2fProperty;
using abcFloat3Property = Abc::OV3fProperty;
using abcFloat4Property = Abc::OC4fProperty;
using abcFloat4x4Property = Abc::OM44fProperty;

using abcBoolArrayProperty = Abc::OBoolArrayProperty;
using abcIntArrayProperty = Abc::OInt32ArrayProperty;
using abcUIntArrayProperty = Abc::OUInt32ArrayProperty;
using abcFloatArrayProperty = Abc::OFloatArrayProperty;
using abcFloat2ArrayProperty = Abc::OV2fArrayProperty;
using abcFloat3ArrayProperty = Abc::OV3fArrayProperty;
using abcFloat4ArrayProperty = Abc::OC4fArrayProperty;
using abcFloat4x4ArrayProperty = Abc::OM44fArrayProperty;

struct aeTimeSamplingData
{
    abcChrono start_time = 0.0;
    std::vector<abcChrono> times;
};
using aeTimeSamplingPtr = std::shared_ptr<aeTimeSamplingData>;

class aeContext
{
 public:
    using NodeCont = std::vector<aeObject*>;
    using NodeIter = NodeCont::iterator;

    aeContext();
    ~aeContext();
    void reset();
    void setConfig(const aeConfig& conf);
    bool openArchive(const char* path);

    const aeConfig& getConfig() const;
    aeObject* getTopObject();

    uint32_t getNumTimeSampling() const;
    abcTimeamplingPtr getTimeSampling(uint32_t i);
    aeTimeSamplingData& getTimeSamplingData(uint32_t i);
    uint32_t addTimeSampling(double start_time);
    void addTime(double time, uint32_t tsi = -1);

    void markFrameBegin();
    void markFrameEnd();

    void addAsync(const std::function<void()>& task);
    void waitAsync();

 private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::unique_ptr<aeObject> m_node_top;
    std::vector<aeTimeSamplingPtr> m_timesamplings;

    std::vector<std::function<void()> > m_async_tasks;
    std::future<void> m_async_task_future;
};
