#include "pch.h"
#include "AlembicExporter.h"
#include "aeObject.h"
#include "aePolyMesh.h"

aePolyMesh::aePolyMesh(aeObject *obj)
    : super(obj)
    , m_abcobj(obj->getAbcObject(), "PolyMesh")
    , m_schema(m_abcobj.getSchema())
{

}

void aePolyMesh::writeSample(const aePolyMeshSampleData &data)
{

}
