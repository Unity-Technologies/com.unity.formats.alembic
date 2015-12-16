#include "pch.h"
#include "AlembicExporter.h"
#include "aeContext.h"
#include "aeObject.h"

aeContext::aeContext(aeConfig &conf)
    : m_config(conf)
{

}

aeContext::~aeContext()
{
    reset();
}

void aeContext::reset()
{
    for (auto n : m_nodes) { delete n; }
    m_nodes.clear();

    m_archive.reset();
}

bool aeContext::openArchive(const char *path)
{
    reset();

    if (m_config.archive_type == aeArchiveType_HDF5) {
        m_archive = Abc::OArchive(Alembic::AbcCoreHDF5::WriteArchive(), path);
    }
    else {
        m_archive = Abc::OArchive(Alembic::AbcCoreOgawa::WriteArchive(), path);
    }

    aeObject *top = new aeObject(this, AbcGeom::OObject(m_archive, AbcGeom::kTop), "");
    m_nodes.push_back(top);
}

aeObject* aeContext::getTopObject()
{
    return m_nodes.empty() ? nullptr : m_nodes.front();
}
