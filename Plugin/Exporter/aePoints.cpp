#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"
#include "aePoints.h"


aePoints::aePoints(aeObject *parent, const char *name)
    : super(parent->getContext(), parent, new abcPoints(parent->getAbcObject(), name, parent->getContext()->getTimeSaplingIndex()))
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

size_t aePoints::getNumSamples() const
{
    return m_schema.getNumSamples();
}

void aePoints::setFromPrevious()
{
    m_schema.setFromPrevious();
}

void aePoints::writeSample(const aePointsSampleData &data_)
{
    auto data = data_;

    const auto &conf = getConfig();

    // handle swapHandedness and scaling for positions, velocities
    if (conf.swapHandedness || conf.scale != 1.0f) {
        float scale = conf.scale;
        {
            m_buf_positions.resize(data.count);
            memcpy(&m_buf_positions[0], data.positions, sizeof(abcV3) * data.count);
            if (conf.swapHandedness) {
                for (auto &v : m_buf_positions) { v.x *= -1.0f; }
            }
            if (scale != 1.0f) {
                for (auto &v : m_buf_positions) { v *= scale; }
            }
            data.positions = &m_buf_positions[0];
        }

        if (data.velocities != nullptr) {
            m_buf_velocities.resize(data.count);
            memcpy(&m_buf_velocities[0], data.velocities, sizeof(abcV3) * data.count);
            if (conf.swapHandedness) {
                for (auto &v : m_buf_velocities) { v.x *= -1.0f; }
            }
            if (scale != 1.0f) {
                for (auto &v : m_buf_velocities) { v *= scale; }
            }
            data.velocities = &m_buf_velocities[0];
        }
    }

    // update id buffer if needed
    if (data.ids == nullptr) {
        int bufsize = (int)m_buf_ids.size();
        if (data.count > bufsize) {
            m_buf_ids.resize(data.count);
            for (int i = bufsize; i < data.count; ++i) {
                m_buf_ids[i] = i;
            }
        }
        data.ids = &m_buf_ids[0];
    }

    // write!
    AbcGeom::OPointsSchema::Sample sample;
    sample.setPositions(Abc::P3fArraySample(data.positions, data.count));
    sample.setIds(Abc::UInt64ArraySample(data.ids, data.count));
    if (data.velocities != nullptr) {
        sample.setVelocities(Abc::V3fArraySample(data.velocities, data.count));
    }

    m_schema.set(sample);
}
