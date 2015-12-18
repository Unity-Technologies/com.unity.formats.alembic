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
