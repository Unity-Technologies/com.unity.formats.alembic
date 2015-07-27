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
    aiObject(aiContext *ctx, abcObject &abc);
    ~aiObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aiObject*   getChild(int i);

    void        updateSample(float time);
    void        erasePastSamples(float from, float range);
    
    bool        hasXForm() const;
    bool        hasPolyMesh() const;
    bool        hasCamera() const;

    aiXForm&    getXForm();
    aiPolyMesh& getPolyMesh();
    aiCamera&   getCamera();

public:

    aiContext*  getContext();
    abcObject&  getAbcObject();
    void        addChild(aiObject *c);

private:
    aiContext   *m_ctx;
    abcObject   m_abc;
    std::vector<aiObject*> m_children;

    std::vector<aiSchemaBase*>  m_schemas;
    std::unique_ptr<aiXForm>    m_xform;
    std::unique_ptr<aiPolyMesh> m_polymesh;
    std::unique_ptr<aiCamera>   m_camera;
};


#endif // aObject_h
