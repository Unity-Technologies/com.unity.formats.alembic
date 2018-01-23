#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePolyMesh.h"

aePolyMesh::aePolyMesh(aeObject *parent, const char *name, uint32_t tsi)
    : super(parent->getContext(), parent, new abcPolyMesh(parent->getAbcObject(), name, tsi), tsi)
    , m_schema(getAbcObject().getSchema())
{
}

abcPolyMesh& aePolyMesh::getAbcObject()
{
    return dynamic_cast<abcPolyMesh&>(*m_abc);
}

abcProperties aePolyMesh::getAbcProperties()
{
    return m_schema.getUserProperties();
}

size_t aePolyMesh::getNumSamples()
{
    return m_schema.getNumSamples();
}

void aePolyMesh::setFromPrevious()
{
    m_schema.setFromPrevious();
}

void aePolyMesh::writeSample(const aePolyMeshData &data)
{
    m_buf_indices.assign(data.indices, data.indices + data.indexCount);
    m_buf_faces.assign(data.faces, data.faces + data.faceCount);

    m_buf_positions.assign(data.positions, data.positions + data.positionCount);
    if (data.velocities) {
        m_buf_velocities.assign(data.velocities, data.velocities + data.positionCount);
    }
    else {
        m_buf_velocities.clear();
    }

    if (data.normals) {
        m_buf_normals.assign(data.normals, data.normals + data.normalCount);
        if (data.normalIndices) {
            m_buf_normal_indices.assign(data.normalIndices, data.normalIndices + data.normalIndexCount);
        }
    }
    else {
        m_buf_normals.clear();
        m_buf_normal_indices.clear();
    }

    if (data.uvs) {
        m_buf_uvs.assign(data.uvs, data.uvs + data.uvCount);
        if (data.normalIndices) {
            m_buf_uv_indices.assign(data.uvIndices, data.uvIndices + data.uvIndexCount);
        }
    }
    else {
        m_buf_uvs.clear();
        m_buf_uv_indices.clear();
    }

    m_ctx->addAsyncTask([this]() { doWriteSample(); });
}

void aePolyMesh::doWriteSample()
{
    const auto &conf = getConfig();

    // handle swap handedness
    if (conf.swapHandedness) {
        for (auto &v : m_buf_positions) { v.x *= -1.0f; }
        for (auto &v : m_buf_velocities) { v.x *= -1.0f; }
        for (auto &v : m_buf_normals) { v.x *= -1.0f; }
    }

    // handle scale factor
    float scale = conf.scale;
    if (scale != 1.0f) {
        for (auto &v : m_buf_positions) { v *= scale; }
        for (auto &v : m_buf_velocities) { v *= scale; }
    }

    // if face counts are empty, assume all faces are triangles
    if (m_buf_faces.empty()) {
        m_buf_faces.resize((int)(m_buf_indices.size() / 3), 3);
    }

    // handle swap face option
    if (conf.swapFaces) {
        RawVector<int> face_indices;
        auto do_swap = [&](RawVector<int>& dst) {
            int i = 0;
            int num_faces = (int)m_buf_faces.size();
            for (int fi = 0; fi < num_faces; ++fi) {
                int ngon = m_buf_faces[i];
                face_indices.assign(&dst[i], &dst[i + ngon]);
                for (int ni = 0; ni < ngon; ++ni) {
                    int ini = ngon - ni - 1;
                    dst[i + ni] = face_indices[ini];
                }
                i += ngon;
            }
        };
        do_swap(m_buf_indices);
        do_swap(m_buf_normal_indices);
        do_swap(m_buf_uv_indices);
    }

    // write!
    AbcGeom::OPolyMeshSchema::Sample sample;
    AbcGeom::ON3fGeomParam::Sample sample_normals;
    AbcGeom::OV2fGeomParam::Sample sample_uvs;
    sample.setPositions(Abc::P3fArraySample(m_buf_positions.data(), m_buf_positions.size()));
    sample.setFaceIndices(Abc::Int32ArraySample(m_buf_indices.data(), m_buf_indices.size()));
    sample.setFaceCounts(Abc::Int32ArraySample(m_buf_faces.data(), m_buf_faces.size()));
    if (!m_buf_velocities.empty()) {
        sample.setVelocities(Abc::V3fArraySample(m_buf_velocities.data(), m_buf_velocities.size()));
    }
    if (!m_buf_normals.empty()) {
        sample_normals.setVals(Abc::V3fArraySample(m_buf_normals.data(), m_buf_normals.size()));
        if(!m_buf_normal_indices.empty())
            sample_normals.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_normal_indices.data(), m_buf_normal_indices.size()));
        sample.setNormals(sample_normals);
    }
    if (!m_buf_uvs.empty()) {
        sample_uvs.setVals(Abc::V2fArraySample(m_buf_uvs.data(), m_buf_uvs.size()));
        if (!m_buf_uv_indices.empty())
            sample_uvs.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_uv_indices.data(), m_buf_uv_indices.size()));
        sample.setUVs(sample_uvs);
    }
    m_schema.set(sample);
}
