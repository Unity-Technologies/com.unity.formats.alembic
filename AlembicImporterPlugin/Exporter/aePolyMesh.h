#ifndef aePolyMesh_h
#define aePolyMesh_h

class aePolyMesh : public aeObject
{
typedef aeObject super;
public:
    aePolyMesh(aeObject *parent, const char *name);
    abcPolyMesh& getAbcObject() override;
    abcProperties* getAbcProperties() override;

    void writeSample(const aePolyMeshSampleData &data);

private:
    AbcGeom::OPolyMeshSchema m_schema;

    std::vector<abcV3>  m_buf_positions;
    std::vector<abcV3>  m_buf_normals;
    std::vector<int>    m_buf_faces;
};


#endif // aePolyMesh_h
