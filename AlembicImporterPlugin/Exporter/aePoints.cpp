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

abcProperties* aePoints::getAbcProperties()
{
    return &m_schema;
}

void aePoints::writeSample(const aePointsSampleData &data_)
{
    auto data = data_;

    const auto &conf = getConfig();

    // if swapHandedness or scaling is required, copy positions to temporary buffer and convert
    if (conf.swapHandedness || conf.scale != 1.0f) {
        m_buf_positions.resize(data.count);
        memcpy(&m_buf_positions[0], data.positions, sizeof(abcV3) * data.count);
        if (conf.swapHandedness) {
            for (auto &v : m_buf_positions) { v.x *= -1.0f; }
        }
        if (conf.scale != 1.0f) {
            const float scale = conf.scale;
            for (auto &v : m_buf_positions) { v *= scale; }
        }
        data.positions = &m_buf_positions[0];
    }

    if (data.ids == nullptr) {
        int bufsize = m_buf_ids.size();
        if (data.count > bufsize) {
            m_buf_ids.resize(data.count);
            for (int i = bufsize; i < data.count; ++i) {
                m_buf_ids[i] = i;
            }
        }
        data.ids = &m_buf_ids[0];
    }

    AbcGeom::OPointsSchema::Sample sample;
    sample.setPositions(Abc::P3fArraySample(data.positions, data.count));
    sample.setIds(Abc::UInt64ArraySample(data.ids, data.count));

    m_schema.set(sample);
}
