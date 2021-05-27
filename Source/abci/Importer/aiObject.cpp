#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"
#include "aiPolyMesh.h"
#include "aiSubD.h"
#include "aiCamera.h"
#include "aiPoints.h"
#include "aiCurves.h"

static std::string SanitizeNodeName(const std::string& src)
{
    try
    {
        using to_utf16_t = std::wstring_convert<std::codecvt_utf8_utf16<wchar_t> >;
        using to_utf8_t = std::wstring_convert<std::codecvt_utf8<wchar_t>, wchar_t>;

        std::wstring wret = to_utf16_t().from_bytes(src);
        return to_utf8_t().to_bytes(wret);
    }
    catch (const std::exception&)
    {
        std::string ret;
        char buf[32];

        size_t len = src.size();
        for (size_t i = 0; i < len;)
        {
            char c = src[i];
            if (std::isprint(c, std::locale::classic()))
            {
                ret += c;
                ++i;
            }
            else
            {
                for (int j = 0; j < 2; ++j)
                {
                    sprintf(buf, "%02x", (uint32_t)(uint8_t)src[i]);
                    ret += buf;
                    ++i;
                    if (i == len - 1)
                        break;
                }
            }
        }
        return ret;
    }
}

aiObject::aiObject()
{
}

aiObject::aiObject(aiContext *ctx, aiObject *parent, const abcObject &abc)
    : m_ctx(ctx)
    , m_abc(abc)
    , m_parent(parent)
{
#if defined(__linux__)
    m_name = m_abc.getName();
    m_fullname = m_abc.getFullName();
#else
    m_name = SanitizeNodeName(m_abc.getName());
    m_fullname = SanitizeNodeName(m_abc.getFullName());
#endif
}

aiObject::~aiObject()
{
    if (!m_children.empty())
    {
        // make m_children empty before deleting children because children try to remove element of it in their destructor
        decltype(m_children)tmp;
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
        else if (AbcGeom::ISubDSchema::matches(metadata))
            ret = new aiSubD(this, abc);
        else if (AbcGeom::ICameraSchema::matches(metadata))
            ret = new aiCamera(this, abc);
        else if (AbcGeom::IPointsSchema::matches(metadata))
            ret = new aiPoints(this, abc);
        else if (AbcGeom::ICurvesSchema::matches(metadata))
            ret = new aiCurves(this, abc);
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

aiContext*  aiObject::getContext() const    { return m_ctx; }
const aiConfig& aiObject::getConfig() const { return m_ctx->getConfig(); }
abcObject&  aiObject::getAbcObject()        { return m_abc; }
const char* aiObject::getName() const       { return m_name.c_str(); }
const char* aiObject::getFullName() const   { return m_fullname.c_str(); }
uint32_t    aiObject::getNumChildren() const{ return (uint32_t)m_children.size(); }
aiObject*   aiObject::getChild(int i)       { return m_children[i].get(); }
aiObject*   aiObject::getParent() const     { return m_parent; }
void        aiObject::setEnabled(bool v)    { m_enabled = v; }

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
