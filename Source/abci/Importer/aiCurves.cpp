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
}

aiCurvesSample::aiCurvesSample(aiCurves *schema) : super(schema)
{
}

void aiCurvesSample::getSummary(aiCurvesSampleSummary &dst)
{
    dst.count = (int)m_positions.size();
}

void aiCurvesSample::fillData(aiCurvesData data)
{
    data.visibility = visibility;
    if (data.positions)
    {
        if (!m_positions.empty())
            m_positions.copy_to(data.positions);
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

    //readVisibility(sample, ss);

    // points
    if (m_summary.has_position)
    {
        auto prop = m_schema.getPositionsProperty();
        prop.get(sample.m_position_sp, ss);
       /* if (summary.interpolate_points)
        {
            prop.get(sample.m_points_sp2, ss2);
        }*/
    }

}

void aiCurves::cookSampleBody(aiCurvesSample &sample)
{
    auto& config = getConfig();

    int point_count = (int)sample.m_position_sp->size();
    if (m_sample_index_changed)
    {
        Assign(sample.m_positions, sample.m_position_sp, point_count);
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

   sample.m_positions_ref = sample.m_positions; // or interpolate
}

void aiCurves::updateSummary()
{
    auto pos = m_schema.getPositionsProperty();
    m_summary.has_position = pos.valid() && pos.getNumSamples() > 0;
    if (m_summary.has_position)
        m_summary.copnstant_position = pos.isConstant();

}
