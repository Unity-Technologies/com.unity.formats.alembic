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

	if (data.velocities)
	{
		if (!m_velocities.empty())
			m_velocities.copy_to(data.velocities);
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
    bool interpolate = getConfig().interpolate_samples && m_current_time_offset > 0;

    readVisibility(sample, ss);

    // points
    if (summary.has_position)
    {
        {
            auto prop = m_schema.getPositionsProperty();
            prop.get(sample.m_position_sp, ss);
            if (interpolate)
            {
                prop.get(sample.m_position_sp2, ss2);
            }
        }
        {
            auto prop = m_schema.getNumVerticesProperty(); // Assume the same number
            prop.get(sample.m_numVertices_sp);
        }

    }

    if (summary.has_UVs)
    {
        auto prop = m_schema.getUVsParam();
        prop.getExpanded(sample.m_uvs_sp, ss);
        if (interpolate)
            prop.getExpanded(sample.m_uvs_sp2, ss2);
    }

    if (summary.has_widths)
    {
        auto prop = m_schema.getWidthsParam();
        prop.getExpanded(sample.m_widths_sp, ss);
        if (interpolate)
            prop.getExpanded(sample.m_widths_sp2, ss2);
    }

    if (summary.has_velocity)
	{
		auto prop = m_schema.getVelocitiesProperty();
		prop.get(sample.m_velocities_sp, ss);
	}

}

void aiCurves::cookSampleBody(aiCurvesSample &sample)
{
	auto& config = getConfig();
	int point_count = (int)sample.m_position_sp->size();
	bool interpolate = config.interpolate_samples && m_current_time_offset > 0;

	if (!m_sample_index_changed)
		return;

	if (!getSummary().has_velocity)
		sample.m_positions.swap(sample.m_positions_prev);

	if (m_summary.has_position)
	{
		Assign(sample.m_numVertices, sample.m_numVertices_sp, sample.m_numVertices_sp->size());
		Assign(sample.m_positions, sample.m_position_sp, point_count);
		if (interpolate)
		{
			Assign(sample.m_positions2, sample.m_position_sp2, point_count);
			Lerp(sample.m_positions.data(), sample.m_positions.data(), sample.m_positions2.data(),
				point_count, m_current_time_offset);
		}

	}

	if (m_summary.has_UVs)
	{
		Assign(sample.m_uvs, sample.m_uvs_sp.getVals(), sample.m_uvs_sp.getVals()->size());
		if (interpolate)
		{
			Assign(sample.m_uvs2, sample.m_uvs_sp2.getVals(), sample.m_uvs_sp2.getVals()->size());
			Lerp(sample.m_uvs.data(), sample.m_uvs.data(), sample.m_uvs2.data(),
				sample.m_uvs.size(), m_current_time_offset);
		}
	}

	if (m_summary.has_widths)
	{
		Assign(sample.m_widths, sample.m_widths_sp.getVals(), sample.m_widths_sp.getVals()->size());
		if (interpolate)
		{
			Assign(sample.m_widths2, sample.m_widths_sp2.getVals(), sample.m_widths_sp2.getVals()->size());
			Lerp(sample.m_widths.data(), sample.m_widths.data(), sample.m_widths2.data(),
				sample.m_widths.size(), m_current_time_offset);
		}
	}

	if (config.swap_handedness)
	{
		SwapHandedness(sample.m_positions.data(), (int)sample.m_positions.size());
	}
	if (config.scale_factor != 1.0f)
	{
		ApplyScale(sample.m_positions.data(), (int)sample.m_positions.size(), config.scale_factor);
	}

	if (m_summary.has_velocity)
	{
		Assign(sample.m_velocities, sample.m_velocities_sp, point_count);
		if (config.swap_handedness)
		{
			SwapHandedness(sample.m_velocities.data(), (int)sample.m_velocities.size());
			for (int i=0;i<sample.m_velocities.size();++i)
				sample.m_velocities[i] -= sample.m_velocities[i];
		}
	}
	else
	{
		if (sample.m_positions_prev.empty())
			sample.m_velocities.resize_zeroclear(sample.m_positions.size());
		else
			GenerateVelocities(sample.m_velocities.data(), sample.m_positions.data(), sample.m_positions_prev.data(),
				(int)sample.m_positions.size(), -1 * config.vertex_motion_scale);
	}

}

void aiCurves::updateSummary()
{
    {
        auto prop = m_schema.getPositionsProperty();
        m_summary.has_position = prop.valid() && prop.getNumSamples() > 0;
    }
    {
        auto prop = m_schema.getUVsParam();
        m_summary.has_UVs = prop.valid() && prop.getNumSamples() > 0;
    }
    {
        auto prop = m_schema.getWidthsParam();
        m_summary.has_widths = prop.valid() && prop.getNumSamples() > 0;
    }
	{
		auto prop = m_schema.getVelocitiesProperty();
		m_summary.has_velocity = prop.valid() && prop.getNumSamples() > 0;
	}
}
