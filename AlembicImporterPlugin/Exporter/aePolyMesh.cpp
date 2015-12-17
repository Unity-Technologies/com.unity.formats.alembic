#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aePolyMesh.h"

aePolyMesh::aePolyMesh(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new AbcGeom::OPolyMesh(parent->getAbcObject(), name))
    , m_schema(getAbcObject().getSchema())
{
}

AbcGeom::OPolyMesh& aePolyMesh::getAbcObject()
{
    return dynamic_cast<AbcGeom::OPolyMesh&>(*m_abc);
}

void aePolyMesh::writeSample(const aePolyMeshSampleData &data)
{
    if (data.normals != nullptr) {
        m_sample_normals.setIndices(Abc::Int32ArraySample(data.indices, data.index_count));
        m_sample_normals.setVals(Abc::V3fArraySample(data.normals, data.vertex_count));
        m_sample.setNormals(m_sample_normals);
    }
    if (data.uvs != nullptr) {
        m_sample_uvs.setIndices(Abc::Int32ArraySample(data.indices, data.index_count));
        m_sample_uvs.setVals(Abc::V2fArraySample(data.uvs, data.vertex_count));
        m_sample.setUVs(m_sample_uvs);
    }

    m_sample.setPositions(Abc::P3fArraySample(data.positions, data.vertex_count));
    m_sample.setFaceIndices(Abc::Int32ArraySample(data.indices, data.index_count));
    if (data.faces != nullptr) {
        m_sample.setFaceCounts(Abc::Int32ArraySample(data.faces, data.face_count));
    }
    else {
        // assume all faces are triangle
        int num_faces = data.index_count / 3;
        m_face_count_buf.resize(num_faces);
        std::fill(m_face_count_buf.begin(), m_face_count_buf.end(), 3);
        m_sample.setFaceCounts(Abc::Int32ArraySample(m_face_count_buf));
    }

    m_schema.set(m_sample);
    m_sample.reset();
    m_sample_normals.reset();
    m_sample_uvs.reset();
}
