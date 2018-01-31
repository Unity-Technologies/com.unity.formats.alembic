#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPoints.h"
#include "aiMisc.h"
#include "aiMath.h"

// ---

aiPointsSample::aiPointsSample(aiPoints *schema)
    : super(schema)
{
}

void aiPointsSample::fillData(aiPointsData &data)
{
    if (data.points)
        m_points.copy_to(data.points);
    if (data.velocities)
        m_velocities.copy_to(data.velocities);
    if (data.ids)
        m_ids.copy_to(data.ids);
    data.center = m_bb_center;
    data.size = m_bb_size;
}

void aiPointsSample::getSummary(aiPointsSampleSummary & dst)
{
    dst.count = (int)m_points.size();
}


aiPoints::aiPoints(aiObject *obj)
    : super(obj)
{
    updateSummary();
}

void aiPoints::updateSummary()
{
    auto points = m_schema.getPositionsProperty();
    auto velocities = m_schema.getVelocitiesProperty();
    auto ids = m_schema.getIdsProperty();

    m_summary.constant_points = points.isConstant();

    m_summary.has_velocities = velocities.valid();
    if(m_summary.has_velocities)
        m_summary.constant_points = points.isConstant();

    m_summary.has_ids = velocities.valid();
    if(m_summary.has_ids)
        m_summary.constant_id = ids.isConstant();
}

const aiPointsSummary& aiPoints::getSummary() const
{
    return m_summary;
}

aiPoints::Sample* aiPoints::newSample()
{
    return new Sample(this);
}

void aiPoints::readSample(Sample& sample, uint64_t idx)
{
    readSampleBody(sample, idx);
}

void aiPoints::cookSample(Sample & sample)
{
    cookSampleBody(sample);
}

void aiPoints::readSampleBody(Sample & sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);

    // points
    m_schema.getPositionsProperty().get(sample.m_points_sp, ss);

    // velocities
    sample.m_velocities_sp.reset();
    auto velocities_prop = m_schema.getVelocitiesProperty();
    if (velocities_prop.valid()) {
        velocities_prop.get(sample.m_velocities_sp, ss);
    }

    // IDs
    sample.m_ids_sp.reset();
    auto ids_prop = m_schema.getIdsProperty();
    if (ids_prop.valid()) {
        ids_prop.get(sample.m_ids_sp, ss);
    }
}

void aiPoints::cookSampleBody(Sample& sample)
{
    // interpolation is not supported for Points
    // (it is possible only when IDs are constant, which is very rare case..)
    if (!m_sample_index_changed)
        return;

    int point_count = (int)sample.m_points_sp->size();
    if (m_sort) {
        sample.m_sort_data.resize(point_count);
        for (int i = 0; i < point_count; ++i) {
            sample.m_sort_data[i].first = ((*sample.m_points_sp)[i] - getSortPosition()).length();
            sample.m_sort_data[i].second = i;
        }

#ifdef _WIN32
        concurrency::parallel_sort
#else
        std::sort
#endif
            (sample.m_sort_data.begin(), sample.m_sort_data.end(),
                [](const std::pair<float, int>& a, const std::pair<float, int>& b) { return a.first > b.first; });

        sample.m_points.resize_discard(point_count);
        for (int i = 0; i < point_count; ++i)
            sample.m_points[i] = (*sample.m_points_sp)[sample.m_sort_data[i].second];

        if (sample.m_velocities_sp) {
            sample.m_velocities.resize_discard(point_count);
            int count = std::min(point_count, (int)sample.m_velocities_sp->size());
            for (int i = 0; i < count; ++i)
                sample.m_velocities[i] = (*sample.m_velocities_sp)[sample.m_sort_data[i].second];
        }

        if (sample.m_ids_sp) {
            sample.m_ids.resize_discard(point_count);
            // I encountered the situation that point_count != ids_count...
            int count = std::min(point_count, (int)sample.m_ids_sp->size());
            for (int i = 0; i < count; ++i)
                sample.m_ids[i] = (uint32_t)(*sample.m_ids_sp)[sample.m_sort_data[i].second];
        }
    }
    else {
        sample.m_points.resize_discard(point_count);
        for (int i = 0; i < point_count; ++i)
            sample.m_points[i] = (*sample.m_points_sp)[i];

        if (sample.m_velocities_sp) {
            sample.m_velocities.resize_discard(point_count);
            int count = std::min(point_count, (int)sample.m_velocities_sp->size());
            for (int i = 0; i < count; ++i)
                sample.m_velocities[i] = (*sample.m_velocities_sp)[i];
        }

        if (sample.m_ids_sp) {
            sample.m_ids.resize_discard(point_count);
            int count = std::min(point_count, (int)sample.m_ids_sp->size());
            for (int i = 0; i < count; ++i)
                sample.m_ids[i] = (uint32_t)(*sample.m_ids_sp)[i];
        }
    }

    auto& config = getConfig();
    if (config.swap_handedness) {
        SwapHandedness(sample.m_points.data(), (int)sample.m_points.size());
        SwapHandedness(sample.m_velocities.data(), (int)sample.m_velocities.size());
    }
    if (config.scale_factor != 1.0f) {
        ApplyScale(sample.m_points.data(), (int)sample.m_points.size(), config.scale_factor);
        ApplyScale(sample.m_velocities.data(), (int)sample.m_velocities.size(), config.scale_factor);
    }
    {
        abcV3 bbmin, bbmax;
        MinMax(bbmin, bbmax, sample.m_points.data(), (int)sample.m_points.size());
        sample.m_bb_center = (bbmin + bbmax) * 0.5f;
        sample.m_bb_size = bbmax - bbmin;
    }
}


void aiPoints::setSort(bool v) { m_sort = v; }
bool aiPoints::getSort() const { return m_sort; }
void aiPoints::setSortPosition(const abcV3& v) { m_sort_position = v; }
const abcV3& aiPoints::getSortPosition() const { return m_sort_position; }
