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

abcProperties aePolyMesh::getAbcProperties()
{
    return m_schema.getUserProperties();
}

void aePolyMesh::writeSample(const aePolyMeshSampleData &data_)
{
    aePolyMeshSampleData data = data_;
    const auto &conf = getConfig();


    // handle swapHandedness and scaling for positions, velocities
    if (conf.swapHandedness || conf.scale != 1.0f) {
        float scale = conf.scale;
        {
            m_buf_positions.resize(data.positionCount);
            // sadly, memcpy() is way faster than std::copy() on VC
            memcpy(&m_buf_positions[0], data.positions, sizeof(abcV3) * data.positionCount);
            if (conf.swapHandedness) {
                for (auto &v : m_buf_positions) { v.x *= -1.0f; }
            }
            if (scale != 1.0f) {
                for (auto &v : m_buf_positions) { v *= scale; }
            }
            data.positions = &m_buf_positions[0];
        }

        if (data.velocities != nullptr) {
            m_buf_velocities.resize(data.positionCount);
            memcpy(&m_buf_velocities[0], data.velocities, sizeof(abcV3) * data.positionCount);
            if (conf.swapHandedness) {
                for (auto &v : m_buf_velocities) { v.x *= -1.0f; }
            }
            if (scale != 1.0f) {
                for (auto &v : m_buf_velocities) { v *= scale; }
            }
            data.velocities = &m_buf_velocities[0];
        }
    }

    // handle swapHandedness for normals
    if (conf.swapHandedness) {
        if (data.normals != nullptr) {
            m_buf_normals.resize(data.normalCount);
            memcpy(&m_buf_normals[0], data.normals, sizeof(abcV3)*data.normalCount);
            for (auto &v : m_buf_normals) { v.x *= -1.0f; }
            data.normals = &m_buf_normals[0];
        }
    }

    if (data.faces == nullptr) {
        // assume all faces are triangles
        int vertices_per_primitive = 3;
        int num_primitives = data.indexCount / vertices_per_primitive;
        m_buf_faces.resize(num_primitives);
        for (auto &v : m_buf_faces) { v = vertices_per_primitive; }
        data.faces = &m_buf_faces[0];
        data.faceCount = (int)m_buf_faces.size();
    }

    // handle swapFace option
    if (conf.swapFaces) {
        auto swap_routine = [&](std::vector<int>& dst, const int *src) {
            int i = 0;
            for (int fi = 0; fi < data.faceCount; ++fi) {
                int ngon = data.faces[i];
                for (int ni = 0; ni < ngon; ++ni) {
                    int ini = ngon - ni - 1;
                    dst[i + ni] = src[i + ini];
                }
                i += ngon;
            }
        };

        {
            m_buf_indices.resize(data.indexCount);
            swap_routine(m_buf_indices, data.indices);
            data.indices = &m_buf_indices[0];
        }
        if (data.normalIndices && data.normalIndexCount) {
            m_buf_normal_indices.resize(data.normalIndexCount);
            swap_routine(m_buf_normal_indices, data.normalIndices);
            data.normalIndices = &m_buf_normal_indices[0];
        }
        if (data.uvIndices && data.uvIndexCount) {
            m_buf_uv_indices.resize(data.uvIndexCount);
            swap_routine(m_buf_uv_indices, data.uvIndices);
            data.uvIndices = &m_buf_uv_indices[0];
        }
    }


    // write!
    AbcGeom::OPolyMeshSchema::Sample sample;
    AbcGeom::ON3fGeomParam::Sample sample_normals;
    AbcGeom::OV2fGeomParam::Sample sample_uvs;
    sample.setPositions(Abc::P3fArraySample(data.positions, data.positionCount));
    sample.setFaceIndices(Abc::Int32ArraySample(data.indices, data.indexCount));
    sample.setFaceCounts(Abc::Int32ArraySample(data.faces, data.faceCount));
    if (data.velocities != nullptr) {
        sample.setVelocities(Abc::V3fArraySample(data.velocities, data.positionCount));
    }
    if (data.normals != nullptr) {
        if (data.normalCount == 0) {
            data.normalCount = data.positionCount;
            data.normalIndices = data.indices;
            data.normalIndexCount = data.indexCount;
        }
        sample_normals.setIndices(Abc::UInt32ArraySample((uint32_t*)data.normalIndices, data.normalIndexCount));
        sample_normals.setVals(Abc::V3fArraySample(data.normals, data.normalCount));
        sample.setNormals(sample_normals);
    }
    if (data.uvs != nullptr) {
        if (data.uvCount == 0) {
            data.uvIndices = data.indices;
            data.uvCount = data.positionCount;
            data.uvIndexCount = data.indexCount;
        }
        sample_uvs.setIndices(Abc::UInt32ArraySample((uint32_t*)data.uvIndices, data.uvIndexCount));
        sample_uvs.setVals(Abc::V2fArraySample(data.uvs, data.uvCount));
        sample.setUVs(sample_uvs);
    }
    m_schema.set(sample);
}
