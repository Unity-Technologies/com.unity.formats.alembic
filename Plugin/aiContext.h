#ifndef aiContext_h
#define aiContext_h


typedef std::shared_ptr<Abc::IArchive> abcArchivePtr;

class aiContext
{
public:
public:
    static aiContextPtr create();
    static void destroy(aiContextPtr ctx);

    aiContext();
    bool load(const char *path);
    abcObject* getTopObject();
    void setCurrentObject(abcObject *obj);
    void setCurrentTime(float time);
    void enableReverseX(bool v);
    void enableTriangulate(bool v);
    void enableReverseIndex(bool v);

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;

    bool        hasXForm() const;
    bool        hasPolyMesh() const;
    bool        hasCurves() const;
    bool        hasPoints() const;
    bool        hasCamera() const;
    bool        hasMaterial() const;

    aiXForm&    getXForm();
    aiPolyMesh& getPolyMesh();
    aiCurves&   getCurves();
    aiPoints&   getPoints();
    aiCamera&   getCamera();
    aiMaterial& getMaterial();

private:
    bool m_reverse_x;
    bool m_triangulate;
    bool m_reverse_index;

    bool m_has_xform;
    bool m_has_polymesh;
    bool m_has_curves;
    bool m_has_points;
    bool m_has_camera;
    bool m_has_material;

#ifdef aiWithDebugLog
    std::string m_dbg_current_object_name;
#endif // aiWithDebugLog
    abcArchivePtr m_archive;
    abcObject m_top_object;
    abcObject m_current;
    Abc::ISampleSelector m_sample_selector;

    aiXForm m_xform;
    aiPolyMesh m_polymesh;
    aiCurves m_curves;
    aiPoints m_points;
    aiCamera m_camera;
    aiMaterial m_material;
};



#endif // aiContext_h
