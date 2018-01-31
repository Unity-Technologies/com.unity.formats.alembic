#include "pch.h"
#include "aiAsync.h"


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

