#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"
#include "aiPolyMesh.h"
#include "aiCamera.h"
#include "aiPoints.h"

aiObject::aiObject()
{
}

aiObject::aiObject(aiContext *ctx, aiObject *parent, const abcObject &abc)
    : m_ctx(ctx)
    , m_abc(abc)
    , m_parent(parent)
{
}

aiObject::~aiObject()
{
    if (!m_children.empty()) {
        // make m_children empty before deleting children because children try to remove element of it in their destructor
        decltype(m_children) tmp;
        tmp.swap(m_children);
    }
    if (m_parent)
        m_parent->removeChild(this);
}

aiObject* aiObject::newChild(const abcObject &abc)
{
    aiObject *ret = nullptr;
    if (abc.valid())
    {
        const auto& metadata = abc.getMetaData();

        if (AbcGeom::IXformSchema::matches(metadata))
            ret = new aiXform(this, abc);
        else if (AbcGeom::IPolyMeshSchema::matches(metadata))
            ret = new aiPolyMesh(this, abc);
        else if (AbcGeom::ICameraSchema::matches(metadata))
            ret = new aiCamera(this, abc);
        else if (AbcGeom::IPointsSchema::matches(metadata))
            ret = new aiPoints(this, abc);
        else
            ret = new aiObject(m_ctx, this, abc);
    }

    if (ret)
        m_children.emplace_back(ret);
    return ret;
}

void aiObject::removeChild(aiObject *c)
{
    if (c == nullptr) { return; }

    auto it = std::find_if(m_children.begin(), m_children.end(), [c](ObjectPtr& p) { return p.get() == c; });
    if (it != m_children.end())
    {
        c->m_parent = nullptr;
        m_children.erase(it);
    }
}

aiContext*  aiObject::getContext() const { return m_ctx; }
const aiConfig& aiObject::getConfig() const { return m_ctx->getConfig(); }
abcObject&  aiObject::getAbcObject() { return m_abc; }
const char* aiObject::getName() const { return m_abc.getName().c_str(); }
const char* aiObject::getFullName() const { return m_abc.getFullName().c_str(); }
uint32_t    aiObject::getNumChildren() const { return (uint32_t)m_children.size(); }
aiObject*   aiObject::getChild(int i) { return m_children[i].get(); }
aiObject*   aiObject::getParent() const { return m_parent; }
void        aiObject::setEnabled(bool v) { m_enabled = v; }

aiSample* aiObject::getSample()
{
    return nullptr;
}

void aiObject::updateSample(const abcSampleSelector& ss)
{
}

void aiObject::waitAsync()
{
}
