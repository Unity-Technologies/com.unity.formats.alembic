#ifndef aiObject_h
#define aiObject_h

#include "Schema/aiSchema.h"

class aiContext;

enum aiNormalsMode
{
    NM_ReadFromFile = 0,
    NM_ComputeIfMissing,
    NM_AlwaysCompute,
    NM_Ignore
};

enum aiTangentsMode
{
    TM_None = 0,
    TM_Smooth,
    TM_Split
};

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

    void enableTriangulate(bool v);
    void swapHandedness(bool v);
    void swapFaceWinding(bool v);
    void setNormalsMode(aiNormalsMode m);
    void setTangentsMode(aiTangentsMode m);
    void cacheTangentsSplits(bool v);
    
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
    
    float       getCurrentTime() const;
    
    bool           getTriangulate() const;
    bool           isHandednessSwapped() const;
    bool           isFaceWindingSwapped() const;
    aiNormalsMode  getNormalsMode() const;
    aiTangentsMode getTangentsMode() const;
    bool           areTangentsSplitsCached() const;

private:
    aiContext   *m_ctx;
    abcObject   m_abc;
    std::vector<aiObject*> m_children;

    std::vector<aiSchema*> m_schemas;
    aiXForm     m_xform;
    aiPolyMesh  m_polymesh;
    aiCamera    m_camera;
    bool        m_hasXform;
    bool        m_hasPolymesh;
    bool        m_hasCamera;
    
    float m_time;
    bool m_triangulate;
    bool m_swapHandedness;
    bool m_swapFaceWinding;
    aiNormalsMode m_normalsMode;
    aiTangentsMode m_tangentsMode;
    bool m_cacheTangentsSplits;
};


#endif // aObject_h
