#ifndef aeContext_h
#define aeContext_h

class aeContext
{
public:
    typedef std::vector<aeObject*> NodeCont;
    typedef NodeCont::iterator NodeIter;

    aeContext(const aeConfig &conf);
    ~aeContext();
    void reset();
    bool openArchive(const char *path);

    const aeConfig& getConfig() const;
    uint32_t getTimeSaplingIndex() const;
    aeObject* getTopObject();

    void setTime(float time);

private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::unique_ptr<aeObject> m_node_top;
    std::vector<abcChrono> m_times;
    uint32_t m_time_sampling_index;
};

#endif // aeContext_h
