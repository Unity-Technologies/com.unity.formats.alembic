#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePolyMesh.h"
#include "aiMisc.h"
#include "aiMath.h"


aeFaceSet::aeFaceSet(aeObject * parent, const char * name, uint32_t tsi)
    : m_abc(new abcFaceSet(parent->getAbcObject(), name, tsi))
    , m_schema(dynamic_cast<abcFaceSet&>(*m_abc).getSchema())
{
}

void aeFaceSet::writeSample(const aeFaceSetData & data)
{
    AbcGeom::OFaceSetSchema::Sample sample;
    sample.setFaces(Abc::Int32ArraySample(data.faces, data.face_count));
    m_schema.set(sample);
}


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
    m_buf_faces.assign(data.faces, data.faces + data.face_count);
    m_buf_indices.assign(data.indices, data.indices + data.index_count);

    m_buf_points.assign(data.points, data.points + data.point_count);
    m_buf_velocities.assign(data.velocities, data.velocities + data.point_count);

    m_buf_normals.assign(data.normals, data.normals + (data.normal_count ? data.normal_count : data.point_count));
    m_buf_normal_indices.assign(data.normal_indices, data.normal_indices + data.normal_index_count);

    m_buf_uv0.assign(data.uv0, data.uv0 + (data.uv0_count ? data.uv0_count : data.point_count));
    m_buf_uv0_indices.assign(data.uv0_indices, data.uv0_indices + data.uv0_index_count);

    m_buf_uv1.assign(data.uv1, data.uv1 + (data.uv1_count ? data.uv1_count : data.point_count));
    m_buf_uv1_indices.assign(data.uv1_indices, data.uv1_indices + data.uv1_index_count);

    m_buf_colors.assign(data.colors, data.colors + (data.colors_count ? data.colors_count : data.point_count));
    m_buf_colors_indices.assign(data.colors_indices, data.colors_indices + data.colors_index_count);

    m_ctx->addAsyncTask([this]() { writeSampleBody(); });
}

int aePolyMesh::addFaceSet(const char *name)
{
    auto fs = new aeFaceSet(this, name, m_tsi);
    int ret = (int)m_facesets.size();
    m_facesets.emplace_back(fs);
    return ret;
}

void aePolyMesh::writeFaceSetSample(int faceset_index, const aeFaceSetData& data)
{
    if (faceset_index >= (int)m_facesets.size()) {
        abciDebugLog("aePolyMesh::writeFaceSetSample(): invalid index");
        return;
    }
    m_facesets[faceset_index]->writeSample(data);
}

void aePolyMesh::writeSampleBody()
{
    const auto &conf = getConfig();

    // handle swap handedness
    if (conf.swap_handedness) {
        SwapHandedness(m_buf_points.data(), (int)m_buf_points.size());
        SwapHandedness(m_buf_velocities.data(), (int)m_buf_velocities.size());
        SwapHandedness(m_buf_normals.data(), (int)m_buf_normals.size());
    }

    // handle scale factor
    float scale = conf.scale;
    if (scale != 1.0f) {
        ApplyScale(m_buf_points.data(), (int)m_buf_points.size(), scale);
        ApplyScale(m_buf_velocities.data(), (int)m_buf_velocities.size(), scale);
    }

    // if face counts are empty, assume all faces are triangles
    if (m_buf_faces.empty()) {
        m_buf_faces.resize((int)(m_buf_indices.size() / 3), 3);
    }

    // handle swap face option
    if (conf.swap_faces) {
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
        do_swap(m_buf_uv0_indices);
    }


    // write!
    AbcGeom::OPolyMeshSchema::Sample sample;
    sample.setFaceIndices(Abc::Int32ArraySample(m_buf_indices.data(), m_buf_indices.size()));
    sample.setFaceCounts(Abc::Int32ArraySample(m_buf_faces.data(), m_buf_faces.size()));
    sample.setPositions(Abc::P3fArraySample(m_buf_points.data(), m_buf_points.size()));
    if (!m_buf_velocities.empty()) {
        sample.setVelocities(Abc::V3fArraySample(m_buf_velocities.data(), m_buf_velocities.size()));
    }
    if (!m_buf_normals.empty()) {
        AbcGeom::ON3fGeomParam::Sample sp;
        sp.setVals(Abc::V3fArraySample(m_buf_normals.data(), m_buf_normals.size()));
        if (!m_buf_normal_indices.empty())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_normal_indices.data(), m_buf_normal_indices.size()));
        else if(m_buf_normals.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        sample.setNormals(sp);
    }
    if (!m_buf_uv0.empty()) {
        AbcGeom::OV2fGeomParam::Sample sp;
        sp.setVals(Abc::V2fArraySample(m_buf_uv0.data(), m_buf_uv0.size()));
        if (!m_buf_uv0_indices.empty())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_uv0_indices.data(), m_buf_uv0_indices.size()));
        else if (m_buf_uv0.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        sample.setUVs(sp);
    }
    m_schema.set(sample);

    if (!m_buf_uv1.empty()) {
        if (!m_uv1_param) {
            bool indexed = !m_buf_uv1_indices.empty() || m_buf_uv1.size() == m_buf_points.size();
            m_uv1_param.reset(new AbcGeom::OV2fGeomParam(
                m_schema.getArbGeomParams(), "uv1", indexed, AbcGeom::GeometryScope::kConstantScope, m_buf_uv1.size(), getTimeSamplingIndex()));
        }

        AbcGeom::OV2fGeomParam::Sample sp;
        sp.setVals(Abc::V2fArraySample(m_buf_uv1.data(), m_buf_uv1.size()));
        if (!m_buf_uv1_indices.empty())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_uv1_indices.data(), m_buf_uv1_indices.size()));
        else if (m_buf_uv1.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        m_uv1_param->set(sp);
    }

    if (!m_buf_colors.empty()) {
        if (!m_colors_param) {
            bool indexed = !m_buf_colors_indices.empty() || m_buf_colors.size() == m_buf_points.size();
            m_colors_param.reset(new AbcGeom::OC4fGeomParam(
                m_schema.getArbGeomParams(), "rgba", indexed, AbcGeom::GeometryScope::kConstantScope, m_buf_uv1.size(), getTimeSamplingIndex()));
        }

        AbcGeom::OC4fGeomParam::Sample sp;
        sp.setVals(Abc::C4fArraySample((abcC4*)m_buf_colors.data(), m_buf_colors.size()));
        if (!m_buf_colors_indices.empty())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_colors_indices.data(), m_buf_colors_indices.size()));
        else if (m_buf_colors.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        m_colors_param->set(sp);
    }
}
