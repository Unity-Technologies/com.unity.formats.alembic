#include "pch.h"
#include <Foundation/aiMath.h>
#include "aiCurves.h"


namespace
{
    template<class T, class U>
    inline void Assign(RawVector<T>& dst, const U& src, int point_count)
    {
        dst.resize_discard(point_count);
        size_t count = std::min<size_t>(point_count, src->size());
        auto src_data = src->get();
        for (size_t i = 0; i < count; ++i)
            dst[i] = (T)src_data[i];
    }

    template<class T, class AbcArraySample>
    inline void Remap(RawVector<T>& dst, const AbcArraySample& src, const RawVector<int>& indices)
    {
        if (indices.empty())
        {
            dst.assign(src.get(), src.get() + src.size());
        }
        else
        {
            dst.resize_discard(indices.size());
            CopyWithIndices(dst.data(), src.get(), indices);
        }
    }

    template<class T, class IndexArray>
    inline void CopyWithIndices(T *dst, const T *src, const IndexArray& indices)
    {
        if (!dst || !src) { return; }
        size_t size = indices.size();
        for (size_t i = 0; i < (int)size; ++i)
        {
            dst[i] = src[indices[i]];
        }
    }
}

aiCurvesSample::aiCurvesSample(aiCurves *schema) : super(schema)
{
}

void aiCurvesSample::getSummary(aiCurvesSampleSummary &dst)
{
    dst.positionCount = m_positions.size();
    dst.numVerticesCount = m_numVertices.size();
}

void aiCurvesSample::fillData(aiCurvesData& data)
{
    data.visibility = visibility;
    if (data.positions)
    {
        if (!m_positions.empty()) {
            m_positions.copy_to(data.positions);
            m_numVertices.copy_to(data.numVertices);
            data.count = m_positions.size();
        }
    }

    if (data.uvs)
    {
        if (!m_uvs.empty())
            m_uvs.copy_to(data.uvs);
    }

    if (data.widths)
    {
        if (!m_widths.empty())
            m_widths.copy_to(data.widths);
    }
}

aiCurves::aiCurves(aiObject *parent, const abcObject &abc) : super(parent, abc)
{
    updateSummary();
}

aiCurvesSample *aiCurves::newSample()
{
    return new Sample(this);
}

void aiCurves::readSampleBody(aiCurvesSample &sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);
    auto& summary = getSummary();

    readVisibility(sample, ss);

    // points
    if (m_summary.has_position)
    {
        {
            auto prop = m_schema.getPositionsProperty();
            prop.get(sample.m_position_sp, ss);
        }
        {
            auto prop = m_schema.getNumVerticesProperty();
            prop.get(sample.m_numVertices_sp);
        }
       /* if (summary.interpolate_points)
        {
            prop.get(sample.m_points_sp2, ss2);
        }*/
    }

    if (m_summary.has_UVs)
    {
        auto prop = m_schema.getUVsParam();
        prop.getExpanded(sample.m_uvs_sp, ss);
    }

    if (m_summary.has_widths)
    {
        auto prop = m_schema.getWidthsParam();
        prop.getExpanded(sample.m_widths_sp, ss);
    }

}

void aiCurves::cookSampleBody(aiCurvesSample &sample)
{
    auto& config = getConfig();

    int point_count = (int)sample.m_position_sp->size();
    if (m_sample_index_changed)
    {
        Assign(sample.m_positions, sample.m_position_sp, point_count);
        Assign(sample.m_numVertices, sample.m_numVertices_sp, sample.m_numVertices_sp->size());
        Assign(sample.m_uvs, sample.m_uvs_sp.getVals(), sample.m_uvs_sp.getVals()->size());
        Assign(sample.m_widths, sample.m_widths_sp.getVals(), sample.m_uvs_sp.getVals()->size());
    }
    // interpolate
    if (config.swap_handedness)
    {
        SwapHandedness(sample.m_positions.data(), (int) sample.m_positions.size());
    }
    if (config.scale_factor != 1.0f)
    {
        ApplyScale(sample.m_positions.data(), (int) sample.m_positions.size(), config.scale_factor);
    }

   //sample.m_positions_ref = sample.m_positions; // or interpolate
}

void aiCurves::updateSummary()
{
    {
        auto prop = m_schema.getPositionsProperty();
        m_summary.has_position = prop.valid() && prop.getNumSamples() > 0;
        if (m_summary.has_position)
            m_summary.constant_position = prop.isConstant();
    }
    {
        auto prop = m_schema.getUVsParam();
        m_summary.has_UVs = prop.valid() && prop.getNumSamples() > 0;
    }
    {
        auto prop = m_schema.getWidthsParam();
        m_summary.has_widths = prop.valid() && prop.getNumSamples() > 0;
    }
}
