#ifndef aeObject_h
#define aeObject_h

class aeObject
{
public:
    aeObject();
    aeObject(aiContext *ctx, abcObject &abc, const char *name);
    ~aeObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aeObject*   getChild(int i);
    aeObject*   getParent();

    aeXForm&    addXForm();
    aePolyMesh& addPolyMesh();
    aeCamera&   addCamera();
    aePoints&   addPoints();

public:
    aeContext*          getContext();
    AbcGeom::OObject&   getAbcObject();
    void                addChild(aeObject *c);
    void                removeChild(aeObject *c);

private:
    aeContext                   *m_ctx;
    aeObject                    *m_parent;
    AbcGeom::OObject            m_abc;
    std::vector<aeObject*>      m_children;

    std::vector<aeSchemaBase*>  m_schemas;
    std::unique_ptr<aeXForm>    m_xform;
    std::unique_ptr<aePolyMesh> m_polymesh;
    std::unique_ptr<aeCamera>   m_camera;
    std::unique_ptr<aePoints>   m_points;
};



class aeSchemaBase
{
public:
    virtual ~aeSchemaBase() {}
private:
};



struct aeXFormSampleData
{
    abcV3 translation;
    abcV4 rotation;
    abcV3 scale;
    bool inherits;

    inline aeXFormSampleData()
        : translation(0.0f, 0.0f, 0.0f)
        , rotation(0.0f, 0.0f, 0.0f, 1.0f)
        , scale(1.0f, 1.0f, 1.0f)
        , inherits(false)
    {
    }

    aeXFormSampleData(const aeXFormSampleData&) = default;
    aeXFormSampleData& operator=(const aeXFormSampleData&) = default;
};

class aeXForm : public aeSchemaBase
{
public:
    void writeSample(aeXFormSampleData &data);

private:
    AbcGeom::OXformSchema m_schema;
};



struct aePointsSample
{
    abcV3 *positions;
    int count;

    inline aePointsSample()
        : positions(nullptr)
        , count(0)
    {
    }
};

class aePoints : public aeSchemaBase
{
public:

private:
    AbcGeom::OPoints m_abcobj;
    AbcGeom::OPointsSchema m_schema;
};

#endif // aeObject_h
