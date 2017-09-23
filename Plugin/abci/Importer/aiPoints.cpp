#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPoints.h"

// ---

aiPointsSample::aiPointsSample(aiPoints *schema)
    : super(schema)
{
}

aiPointsSample::~aiPointsSample()
{
}

void aiPointsSample::updateConfig(const aiConfig &config, bool &topoChanged, bool &dataChanged)
{
    DebugLog("aiPointsSample::updateConfig()");

    topoChanged = false;
    dataChanged = (config.swapHandedness != m_config.swapHandedness);
    m_config = config;
}


void aiPointsSample::getDataPointer(aiPointsData &data)
{
    int count = (int)m_positions->size();
    data.count = count;

    auto& schema = static_cast<aiPoints&>(*m_schema);
    if (schema.getSort()) {
        createSortData();
        if (m_positions) {
            m_tmp_positions.resize(count);
            for (int i = 0; i < count; ++i) {
                m_tmp_positions[i] = (*m_positions)[m_sort_data[i].second];
            }
            data.positions = m_tmp_positions.data();
        }
        if (m_velocities) {
            m_tmp_velocities.resize(count);
            int v_count = std::min<int>(count, (int)m_velocities->size());
            for (int i = 0; i < count; ++i) {
                m_tmp_velocities[i] = (*m_velocities)[m_sort_data[i].second];
            }
            data.velocities = m_tmp_positions.data();
        }
        if (m_ids) {
            m_tmp_ids.resize(count);
            int v_count = std::min<int>(count, (int)m_ids->size());
            for (int i = 0; i < count; ++i) {
                m_tmp_ids[i] = (*m_ids)[m_sort_data[i].second];
            }
            data.ids = m_tmp_ids.data();
        }
    }
    else {
        if (m_positions) {
            data.positions = (abcV3*)m_positions->get();
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
    int count = (int)m_positions->size();
    data.count = count;

    auto& schema = static_cast<aiPoints&>(*m_schema);
    bool sort = schema.getSort();
    if (sort) {
        createSortData();
    }

    // copy positions
    if (m_positions && data.positions) {
        if (sort) {
            for (int i = 0; i < count; ++i) {
                data.positions[i] = (*m_positions)[m_sort_data[i].second];
            }
        }
        else {
            for (int i = 0; i < count; ++i) {
                data.positions[i] = (*m_positions)[i];
            }
        }

        if (m_config.swapHandedness) {
            for (int i = 0; i < count; ++i) {
                data.positions[i].x *= -1.0f;
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

        if (m_config.swapHandedness) {
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

    m_sort_data.resize(m_positions->size());
    int count = (int)m_positions->size();
    for (int i = 0; i < count; ++i) {
        m_sort_data[i].first = ((*m_positions)[i] - schema.getSortPosition()).length();
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
    Sample *sample = getSample();

    if (!sample)
    {
        sample = new Sample(this);
    }

    return sample;
}

aiPoints::Sample* aiPoints::readSample(const uint64_t idx, bool &topologyChanged)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);
    DebugLog("aiPoints::readSample(t=%d)", idx);

    Sample *ret = newSample();

    // read positions
    m_schema.getPositionsProperty().get(ret->m_positions, ss);

    // read velocities
    ret->m_velocities.reset();
    auto velocitiesProp = m_schema.getVelocitiesProperty();
    if (velocitiesProp.valid())
    {
        DebugLog("  Read velocities");
        velocitiesProp.get(ret->m_velocities, ss);
    }

    // read IDs
    ret->m_ids.reset();
    auto idProp = m_schema.getIdsProperty();
    if (idProp.valid())
    {
        DebugLog("  Read IDs");
        idProp.get(ret->m_ids, ss);
    }

    // read bounds
    auto boundsProp = m_schema.getSelfBoundsProperty();
    if (boundsProp.valid())
    {
        DebugLog("  Read bounds");
        boundsProp.get(ret->m_bounds, ss);
    }

    return ret;
}


const aiPointsSummary& aiPoints::getSummary() const
{
    if (m_summary.peakCount == 0) {
        auto positions = m_schema.getPositionsProperty();
        auto velocities = m_schema.getVelocitiesProperty();
        auto ids = m_schema.getIdsProperty();

        m_summary.hasVelocity = velocities.valid();
        m_summary.positionIsConstant = positions.isConstant();
        m_summary.idIsConstant = ids.isConstant();

        m_summary.peakCount = (int)abcArrayPropertyGetPeakSize(positions);

        auto id_minmax = abcArrayPropertyGetMinMaxValue(ids);
        m_summary.minID = id_minmax.first;
        m_summary.maxID = id_minmax.second;

        auto bounds = abcGetMaxBounds(m_schema.getSelfBoundsProperty());
        m_summary.boundsCenter = bounds.center();
        m_summary.boundsExtents = bounds.size();

    }

    return m_summary;
}

void aiPoints::setSort(bool v) { m_sort = v; }
bool aiPoints::getSort() const { return m_sort; }
void aiPoints::setSortPosition(const abcV3& v) { m_sort_position = v; }
const abcV3& aiPoints::getSortPosition() const { return m_sort_position; }
