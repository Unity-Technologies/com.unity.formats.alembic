#pragma once

class aeFaceSet
{
public:
    aeFaceSet(aeObject *parent, const char *name, uint32_t tsi);
    void writeSample(const aeFaceSetData &data);

private:
    std::unique_ptr<abcObject> m_abc;
    AbcGeom::OFaceSetSchema m_schema;
    RawVector<int> m_buf_faces;
};
using aeFaceSetPtr = std::shared_ptr<aeFaceSet>;


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

    int addFaceSet(const char *name);
    void writeFaceSetSample(int faceset_index, const aeFaceSetData &data);

private:
    void    doWriteSample();

    AbcGeom::OPolyMeshSchema m_schema;
    RawVector<abcV3> m_buf_positions;
    RawVector<abcV3> m_buf_velocities;
    RawVector<abcV3> m_buf_normals;
    RawVector<abcV2> m_buf_uvs;
    RawVector<int>   m_buf_indices;
    RawVector<int>   m_buf_normal_indices;
    RawVector<int>   m_buf_uv_indices;
    RawVector<int>   m_buf_faces;

    std::vector<aeFaceSetPtr> m_facesets;
};
