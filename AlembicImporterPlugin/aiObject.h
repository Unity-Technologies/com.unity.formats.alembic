#ifndef aiObject_h
#define aiObject_h

#include "Schema/aiSchema.h"
class aiContext;


class aiObject
{
public:
    aiObject(aiContext *ctx, abcObject &abc);
    ~aiObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aiObject*   getChild(int i);

    void        updateSample(float time);
    void        erasePastSamples(float time, float range_keep);

    bool        hasXForm() const;
    bool        hasPolyMesh() const;
    bool        hasCamera() const;
    //bool        hasCurves() const;
    //bool        hasPoints() const;
    //bool        hasLight() const;
    //bool        hasMaterial() const;

    aiXForm&    getXForm();
    aiPolyMesh& getPolyMesh();
    aiCamera&   getCamera();
    //aiCurves&   getCurves();
    //aiPoints&   getPoints();
    //aiLight&    getLight();
    //aiMaterial& getMaterial();

    void debugDump() const;

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
    //std::unique_ptr<aiCurves>   m_curves;
    //std::unique_ptr<aiPoints>   m_points;
    //std::unique_ptr<aiLight>    m_light;
    //std::unique_ptr<aiMaterial> m_material;
};


#endif // aObject_h
