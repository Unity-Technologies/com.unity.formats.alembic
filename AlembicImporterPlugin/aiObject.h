#ifndef aiObject_h
#define aiObject_h

#include "aiGeometry.h"
class aiContext;
const int aiMagicObj = 0x004a424f; // "OBJ"


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

    void setCurrentTime(float time);
    void enableReverseX(bool v);
    void enableTriangulate(bool v);
    void enableReverseIndex(bool v);
    void forceSmoothNormals(bool v);

    bool        hasXForm() const;
    bool        hasPolyMesh() const;
    bool        hasCurves() const;
    bool        hasPoints() const;
    bool        hasCamera() const;
    bool        hasLight() const;
    bool        hasMaterial() const;
    aiXForm&    getXForm();
    aiPolyMesh& getPolyMesh();
    aiCurves&   getCurves();
    aiPoints&   getPoints();
    aiCamera&   getCamera();
    aiLight&    getLight();
    aiMaterial& getMaterial();

public:
    aiContext*  getContext();
    abcObject&  getAbcObject();
    void        addChild(aiObject *c);
    float       getCurrentTime() const;
    bool        getReverseX() const;
    bool        getReverseIndex() const;
    bool        getTriangulate() const;
    bool        getForceSmoothNormals() const;

private:
#ifdef aiDebug
    int m_magic;
#endif // aiDebug
    aiContext   *m_ctx;
    abcObject   m_abc;
    std::vector<aiObject*> m_children;

    std::vector<aiSchema*> m_schemas;
    aiXForm     m_xform;
    aiPolyMesh  m_polymesh;
    aiCurves    m_curves;
    aiPoints    m_points;
    aiCamera    m_camera;
    aiLight     m_light;
    aiMaterial  m_material;
    bool        m_hasXform;
    bool        m_hasPolymesh;
    bool        m_hasCurves;
    bool        m_hasPoints;
    bool        m_hasCamera;
    bool        m_hasLight;
    bool        m_hasMaterial;

    float m_time;
    bool m_reverseX;
    bool m_triangulate;
    bool m_reverseIndex;
    bool m_forceSmoothNormals;
};


#endif // aObject_h
