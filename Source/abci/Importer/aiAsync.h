#pragma once

class aiAsync
{
public:
    virtual ~aiAsync() {}
    virtual void prepare() = 0;
    virtual void run() = 0;
    virtual void wait() = 0;
};

class aiAsyncManager
{
public:
    static aiAsyncManager& instance();
    void queue(aiAsync *task);
    void queue(aiAsync **tasks, size_t num);

private:
    ~aiAsyncManager();
    void process();

    std::deque<aiAsync*> m_tasks;
    std::mutex m_mutex;
    std::future<void> m_future;
    bool m_processing = false;
};


class aiAsyncLoad : public aiAsync
{
public:
    std::function<void()> m_read;
    std::function<void()> m_cook;

    ~aiAsyncLoad();
    void reset();
    bool ready() const;
    void prepare() override;
    void run() override;
    void wait() override;

private:
    void release();

    std::future<void> m_async_cook;
    // these are needed because m_async_cook possibly has not started yet when wait() is called
    std::mutex m_mutex;
    std::condition_variable m_notify_completed;
    bool m_completed = true;
};


