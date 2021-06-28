#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePolyMesh.h"
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
    m_buf_visibility = data.visibility;
    Assign(m_buf_points, data.points,  data.point_count);
    Assign(m_buf_normals, data.normals,  data.point_count);
    Assign(m_buf_uv0, data.uv0,  data.point_count);
    Assign(m_buf_uv1, data.uv1,  data.point_count);
    Assign(m_buf_colors,data.colors, data.point_count);

    m_buf_submeshes.resize(data.submesh_count);
    for (int smi = 0; smi < data.submesh_count; ++smi)
    {
        auto& src = data.submeshes[smi];
        auto& dst = m_buf_submeshes[smi];
        Assign(dst.indices, src.indices,  src.index_count);
        dst.topology = src.topology;
    }

    m_ctx->addAsync([this]() { writeSampleBody(); });
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
    if (faceset_index >= (int)m_facesets.size())
    {
        DebugLog("aePolyMesh::writeFaceSetSample(): invalid index");
        return;
    }
    m_facesets[faceset_index]->writeSample(data);
}

void aePolyMesh::writeSampleBody()
{
    const auto &conf = getConfig();

    // handle swap handedness
    if (conf.swap_handedness)
    {
        SwapHandedness(m_buf_points.data(), (int)m_buf_points.size());
        SwapHandedness(m_buf_velocities.data(), (int)m_buf_velocities.size());
        SwapHandedness(m_buf_normals.data(), (int)m_buf_normals.size());
    }

    // handle scale factor
    float scale = conf.scale_factor;
    if (scale != 1.0f)
    {
        ApplyScale(m_buf_points.data(), (int)m_buf_points.size(), scale);
        ApplyScale(m_buf_velocities.data(), (int)m_buf_velocities.size(), scale);
    }

    auto facecount = 0;
    auto attribScope = Alembic::AbcGeom::GeometryScope::kFacevaryingScope;

    // process submesh data if present
    {
        m_buf_indices.resize(0);
        m_buf_faces.resize(0);

        int offset_faces = 0;
        for (size_t smi = 0; smi < m_buf_submeshes.size(); ++smi)
        {
            auto& sm = m_buf_submeshes[smi];
            m_buf_indices.insert(m_buf_indices.end(), sm.indices.begin(), sm.indices.end());

            int ngon = 0;
            int face_count = 0;
            switch (sm.topology)
            {
                case aeTopology::Lines:
                    ngon = 2;
                    face_count = (int)sm.indices.size() / 2;
                    break;
                case aeTopology::Triangles:
                    ngon = 3;
                    face_count = (int)sm.indices.size() / 3;
                    break;
                case aeTopology::Quads:
                    ngon = 4;
                    face_count = (int)sm.indices.size() / 4;
                    break;
                default: // points
                    ngon = 1;
                    face_count = (int)sm.indices.size();
                    break;
            }
            m_buf_faces.resize(m_buf_faces.size() + face_count, ngon);
            facecount+= face_count * ngon;

            if (smi < m_facesets.size())
            {
                m_tmp_facecet.resize(face_count);
                std::iota(m_tmp_facecet.begin(), m_tmp_facecet.end(), offset_faces);

                aeFaceSetData fsd;
                fsd.faces = m_tmp_facecet.data();
                fsd.face_count = (int)m_tmp_facecet.size();
                m_facesets[smi]->writeSample(fsd);
            }

            offset_faces += face_count;
        }

        attribScope = facecount == m_buf_indices.size() ? Alembic::AbcGeom::GeometryScope::kFacevaryingScope : Alembic::AbcGeom::GeometryScope::kVertexScope;
    }

    // handle swap face option
    if (conf.swap_faces)
    {
        Vector<int> face_indices;
        auto do_swap = [&](Vector<int>& dst) {
                if (dst.empty())
                {
                    return;
                }
                int i = 0;
                int num_faces = (int)m_buf_faces.size();
                for (int fi = 0; fi < num_faces; ++fi)
                {
                    int ngon = m_buf_faces[fi];
                    face_indices.assign(&dst[i], &dst[i + ngon]);
                    for (int ni = 0; ni < ngon; ++ni)
                    {
                        int ini = ngon - ni - 1;
                        dst[i + ni] = face_indices[ini];
                    }
                    i += ngon;
                }
            };
        do_swap(m_buf_indices);
    }


    // write!
    writeVisibility(m_buf_visibility);

    AbcGeom::OPolyMeshSchema::Sample sample;
    sample.setFaceIndices(Abc::Int32ArraySample(m_buf_indices.data(), m_buf_indices.size()));
    sample.setFaceCounts(Abc::Int32ArraySample(m_buf_faces.data(), m_buf_faces.size()));
    sample.setPositions(Abc::P3fArraySample(m_buf_points.data(), m_buf_points.size()));
    if (!m_buf_velocities.empty())
    {
        sample.setVelocities(Abc::V3fArraySample(m_buf_velocities.data(), m_buf_velocities.size()));
    }
    if (!m_buf_normals.empty())
    {
        AbcGeom::ON3fGeomParam::Sample sp;
        sp.setVals(Abc::V3fArraySample(m_buf_normals.data(), m_buf_normals.size()));
        if (m_buf_normals.size() == m_buf_points.size())
        {
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        }

        sp.setScope(attribScope);
        sample.setNormals(sp);
    }
    if (!m_buf_uv0.empty())
    {
        AbcGeom::OV2fGeomParam::Sample sp;
        sp.setVals(Abc::V2fArraySample(m_buf_uv0.data(), m_buf_uv0.size()));
        sp.setScope(attribScope);

        if (m_buf_uv0.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        sample.setUVs(sp);
    }
    m_schema.set(sample);

    if (!m_buf_uv1.empty())
    {
        if (!m_uv1_param.valid())
        {
            bool indexed = m_buf_uv1.size() == m_buf_points.size();
            m_uv1_param = AbcGeom::OV2fGeomParam(
                m_schema.getArbGeomParams(), "uv1", indexed, AbcGeom::GeometryScope::kConstantScope, m_buf_uv1.size(), getTimeSamplingIndex());
        }

        AbcGeom::OV2fGeomParam::Sample sp;
        sp.setVals(Abc::V2fArraySample(m_buf_uv1.data(), m_buf_uv1.size()));
        sp.setScope(attribScope);

        if (m_buf_uv1.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        m_uv1_param.set(sp);
    }

    if (!m_buf_colors.empty())
    {
        if (!m_colors_param.valid())
        {
            bool indexed = m_buf_colors.size() == m_buf_points.size();
            m_colors_param = AbcGeom::OC4fGeomParam(
                m_schema.getArbGeomParams(), "rgba", indexed, AbcGeom::GeometryScope::kConstantScope, m_buf_uv1.size(), getTimeSamplingIndex());
        }

        AbcGeom::OC4fGeomParam::Sample sp;
        sp.setVals(Abc::C4fArraySample((abcC4*)m_buf_colors.data(), m_buf_colors.size()));
        sp.setScope(attribScope);

        if (m_buf_colors.size() == m_buf_points.size())
            sp.setIndices(Abc::UInt32ArraySample((const uint32_t*)m_buf_indices.data(), m_buf_indices.size()));
        m_colors_param.set(sp);
    }
}
