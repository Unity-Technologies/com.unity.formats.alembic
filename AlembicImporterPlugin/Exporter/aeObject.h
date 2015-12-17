#ifndef aeObject_h
#define aeObject_h

class aeObject
{
public:
    aeObject(aeContext *ctx, aeObject *parent, AbcGeom::OObject *abc);
    virtual ~aeObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aeObject*   getChild(int i);
    aeObject*   getParent();

    aeContext*          getContext();
    const aeConfig&     getConfig() const;
    virtual AbcGeom::OObject&   getAbcObject();

    /// T: aeCamera, aeXForm, aePoint, aePolyMesh
    template<class T> T* newChild(const char *name);
    void                removeChild(aeObject *c);

protected:
    aeContext               *m_ctx;
    aeObject                *m_parent;
    std::vector<aeObject*>  m_children;
    std::unique_ptr<AbcGeom::OObject> m_abc;
};


class aeSchemaBase
{
public:
    aeSchemaBase(aeObject *obj);
    virtual ~aeSchemaBase();

    const aeConfig& getConfig() const;

protected:
    aeObject *m_obj;
};



#endif // aeObject_h
