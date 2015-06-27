#include "pch.h"
#include "AlembicImporter.h"
#include "aiObject.h"
#include "Schema/aiSchema.h"
#include "aiContext.h"
#include <limits>


aiContext* aiContext::create()
{
    return new aiContext();
}

void aiContext::destroy(aiContext* ctx)
{
    delete ctx;
}

aiContext::aiContext()
{
    m_time_range[0] = 0;
    m_time_range[1] = 0;
}

aiContext::~aiContext()
{
    waitTasks();
    for (auto n : m_nodes) { delete n; }
    m_nodes.clear();
}

void aiContext::gatherNodesRecursive(aiObject *n)
{
    m_nodes.push_back(n);
    abcObject &abc = n->getAbcObject();
    int num_children = abc.getNumChildren();
    for (int i = 0; i < num_children; ++i) {
        abcObject abcChild = abc.getChild(i);
        aiObject *child = new aiObject(this, abcChild);
        n->addChild(child);
        gatherNodesRecursive(child);
    }
}

bool aiContext::load(const char *path)
{
    if (path == nullptr) return false;

    try {
        aiDebugLog("trying to open AbcCoreOgawa::ReadArchive...\n");
        m_archive = abcArchivePtr(new Abc::IArchive(AbcCoreOgawa::ReadArchive(), path));
    }
    catch (Alembic::Util::Exception e)
    {
        aiDebugLog("exception: %s\n", e.what());

        try {
            aiDebugLog("trying to open AbcCoreHDF5::ReadArchive...\n");
            m_archive = abcArchivePtr(new Abc::IArchive(AbcCoreHDF5::ReadArchive(), path));
        }
        catch (Alembic::Util::Exception e)
        {
            aiDebugLog("exception: %s\n", e.what());
        }
    }

    if (m_archive && m_archive->valid()) {
        abcObject abcTop = m_archive->getTop();
        aiObject *top = new aiObject(this, abcTop);
        gatherNodesRecursive(top);

        m_time_range[0] = std::numeric_limits<double>::max();
        m_time_range[1] = -std::numeric_limits<double>::max();

        for (unsigned int i=0; i<m_archive->getNumTimeSamplings(); ++i)
        {
            AbcCoreAbstract::TimeSamplingPtr ts = m_archive->getTimeSampling(i);

            AbcCoreAbstract::TimeSamplingType tst = ts->getTimeSamplingType();

            // Note: alembic guaranties we have at least one stored time

            if (tst.isCyclic() || tst.isUniform())
            {
                size_t num_cycles = (m_archive->getMaxNumSamplesForTimeSamplingIndex(i) / tst.getNumSamplesPerCycle());

                m_time_range[0] = ts->getStoredTimes()[0];
                m_time_range[1] = m_time_range[0] + (num_cycles - 1) * tst.getTimePerCycle();
            }
            else if (tst.isAcyclic())
            {
                m_time_range[0] = ts->getSampleTime(0);
                m_time_range[1] = ts->getSampleTime(ts->getNumStoredTimes() - 1);
            }
        }

        if (m_time_range[0] > m_time_range[1])
        {
            m_time_range[0] = 0.0;
            m_time_range[1] = 0.0;
        }

        aiDebugLog("succeeded\n");
        return true;
    }
    else {
        m_archive.reset();

        m_time_range[0] = 0.0;
        m_time_range[1] = 0.0;

        return false;
    }
}

float aiContext::getStartTime() const
{
    return float(m_time_range[0]);
}

float aiContext::getEndTime() const
{
    return float(m_time_range[1]);
}

aiObject* aiContext::getTopObject()
{
    return m_nodes.empty() ? nullptr : m_nodes.front();
}

const aiImportConfig& aiContext::getImportConfig() const
{
    return m_iconfig;
}
void aiContext::setImportConfig(const aiImportConfig &conf)
{
    m_iconfig = conf;
}

void aiContext::updateSamples(float time)
{
    for (const auto &e : m_nodes) {
        e->updateSample(time);
    }
}
void aiContext::updateSamplesBegin(float time)
{
    enqueueTask([this, time](){ updateSamples(time); });
}
void aiContext::updateSamplesEnd()
{
    waitTasks();
}

void aiContext::erasePastSamples(float time, float range_keep)
{
    for (const auto &e : m_nodes) {
        e->erasePastSamples(time, range_keep);
    }
}

void aiContext::enqueueTask(const task_t &task)
{
    m_tasks.run(task);
}

void aiContext::waitTasks()
{
    m_tasks.wait();
}

void aiContext::debugDump() const
{
    for (const auto &e : m_nodes) {
        e->debugDump();
    }
}
