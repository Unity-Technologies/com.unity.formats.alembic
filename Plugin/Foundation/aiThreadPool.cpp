#include "pch.h"
#include "aiLogger.h"
#include "aiThreadPool.h"

#ifndef aiWithTBB

class aiWorkerThread
{
public:
    aiWorkerThread(aiThreadPool *pool);
    void operator()();

private:
    aiThreadPool *m_pool;
};


aiWorkerThread::aiWorkerThread(aiThreadPool *pool)
    : m_pool(pool)
{
}

void aiWorkerThread::operator()()
{
    std::function<void()> task;

    bool poolStopped = false;

    while (!poolStopped)
    {
        {
            std::unique_lock<std::mutex> lock(m_pool->m_queueMutex);

            while (!m_pool->m_stop && m_pool->m_tasks.empty())
            {
                m_pool->m_queueCondition.wait(lock);
            }
            
            if (m_pool->m_stop)
            {
                poolStopped = true;
                continue;
            }

            task = m_pool->m_tasks.front();
            m_pool->m_tasks.pop_front();
        }

        task();
    }
}

// ---

static aiThreadPool *g_instance = nullptr;

aiThreadPool::aiThreadPool(size_t threads)
    : m_stop(false)
{
    DebugLog("aiThreadPool: Starting %lu thread(s)", threads);

    for (size_t i = 0; i < threads; ++i)
    {
        m_workers.push_back(std::thread(aiWorkerThread(this)));
    }
}

aiThreadPool::~aiThreadPool()
{
    m_stop = true;

    m_queueCondition.notify_all();

    for (auto& worker : m_workers)
    {
        worker.join();
    }
}

void aiThreadPool::releaseInstance()
{
    if (g_instance != nullptr)
    {
        delete g_instance;
        g_instance = nullptr;
    }
}

aiThreadPool& aiThreadPool::getInstance()
{
    if (g_instance == nullptr)
    {
        g_instance = new aiThreadPool(std::thread::hardware_concurrency());
    }

    return *g_instance;
}

void aiThreadPool::enqueue(const std::function<void()> &f)
{
    {
        std::unique_lock<std::mutex> lock(m_queueMutex);
        
        m_tasks.push_back(std::function<void()>(f));
    }

    m_queueCondition.notify_one();
}

// ---

aiTaskGroup::aiTaskGroup()
    : m_activeTasks(0)
{
}

aiTaskGroup::~aiTaskGroup()
{
}

void aiTaskGroup::taskDone()
{
    {
        std::unique_lock<std::mutex> lock(m_taskMutex);

        --m_activeTasks;
    }

    m_taskCondition.notify_one();
}

void aiTaskGroup::wait()
{
    aiThreadPool &pool = aiThreadPool::getInstance();

    {
        std::unique_lock<std::mutex> lock(m_taskMutex);
        
        while (m_activeTasks > 0)
        {
            if (pool.m_stop)
            {
                break;
            }

            m_taskCondition.wait(lock);
        }
    }
}

#endif // aiWithTBB
