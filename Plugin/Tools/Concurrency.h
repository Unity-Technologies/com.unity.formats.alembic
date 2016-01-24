#ifndef Concurrency_h
#define Concurrency_h

#include <vector>
#include <mutex>
#ifdef WithTBB
    #include <tbb/tbb.h>
    #include <tbb/combinable.h>
#else
    #include <ppl.h>
#endif
#ifdef _WIN32
    #include <windows.h>
#else
    #include <pthread.h>
#endif

namespace ist {

class tls_base
{
public:
#if _WIN32
    typedef DWORD tls_key_t;
#if WindowsStoreApp
    tls_base() { m_key = ::FlsAlloc(nullptr); }
    ~tls_base() { ::FlsFree(m_key); }
    void set_value(void *value) { ::FlsSetValue(m_key, value); }
    void* get_value() { return (void *)::FlsGetValue(m_key); }
#else
    tls_base() { m_key = ::TlsAlloc(); }
    ~tls_base() { ::TlsFree(m_key); }
    void set_value(void *value) { ::TlsSetValue(m_key, value); }
    void* get_value() const { return (void *)::TlsGetValue(m_key); }
#endif
#else
    typedef pthread_key_t tls_key_t;
    tls_base() { pthread_key_create(&m_key, nullptr); }
    ~tls_base() { pthread_key_delete(m_key); }
    void set_value(void *value) { pthread_setspecific(m_key, value); }
    void* get_value() const { return pthread_getspecific(m_key); }
#endif
private:
    tls_key_t m_key;
};

template<class T>
class tls : private tls_base
{
public:
    tls() {}

    ~tls()
    {
        std::unique_lock<std::mutex> lock;
        for (auto p : m_locals) { delete p; }
        m_locals.clear();
    }

    T& local()
    {
        void *value = get_value();
        if (value == nullptr) {
            T *v = new T();
            set_value(v);
            value = v;

            {
                std::unique_lock<std::mutex> lock;
                m_locals.push_back(v);
            }
        }
        return *(T*)value;
    }

    template<class Body>
    void each(const Body& body)
    {
        for (auto p : m_locals) { body(*p); }
    }

protected:
    std::mutex m_mutex;
    std::vector<T*> m_locals;
};


#ifdef WithTBB

template<class IndexType, class Body>
inline void parallel_for(IndexType first, IndexType last, const Body& body)
{
    tbb::parallel_for(first, last, body);
}

template<class IndexType, class IntType, class Body>
inline void parallel_for(IndexType first, IndexType last, IntType granularity, const Body& body)
{
    typedef tbb::blocked_range<IndexType> range_t;
    tbb::parallel_for(range_t(first, last, granularity),
        [&](const range_t &r) {
            for (int i = r.begin(); i < r.end(); ++i) { body(i); }
        });
}

template<class IndexType, class IntType, class Body>
inline void parallel_for_blocked(IndexType first, IndexType last, IntType granularity, const Body& body)
{
    typedef tbb::blocked_range<IndexType> range_t;
    tbb::parallel_for(range_t(first, last, granularity), [&](const range_t &r) { body(r.begin(), r.end()); });
}


using tbb::parallel_sort;
using tbb::parallel_invoke;
using tbb::task_group;
using tbb::combinable;


#else

template<class IndexType, class Body>
inline void parallel_for(IndexType first, IndexType last, const Body& body)
{
    concurrency::parallel_for(first, last, body);
}

template<class IndexType, class IntType, class Body>
inline void parallel_for(IndexType first, IndexType last, IntType granularity, const Body& body)
{
    concurrency::parallel_for(first, last, granularity, [&](int i) {
        IndexType beg = i;
        IndexType end = std::min<IndexType>(beg + granularity, last);
        for (int i = beg; i < end; ++i) { body(i); }
    });
}

template<class IndexType, class IntType, class Body>
inline void parallel_for_blocked(IndexType first, IndexType last, IntType granularity, const Body& body)
{
    concurrency::parallel_for(first, last, granularity, [&](int i) {
        IndexType beg = i;
        IndexType end = std::min<IndexType>(beg + granularity, last);
        body(beg, end);
    });
}


using concurrency::parallel_sort;
using concurrency::parallel_invoke;
using concurrency::task_group;


template<class T>
class combinable : public tls<T>
{
public:
    using tls<T>::local;

    template<class Body>
    void combine_each(const Body& body)
    {
        for (auto p : m_locals) { body(*p); }
    }
};

#endif // WithTBB

} // namespace ist

#endif // Concurrency_h
