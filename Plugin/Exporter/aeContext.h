#ifndef aeContext_h
#define aeContext_h

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


class aeContext
{
public:
    typedef std::vector<aeObject*> NodeCont;
    typedef NodeCont::iterator NodeIter;

    aeContext(const aeConfig &conf);
    ~aeContext();
    void reset();
    bool openArchive(const char *path);

    const aeConfig& getConfig() const;
    uint32_t getDefaultTimeSaplingIndex() const;
    aeObject* getTopObject();

    uint32_t addTimeSampling(float start_time);
    void setTime(float time, uint32_t tsi = 0);

private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::unique_ptr<aeObject> m_node_top;
    std::vector<std::vector<abcChrono>> m_timesamplings;
    uint32_t m_default_timesampling_index;
};

#endif // aeContext_h
