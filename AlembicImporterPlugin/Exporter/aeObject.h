#ifndef aeObject_h
#define aeObject_h

class aeObject
{
public:
    aeObject(aeContext *ctx, AbcGeom::OObject &abc, const char *name);
    aeObject(aeObject *parent, const char *name);
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



#endif // aeObject_h
