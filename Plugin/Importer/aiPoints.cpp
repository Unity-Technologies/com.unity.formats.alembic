#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPoints.h"
#include "aiXForm.h"

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
    if (m_positions) {
        data.count = (int)m_positions->size();
        data.positions = (abcV3*)m_positions->get();
    }

    if (m_velocities) {
        data.velocities = (abcV3*)m_velocities->get();
    }

    if (m_ids) {
        data.ids = (uint64_t*)m_ids->get();
    }

    data.center = m_bounds.center();
    data.size = m_bounds.size();
}

void aiPointsSample::copyData(aiPointsData &data)
{
    int count = (int)m_positions->size();
    data.count = count;

    // copy positions
    if (m_positions && data.positions) {
        for (int i = 0; i < count; ++i) {
            data.positions[i] = (*m_positions)[i];
        }
        if (m_config.swapHandedness) {
            for (int i = 0; i < count; ++i) {
                data.positions[i].x *= -1.0f;
            }
        }
    }

    // copy velocities
    if (m_velocities && data.velocities) {
        for (int i = 0; i < count; ++i) {
            data.velocities[i] = (*m_velocities)[i];
        }
    }

    // copy ids
    if (m_ids && data.ids) {
        for (int i = 0; i < count; ++i) {
            data.ids[i] = (*m_ids)[i];
        }
    }

    data.center = m_bounds.center();
    data.size = m_bounds.size();
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

aiPoints::Sample* aiPoints::readSample(const abcSampleSelector& ss, bool &topologyChanged)
{
    DebugLog("aiPoints::readSample(t=%f)", (float)ss.getRequestedTime());

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