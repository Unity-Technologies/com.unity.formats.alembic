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


void aiPointsSample::fillData(aiPointsSampleData &data)
{
    int count = m_positions->size();
    data.count = count;

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

    if (m_velocities && data.velocities) {
        for (int i = 0; i < count; ++i) {
            data.velocities[i] = (*m_velocities)[i];
        }
    }

    if (m_ids && data.ids) {
        for (int i = 0; i < count; ++i) {
            data.ids[i] = (*m_ids)[i];
        }
    }

    data.boundsCenter = m_bounds.center();
    data.boundsExtents = m_bounds.size();
}

// ---

aiPoints::aiPoints(aiObject *obj)
    : super(obj)
    , m_peakVertexCount(0)
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

aiPoints::Sample* aiPoints::readSample(float time, bool &topologyChanged)
{
    DebugLog("aiPoints::readSample(t=%f)", time);

    Sample *ret = newSample();
    auto ss = MakeSampleSelector(time);

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

    auto boundsProp = m_schema.getSelfBoundsProperty();
    if (boundsProp.valid())
    {
        DebugLog("  Read bounds");
        boundsProp.get(ret->m_bounds, ss);
    }

    return ret;
}

int aiPoints::getPeakVertexCount() const
{
    if (m_peakVertexCount == 0)
    {
        DebugLog("aiPoints::getPeakVertexCount()");

        Util::Dimensions dim;

        auto positionsProp = m_schema.getPositionsProperty();

        int numSamples = positionsProp.getNumSamples();

        if (numSamples == 0)
        {
            return 0;
        }
        else if (positionsProp.isConstant())
        {
            positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(0)));

            m_peakVertexCount = dim.numPoints();
        }
        else
        {
            m_peakVertexCount = 0;

            for (int i = 0; i < numSamples; ++i)
            {
                positionsProp.getDimensions(dim, Abc::ISampleSelector(int64_t(i)));

                size_t numVertices = dim.numPoints();

                if (numVertices > size_t(m_peakVertexCount))
                {
                    m_peakVertexCount = int(numVertices);
                }
            }
        }
    }

    return m_peakVertexCount;
}
