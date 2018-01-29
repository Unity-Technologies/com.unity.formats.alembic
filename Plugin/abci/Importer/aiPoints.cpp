#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPoints.h"
#include "aiMisc.h"

// ---

aiPointsSample::aiPointsSample(aiPoints *schema)
    : super(schema)
{
}

aiPointsSample::~aiPointsSample()
{
}

void aiPointsSample::getDataPointer(aiPointsData &data)
{
    int count = (int)m_points->size();
    data.count = count;

    auto& schema = static_cast<aiPoints&>(*m_schema);
    if (schema.getSort()) {
        createSortData();
        if (m_points) {
            m_tmp_positions.resize(count);
            for (int i = 0; i < count; ++i) {
                m_tmp_positions[i] = (*m_points)[m_sort_data[i].second];
            }
            data.points = m_tmp_positions.data();
        }
        if (m_velocities) {
            m_tmp_velocities.resize(count);
            for (int i = 0; i < count; ++i) {
                m_tmp_velocities[i] = (*m_velocities)[m_sort_data[i].second];
            }
            data.velocities = m_tmp_positions.data();
        }
        if (m_ids) {
            m_tmp_ids.resize(count);
            for (int i = 0; i < count; ++i) {
                m_tmp_ids[i] = (*m_ids)[m_sort_data[i].second];
            }
            data.ids = m_tmp_ids.data();
        }
    }
    else {
        if (m_points) {
            data.points = (abcV3*)m_points->get();
        }
        if (m_velocities) {
            data.velocities = (abcV3*)m_velocities->get();
        }
        if (m_ids) {
            data.ids = (uint64_t*)m_ids->get();
        }
    }

    data.center = m_bounds.center();
    data.size = m_bounds.size();
}

void aiPointsSample::copyData(aiPointsData &data)
{
    auto& config = getConfig();

    int count = (int)m_points->size();
    data.count = count;

    auto& schema = static_cast<aiPoints&>(*m_schema);
    bool sort = schema.getSort();
    if (sort) {
        createSortData();
    }

    // copy positions
    if (m_points && data.points) {
        if (sort) {
            for (int i = 0; i < count; ++i) {
                data.points[i] = (*m_points)[m_sort_data[i].second];
            }
        }
        else {
            for (int i = 0; i < count; ++i) {
                data.points[i] = (*m_points)[i];
            }
        }

        if (config.swap_handedness) {
            for (int i = 0; i < count; ++i) {
                data.points[i].x *= -1.0f;
            }
        }
    }

    // copy velocities
    if (m_velocities && data.velocities) {
        int v_count = std::min<int>(count, (int)m_velocities->size());

        if (sort) {
            for (int i = 0; i < v_count; ++i) {
                data.velocities[i] = (*m_velocities)[m_sort_data[i].second];
            }
        }
        else {
            for (int i = 0; i < v_count; ++i) {
                data.velocities[i] = (*m_velocities)[i];
            }
        }

        if (config.swap_handedness) {
            for (int i = 0; i < v_count; ++i) {
                data.velocities[i].x *= -1.0f;
            }
        }
    }

    // copy ids
    if (m_ids && data.ids) {
        int v_count = std::min<int>(count, (int)m_ids->size());

        if (sort) {
            for (int i = 0; i < v_count; ++i) {
                data.ids[i] = (*m_ids)[m_sort_data[i].second];
            }
        }
        else {
            for (int i = 0; i < v_count; ++i) {
                data.ids[i] = (*m_ids)[i];
            }
        }
    }

    data.center = m_bounds.center();
    data.size = m_bounds.size();
}

void aiPointsSample::createSortData()
{
    auto& schema = static_cast<aiPoints&>(*m_schema);

    m_sort_data.resize(m_points->size());
    int count = (int)m_points->size();
    for (int i = 0; i < count; ++i) {
        m_sort_data[i].first = ((*m_points)[i] - schema.getSortPosition()).length();
        m_sort_data[i].second = i;
    }

#ifdef _WIN32
    concurrency::parallel_sort
#else
    std::sort
#endif
        (m_sort_data.begin(), m_sort_data.end(),
            [](const std::pair<float, int>& a, const std::pair<float, int>& b) { return a.first > b.first; });
}

// ---

aiPoints::aiPoints(aiObject *obj)
    : super(obj)
{

}

aiPoints::Sample* aiPoints::newSample()
{
    return new Sample(this);
}

void aiPoints::readSample(Sample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);

    // read positions
    m_schema.getPositionsProperty().get(sample.m_points, ss);

    // read velocities
    sample.m_velocities.reset();
    auto velocitiesProp = m_schema.getVelocitiesProperty();
    if (velocitiesProp.valid())
    {
        DebugLog("  Read velocities");
        velocitiesProp.get(sample.m_velocities, ss);
    }

    // read IDs
    sample.m_ids.reset();
    auto idProp = m_schema.getIdsProperty();
    if (idProp.valid())
    {
        DebugLog("  Read IDs");
        idProp.get(sample.m_ids, ss);
    }

    // read bounds
    auto boundsProp = m_schema.getSelfBoundsProperty();
    if (boundsProp.valid())
    {
        DebugLog("  Read bounds");
        boundsProp.get(sample.m_bounds, ss);
    }
}


const aiPointsSummary& aiPoints::getSummary() const
{
    if (m_summary.peak_count == 0) {
        auto positions = m_schema.getPositionsProperty();
        auto velocities = m_schema.getVelocitiesProperty();
        auto ids = m_schema.getIdsProperty();

        m_summary.has_velocity = velocities.valid();
        m_summary.position_is_constant = positions.isConstant();
        m_summary.id_is_constant = ids.isConstant();

        m_summary.peak_count = (int)abcArrayPropertyGetPeakSize(positions);

        auto id_minmax = abcArrayPropertyGetMinMaxValue(ids);
        m_summary.min_id = id_minmax.first;
        m_summary.max_id = id_minmax.second;

        auto bounds = abcGetMaxBounds(m_schema.getSelfBoundsProperty());
        m_summary.bounds_center = bounds.center();
        m_summary.bounds_extents = bounds.size();

    }

    return m_summary;
}

void aiPoints::setSort(bool v) { m_sort = v; }
bool aiPoints::getSort() const { return m_sort; }
void aiPoints::setSortPosition(const abcV3& v) { m_sort_position = v; }
const abcV3& aiPoints::getSortPosition() const { return m_sort_position; }
