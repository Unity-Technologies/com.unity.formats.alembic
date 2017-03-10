#pragma once

typedef AbcGeom::OObject            abcObject;
typedef AbcGeom::OXform             abcXForm;
typedef AbcGeom::OCamera            abcCamera;
typedef AbcGeom::OPolyMesh          abcPolyMesh;
typedef AbcGeom::OPoints            abcPoints;
typedef AbcGeom::OCompoundProperty  abcProperties;

typedef Abc::OBoolProperty          abcBoolProperty;
typedef Abc::OInt32Property         abcIntProperty;
typedef Abc::OUInt32Property        abcUIntProperty;
typedef Abc::OFloatProperty         abcFloatProperty;
typedef Abc::OV2fProperty           abcFloat2Property;
typedef Abc::OV3fProperty           abcFloat3Property;
typedef Abc::OC4fProperty           abcFloat4Property;
typedef Abc::OM44fProperty          abcFloat4x4Property;

typedef Abc::OBoolArrayProperty     abcBoolArrayProperty;
typedef Abc::OInt32ArrayProperty    abcIntArrayProperty;
typedef Abc::OUInt32ArrayProperty   abcUIntArrayProperty;
typedef Abc::OFloatArrayProperty    abcFloatArrayProperty;
typedef Abc::OV2fArrayProperty      abcFloat2ArrayProperty;
typedef Abc::OV3fArrayProperty      abcFloat3ArrayProperty;
typedef Abc::OC4fArrayProperty      abcFloat4ArrayProperty;
typedef Abc::OM44fArrayProperty     abcFloat4x4ArrayProperty;

struct aeTimeSampling
{
    abcChrono start_time;
    std::vector<abcChrono> times;

    aeTimeSampling() : start_time(0.0) {}
};


class aeContext
{
public:
    typedef std::vector<aeObject*> NodeCont;
    typedef NodeCont::iterator NodeIter;

    aeContext();
    ~aeContext();
    void reset();
    void setConfig(const aeConfig &conf);
    bool openArchive(const char *path);

    const aeConfig& getConfig() const;
    aeObject* getTopObject();

    uint32_t getNumTimeSampling() const;
    aeTimeSampling& getTimeSampling(uint32_t i);
    uint32_t addTimeSampling(float start_time);
    void addTime(float time, uint32_t tsi = -1);

private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::unique_ptr<aeObject> m_node_top;
    std::vector<aeTimeSampling> m_timesamplings;
};
