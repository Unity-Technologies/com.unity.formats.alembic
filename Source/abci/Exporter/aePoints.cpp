#include "pch.h"
#include "aeInternal.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePoints.h"


aePoints::aePoints(aeObject *parent, const char *name, uint32_t tsi)
    : super(parent->getContext(), parent, new abcPoints(parent->getAbcObject(), name, tsi), tsi)
    , m_schema(getAbcObject().getSchema())
{
}

abcPoints& aePoints::getAbcObject()
{
    return dynamic_cast<abcPoints&>(*m_abc);
}

abcProperties aePoints::getAbcProperties()
{
    return m_schema.getUserProperties();
}

size_t aePoints::getNumSamples()
{
    return m_schema.getNumSamples();
}

void aePoints::setFromPrevious()
{
    m_schema.setFromPrevious();
}

void aePoints::writeSample(const aePointsData &data)
{
    m_buf_visibility = data.visibility;
    Assign(m_buf_positions, data.positions, data.count);
    Assign(m_buf_velocities, data.velocities, data.count);
    Assign(m_buf_ids, data.ids, data.count);

    m_ctx->addAsync([this]() { doWriteSample(); });
}

void aePoints::doWriteSample()
{
    const auto &conf = getConfig();

    // generate ids if needed
    if (m_buf_ids.size() != m_buf_positions.size())
    {
        m_buf_ids.resize(m_buf_positions.size());
        std::iota(m_buf_ids.begin(), m_buf_ids.end(), 0);
    }

    // handle swap handedness
    if (conf.swap_handedness)
    {
        for (auto &v : m_buf_positions)
        {
            v.x *= -1.0f;
        }
        for (auto &v : m_buf_velocities)
        {
            v.x *= -1.0f;
        }
    }

    // handle scale factor
    float scale = conf.scale_factor;
    if (scale != 1.0f)
    {
        for (auto &v : m_buf_positions)
        {
            v *= scale;
        }
        for (auto &v : m_buf_velocities)
        {
            v *= scale;
        }
    }

    // write!
    writeVisibility(m_buf_visibility);

    AbcGeom::OPointsSchema::Sample sample;
    sample.setPositions(Abc::P3fArraySample(m_buf_positions.data(), m_buf_positions.size()));
    sample.setIds(Abc::UInt64ArraySample(m_buf_ids.data(), m_buf_ids.size()));
    if (!m_buf_velocities.empty())
    {
        sample.setVelocities(Abc::V3fArraySample(m_buf_velocities.data(), m_buf_velocities.size()));
    }

    m_schema.set(sample);
}
