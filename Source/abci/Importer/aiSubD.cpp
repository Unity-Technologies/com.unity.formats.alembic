#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiMeshSchema.h"
#include "aiSubD.h"


aiSubDSample::aiSubDSample(aiSubD *schema, TopologyPtr topo)
    : aiMeshSample(schema, topo)
{
}

aiSubDSample::~aiSubDSample()
{
}

aiSubD::aiSubD(aiObject *parent, const abcObject &abc)
    : aiMeshSchema(parent, abc)
{
}

aiSubD::~aiSubD()
{
}

aiSubDSample* aiSubD::newSample()
{
    if (!m_varying_topology)
    {
        if (!m_shared_topology)
            m_shared_topology.reset(new aiMeshTopology());
        return new Sample(this, m_shared_topology);
    }
    else
    {
        return new Sample(this, TopologyPtr(new aiMeshTopology()));
    }
}

