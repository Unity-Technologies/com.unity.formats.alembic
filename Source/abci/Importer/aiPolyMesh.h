#pragma once
#include "aiMeshSchema.h"

class aiPolyMeshSample : public aiMeshSample<aiPolyMesh>
{
public:
    aiPolyMeshSample(aiPolyMesh* schema, TopologyPtr topo);
    ~aiPolyMeshSample();
};

struct aiPolyMeshTraits
{
    using SampleT = aiPolyMeshSample;
    using AbcSchemaT = AbcGeom::IPolyMeshSchema;
};

class aiPolyMesh : public aiMeshSchema<aiPolyMeshTraits, aiPolyMeshSample>
{
public:
    aiPolyMesh(aiObject *parent, const abcObject &abc);
    ~aiPolyMesh() override;

    aiPolyMeshSample* newSample() override;
};
