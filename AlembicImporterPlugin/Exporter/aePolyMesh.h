#ifndef aePolyMesh_h
#define aePolyMesh_h

struct aePolyMeshSampleData
{
    abcV3 *positions;
    abcV3 *normals; // can be null
    abcV2 *uvs; // can be null
    int *indices;
    int *faces; // can be null. assume all faces are triangles if null

    int vertex_count;
    int index_count;
    int face_count;

    aePolyMeshSampleData()
        : positions(nullptr)
        , normals(nullptr)
        , uvs(nullptr)
        , indices(nullptr)
        , faces(nullptr)
        , vertex_count(0)
        , index_count(0)
        , face_count(0)
    {
    }
};

class aePolyMesh : public aeObject
{
typedef aeObject super;
public:
    aePolyMesh(aeObject *parent, const char *name);
    AbcGeom::OPolyMesh& getAbcObject() override;

    void writeSample(const aePolyMeshSampleData &data);

private:
    AbcGeom::OPolyMeshSchema m_schema;

    std::vector<abcV3>  m_buf_positions;
    std::vector<abcV3>  m_buf_normals;
    std::vector<int>    m_buf_faces;
};


#endif // aePolyMesh_h
