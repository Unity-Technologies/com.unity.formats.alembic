#include "pch.h"
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
    aiThreadPool &pool = *m_pool;
    std::function<void()> task;
    bool pool_stopped = false;
    while (!pool_stopped)
    {
        {
            std::unique_lock<std::mutex> lock(pool.m_queue_mutex);
            while (!pool.m_stop && pool.m_tasks.empty()) {
                pool.m_condition.wait(lock);
            }
            if (pool.m_stop) {
                pool_stopped = true;
                continue;
            }

            task = pool.m_tasks.front();
            pool.m_tasks.pop_front();
        }
        task();
    }
}

aiThreadPool::aiThreadPool(size_t threads)
    : m_stop(false)
{
    for (size_t i = 0; i < threads; ++i) {
        m_workers.push_back(std::thread(aiWorkerThread(this)));
    }
}

aiThreadPool::~aiThreadPool()
{
    m_stop = true;
    m_condition.notify_all();

    for (auto& worker : m_workers) {
        worker.join();
    }
}

static aiThreadPool *g_instance;

void aiThreadPool::releaseInstance()
{
    if (g_instance != nullptr) {
        delete g_instance;
        g_instance = nullptr;
    }
}

aiThreadPool& aiThreadPool::getInstance()
{
    if (g_instance == nullptr) {
        g_instance = new aiThreadPool(std::thread::hardware_concurrency());
    }
    return *g_instance;
}

void aiThreadPool::enqueue(const std::function<void()> &f)
{
    {
        std::unique_lock<std::mutex> lock(m_queue_mutex);
        m_tasks.push_back(std::function<void()>(f));
    }
    m_condition.notify_one();
}



aiTaskGroup::aiTaskGroup()
    : m_active_tasks(0)
{
}

aiTaskGroup::~aiTaskGroup()
{
}

void aiTaskGroup::taskDone()
{
    {
        std::unique_lock<std::mutex> lock(m_task_mutex);
        --m_active_tasks;
    }

    m_condition.notify_one();
}

void aiTaskGroup::wait()
{
    aiThreadPool &pool = aiThreadPool::getInstance();
    
    {
        std::unique_lock<std::mutex> lock(m_task_mutex);
        while (m_active_tasks > 0) {
            if (pool.m_stop) {
                break;
            }
            m_condition.wait(lock);
        }
    }
}

#endif // aiWithTBB
