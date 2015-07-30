#ifndef aiThreadPool_h
#define aiThreadPool_h

#ifndef aiWithTBB

#include <vector>
#include <deque>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <atomic>

class aiWorkerThread;
class aiThreadPool;
class aiTaskGroup;


class aiThreadPool
{
friend class aiWorkerThread;
friend class aiTaskGroup;
public:
    static void releaseInstance();
    static aiThreadPool& getInstance();
    void enqueue(const std::function<void()> &f);

private:
    aiThreadPool(size_t);
    ~aiThreadPool();

private:
    std::vector< std::thread > m_workers;
    std::deque< std::function<void()> > m_tasks;
    std::mutex m_queue_mutex;
    std::condition_variable m_condition;
    bool m_stop;
};



class aiTaskGroup
{
public:
    aiTaskGroup();
    ~aiTaskGroup();
    template<class F> void run(const F &f);
    void wait();
    void taskDone();

private:
    int m_active_tasks;
    std::mutex m_task_mutex;
    std::condition_variable m_condition;
};

template<class F>
void aiTaskGroup::run(const F &f)
{
    {
        std::unique_lock<std::mutex> lock(m_task_mutex);
        ++m_active_tasks;
    }
    
    aiThreadPool::getInstance().enqueue([this, f](){
        f();
        this->taskDone();
    });
}

#else // aiWithTBB

#include <tbb/tbb.h>
typedef tbb::task_group aiTaskGroup;

#endif // aiWithTBB

#endif // aiThreadPool_h
