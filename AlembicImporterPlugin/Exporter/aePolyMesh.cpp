#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePolyMesh.h"

aePolyMesh::aePolyMesh(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new abcPolyMesh(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
    , m_schema(getAbcObject().getSchema())
{
}

abcPolyMesh& aePolyMesh::getAbcObject()
{
    return dynamic_cast<abcPolyMesh&>(*m_abc);
}

abcProperties* aePolyMesh::getAbcProperties()
{
    return &m_schema;
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
            m_buf_positions.resize(data.vertexCount);
            memcpy(&m_buf_positions[0], data.positions, sizeof(abcV3)*data.vertexCount);
            for (abcV3 &v : m_buf_positions) { v.x *= -1.0f; }
            data.positions = &m_buf_positions[0];
        }

        // convert normal
        if (data.normals != nullptr) {
            m_buf_normals.resize(data.vertexCount);
            memcpy(&m_buf_normals[0], data.normals, sizeof(abcV3)*data.vertexCount);
            for (abcV3 &v : m_buf_normals) { v.x *= -1.0f; }
            data.normals = &m_buf_normals[0];
        }
    }

    if (data.faces == nullptr) {
        // assume all faces are triangles

        int vertices_per_primitive = 3;
        int num_primitives = data.indexCount / vertices_per_primitive;
        m_buf_faces.resize(num_primitives);
        std::fill(m_buf_faces.begin(), m_buf_faces.end(), vertices_per_primitive);
        data.faces = &m_buf_faces[0];
        data.faceCount = m_buf_faces.size();
    }

    sample.setPositions(Abc::P3fArraySample(data.positions, data.vertexCount));
    sample.setFaceIndices(Abc::Int32ArraySample(data.indices, data.indexCount));
    sample.setFaceCounts(Abc::Int32ArraySample(data.faces, data.faceCount));
    if (data.normals != nullptr) {
        sample_normals.setIndices(Abc::UInt32ArraySample((uint32_t*)data.indices, data.indexCount));
        sample_normals.setVals(Abc::V3fArraySample(data.normals, data.vertexCount));
        sample.setNormals(sample_normals);
    }
    if (data.uvs != nullptr) {
        sample_uvs.setIndices(Abc::UInt32ArraySample((uint32_t*)data.indices, data.indexCount));
        sample_uvs.setVals(Abc::V2fArraySample(data.uvs, data.vertexCount));
        sample.setUVs(sample_uvs);
    }
    m_schema.set(sample);
}
