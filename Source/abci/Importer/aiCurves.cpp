#include "pch.h"
#include <Foundation/aiMath.h>
#include "aiCurves.h"

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
            CopyTo(m_positions,data.positions);
            CopyTo(m_numVertices, data.numVertices);
            data.count = m_positions.size();
        }
    }

    if (data.uvs)
    {
        if (!m_uvs.empty())
        {
            CopyTo(m_uvs,data.uvs);
        }
    }

    if (data.widths)
    {
        if (!m_widths.empty())
        {
            CopyTo(m_widths,data.widths);
        }
    }

	if (data.velocities)
	{
		if (!m_velocities.empty())
        {
            CopyTo(m_velocities,data.velocities);
        }
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
    bool interpolate = getConfig().interpolate_samples;

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
	bool interpolate = config.interpolate_samples;

	//if (!m_sample_index_changed)
	//	return;

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
        }

        ApplyScale(sample.m_velocities.data(), (int)sample.m_velocities.size(), -config.scale_factor);
	}
	else
	{
		if (sample.m_positions_prev.empty())
        {
            ResizeZeroClear(sample.m_velocities,sample.m_positions.size());
        }
		else
        {
            GenerateVelocities(sample.m_velocities.data(), sample.m_positions.data(), sample.m_positions_prev.data(),
                               (int) sample.m_positions.size(), -1 * config.vertex_motion_scale);
        }
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
