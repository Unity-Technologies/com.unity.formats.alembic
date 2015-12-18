#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePolyMesh.h"

aePolyMesh::aePolyMesh(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new AbcGeom::OPolyMesh(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
    , m_schema(getAbcObject().getSchema())
{
}

AbcGeom::OPolyMesh& aePolyMesh::getAbcObject()
{
    return dynamic_cast<AbcGeom::OPolyMesh&>(*m_abc);
}

void aePolyMesh::writeSample(const aePolyMeshSampleData &data_)
{
    AbcGeom::OPolyMeshSchema::Sample sample;
    AbcGeom::ON3fGeomParam::Sample sample_normals;
    AbcGeom::OV2fGeomParam::Sample sample_uvs;

    aePolyMeshSampleData data = data_;
    if (getConfig().swapHandedness) {
        // convert vertex
        {
            m_buf_positions.resize(data.vertex_count);
            memcpy(&m_buf_positions[0], data.positions, sizeof(abcV3)*data.vertex_count);
            for (abcV3 &v : m_buf_positions) { v.x *= -1.0f; }
            data.positions = &m_buf_positions[0];
        }

        // convert normal
        if (data.normals != nullptr) {
            m_buf_normals.resize(data.vertex_count);
            memcpy(&m_buf_normals[0], data.normals, sizeof(abcV3)*data.vertex_count);
            for (abcV3 &v : m_buf_normals) { v.x *= -1.0f; }
            data.normals = &m_buf_normals[0];
        }
    }

    if (data.normals != nullptr) {
        sample_normals.setIndices(Abc::UInt32ArraySample((uint32_t*)data.indices, data.index_count));
        sample_normals.setVals(Abc::V3fArraySample(data.normals, data.vertex_count));
        sample.setNormals(sample_normals);
    }
    if (data.uvs != nullptr) {
        sample_uvs.setIndices(Abc::UInt32ArraySample((uint32_t*)data.indices, data.index_count));
        sample_uvs.setVals(Abc::V2fArraySample(data.uvs, data.vertex_count));
        sample.setUVs(sample_uvs);
    }

    sample.setPositions(Abc::P3fArraySample(data.positions, data.vertex_count));
    sample.setFaceIndices(Abc::Int32ArraySample(data.indices, data.index_count));
    if (data.faces != nullptr) {
        sample.setFaceCounts(Abc::Int32ArraySample(data.faces, data.face_count));
    }
    else {
        // assume all faces are triangle
        int num_faces = data.index_count / 3;
        m_buf_faces.resize(num_faces);
        std::fill(m_buf_faces.begin(), m_buf_faces.end(), 3);
        sample.setFaceCounts(Abc::Int32ArraySample(m_buf_faces));
    }

    m_schema.set(sample);
}
