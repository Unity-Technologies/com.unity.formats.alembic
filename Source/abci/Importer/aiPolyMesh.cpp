#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiPolyMesh.h"


aiPolyMeshSample::aiPolyMeshSample(aiPolyMesh *schema, TopologyPtr topo)
    : aiMeshSample(schema, topo)
{
}

aiPolyMeshSample::~aiPolyMeshSample()
{
}

aiPolyMesh::aiPolyMesh(aiObject* parent, const abcObject& abc)
    :aiMeshSchema(parent, abc)
{
}

aiPolyMesh::~aiPolyMesh()
{
}

aiPolyMeshSample* aiPolyMesh::newSample()
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

