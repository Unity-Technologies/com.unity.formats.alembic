#ifndef aiObject_h
#define aiObject_h

class aiContext;
class aiSchemaBase;
class aiXForm;
class aiPolyMesh;
class aiCamera;

class aiObject
{
public:
    aiObject();
    aiObject(aiContext *ctx, aiObject *parent, abcObject &abc);
    ~aiObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aiObject*   getChild(int i);
    aiObject*   getParent();

    void        readConfig();
    void        updateSample(float time);
    void        notifyUpdate();
 
    aiXForm*    getXForm();
    aiPolyMesh* getPolyMesh();
    aiCamera*   getCamera();
    aiPoints*   getPoints();

    template<class F>
    void eachChildren(const F &f)
    {
        for (auto *c : m_children) { f(c); }
    }

    template<class F>
    void eachChildrenRecursive(const F &f)
    {
        for (auto *c : m_children) {
            f(c);
            c->eachChildrenRecursive(f);
        }
    }

public:
    // for internal use
    aiContext*  getContext();
    abcObject&  getAbcObject();
    aiObject*   newChild(abcObject &abc);
    void        removeChild(aiObject *c);

private:
    aiContext   *m_ctx;
    abcObject   m_abc;
    aiObject    *m_parent;
    std::vector<aiObject*> m_children;

    std::vector<aiSchemaBase*>  m_schemas;
    std::unique_ptr<aiXForm>    m_xform;
    std::unique_ptr<aiPolyMesh> m_polymesh;
    std::unique_ptr<aiCamera>   m_camera;
    std::unique_ptr<aiPoints>   m_points;
};


#endif // aObject_h
