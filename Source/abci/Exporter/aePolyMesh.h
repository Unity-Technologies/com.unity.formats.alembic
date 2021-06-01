#pragma once
#include <Foundation/Vector.h>

class aeFaceSet
{
public:
    aeFaceSet(aeObject *parent, const char *name, uint32_t tsi);
    void writeSample(const aeFaceSetData &data);

private:
    std::unique_ptr<abcObject> m_abc;
    AbcGeom::OFaceSetSchema m_schema;
    Vector<int> m_buf_faces;
};
using aeFaceSetPtr = std::shared_ptr<aeFaceSet>;


class aePolyMesh : public aeSchema
{
    using super = aeSchema;
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
    void writeSampleBody();

    struct SubmeshBuffer
    {
        Vector<int> indices;
        aeTopology topology = aeTopology::Triangles;
    };

    AbcGeom::OPolyMeshSchema m_schema;
    AbcGeom::OV2fGeomParam m_uv1_param;
    AbcGeom::OC4fGeomParam m_colors_param;
    std::vector<aeFaceSetPtr> m_facesets;

    bool m_buf_visibility = true;

    Vector<int>   m_buf_faces;
    Vector<int>   m_buf_indices;

    Vector<abcV3> m_buf_points;
    Vector<abcV3> m_buf_velocities;

    Vector<abcV3> m_buf_normals;
    Vector<abcV2> m_buf_uv0;
    Vector<abcV2> m_buf_uv1;
    Vector<abcV4> m_buf_colors;

    std::vector<SubmeshBuffer> m_buf_submeshes;
    Vector<int> m_tmp_facecet;
};
