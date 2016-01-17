#include "pch.h"
#include "Common.h"



tContext::tContext()
    : m_ictx(nullptr), m_ectx(nullptr)
{
}

tContext::~tContext()
{
}

void tContext::setArchives(aiContext *ictx, aeContext *ectx)
{
    m_ictx = ictx;
    m_ectx = ectx;
}

aiObject* tContext::getIObject(aeObject *eobj)
{
    auto i = m_eimap.find(eobj);
    return i == m_eimap.end() ? nullptr : i->second;
}

aeObject* tContext::getEObject(aiObject *iobj)
{
    auto i = m_iemap.find(iobj);
    return i == m_iemap.end() ? nullptr : i->second;
}


void tContext::constructIETree(const IEEnumerator &cb)
{
    if (!m_ictx || !m_ectx) { return; }
    constructIETreeImpl(aiGetTopObject(m_ictx), cb);
}

void tContext::constructIETreeImpl(aiObject *iobj, const IEEnumerator &cb)
{
    aeObject *eobj = nullptr;
    if (iobj == aiGetTopObject(m_ictx)) {
        eobj = aeGetTopObject(m_ectx);
    }
    else {
        aeObject *eparent = m_estack.empty() ? nullptr : m_estack.back();
        const char *name = aiGetNameS(iobj);
        if (auto *xf = aiGetXForm(iobj)) {
            eobj = aeNewXForm(eparent, name);
        }
        else if (auto *cam = aiGetCamera(iobj)) {
            eobj = aeNewCamera(eparent, name);
        }
        else if (auto *points = aiGetPoints(iobj)) {
            eobj = aeNewPoints(eparent, name);
        }
        else if (auto *mesh = aiGetPolyMesh(iobj)) {
            eobj = aeNewPolyMesh(eparent, name);
        }

        cb(iobj, eobj);
    }

    m_iemap[iobj] = eobj;
    m_eimap[eobj] = iobj;
    m_istack.push_back(iobj);
    m_estack.push_back(eobj);

    int n = aiGetNumChildren(iobj);
    for (int i = 0; i < n; ++i) {
        auto *child = aiGetChild(iobj, i);
        constructIETreeImpl(child, cb);
    }

    m_estack.pop_back();
    m_istack.pop_back();
}
