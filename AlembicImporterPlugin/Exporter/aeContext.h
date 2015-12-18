#ifndef aeContext_h
#define aeContext_h

typedef AbcGeom::OObject            abcObject;
typedef AbcGeom::OXform             abcXForm;
typedef AbcGeom::OCamera            abcCamera;
typedef AbcGeom::OPolyMesh          abcPolyMesh;
typedef AbcGeom::OPoints            abcPoints;
typedef AbcGeom::OCompoundProperty  abcProperties;

typedef Abc::OFloatArrayProperty    abcFloatArrayProperty;
typedef Abc::OInt32ArrayProperty    abcInt32ArrayProperty;
typedef Abc::OBoolArrayProperty     abcBoolArrayProperty;
typedef Abc::OV2fArrayProperty      abcVec2ArrayProperty;
typedef Abc::OV3fArrayProperty      abcVec3ArrayProperty;
typedef Abc::OC4fArrayProperty      abcVec4ArrayProperty;
typedef Abc::OM44fArrayProperty     abcMat44ArrayProperty;


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
    uint32_t getTimeSaplingIndex() const;
    aeObject* getTopObject();

    void setTime(float time);

private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::unique_ptr<aeObject> m_node_top;
    std::vector<abcChrono> m_times;
    uint32_t m_time_sampling_index;
};

#endif // aeContext_h
