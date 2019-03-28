#include "pch.h"
#include "aiAsync.h"


aiAsyncManager::~aiAsyncManager()
{
    if (m_future.valid())
        m_future.wait();
}

aiAsyncManager& aiAsyncManager::instance()
{
    static aiAsyncManager s_instance;
    return s_instance;
}

void aiAsyncManager::queue(aiAsync *task)
{
    queue(&task, 1);
}

void aiAsyncManager::queue(aiAsync **tasks, size_t num)
{
    for (size_t i = 0; i < num; ++i)
        tasks[i]->prepare();

    {
        std::lock_guard<std::mutex> lock(m_mutex);
        m_tasks.insert(m_tasks.end(), tasks, tasks + num);

        // launch worker thread (task) if needed
        if (!m_processing) {
            m_processing = true;
            m_future = std::async(std::launch::async, [this]() { process(); });
        }
    }
}

void aiAsyncManager::process()
{
    while (true) {
        aiAsync *task = nullptr;
        {
            std::lock_guard<std::mutex> lock(m_mutex);
            if (!m_tasks.empty()) {
                task = m_tasks.front();
                m_tasks.pop_front();
            }
            else {
                m_processing = false;
            }
        }

        if (task)
            task->run();
        else
            break;
    }
}


aiAsyncLoad::~aiAsyncLoad()
{
    if (m_async_cook.valid())
        m_async_cook.wait();
}

void aiAsyncLoad::reset()
{
    m_read = {};
    m_cook = {};
}

bool aiAsyncLoad::ready() const
{
    return m_read || m_cook;
}

void aiAsyncLoad::prepare()
{
    m_completed = false;
}

void aiAsyncLoad::run()
{
    if (m_read)
        m_read();

    if (m_cook) {
        m_async_cook = std::async(std::launch::async, [this]() {
            m_cook();
            release();
        });
    }
    else {
        release();
    }
}

void aiAsyncLoad::release()
{
    {
        std::lock_guard<std::mutex> lock(m_mutex);
        m_completed = true;
    }
    m_notify_completed.notify_all();
}

void aiAsyncLoad::wait()
{
    if (!m_completed) {
        std::unique_lock<std::mutex> lock(m_mutex);
        m_notify_completed.wait(lock, [this] { return m_completed; });
    }
}

