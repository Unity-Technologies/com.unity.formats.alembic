#ifndef aePolyMesh_h
#define aePolyMesh_h

class aePolyMesh : public aeObject
{
typedef aeObject super;
public:
    aePolyMesh(aeObject *parent, const char *name, uint32_t tsi);
    abcPolyMesh& getAbcObject() override;
    abcProperties getAbcProperties() override;

    size_t  getNumSamples() override;
    void    setFromPrevious() override;
    void    writeSample(const aePolyMeshData &data);

private:
    AbcGeom::OPolyMeshSchema m_schema;

    std::vector<abcV3>  m_buf_positions;
    std::vector<abcV3>  m_buf_velocities;
    std::vector<abcV3>  m_buf_normals;
    std::vector<int>    m_buf_indices;
    std::vector<int>    m_buf_normal_indices;
    std::vector<int>    m_buf_uv_indices;
    std::vector<int>    m_buf_faces;
};


#endif // aePolyMesh_h
