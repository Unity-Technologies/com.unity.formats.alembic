#include "pch.h"
#include "AlembicImporter.h"
#include "aiSchema.h"
#include "aiCamera.h"
#include "aiPolyMesh.h"
#include "aiXForm.h"
#include "aiObject.h"
#include "aiContext.h"

aiSchema::aiSchema()
    : m_obj(nullptr)
{
}

aiSchema::aiSchema(aiObject *obj)
    : m_obj(obj)
{
}

aiSchema::~aiSchema()
{
}
