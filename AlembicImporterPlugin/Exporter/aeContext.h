#ifndef aeContext_h
#define aeContext_h

enum aeArchiveType
{
    aeArchiveType_HDF5,
    aeArchiveType_Ogawa,
};

struct aeConfig
{
    aeArchiveType archive_type;
    bool swapHandedness;

    aeConfig()
        : archive_type(aeArchiveType_Ogawa)
        , swapHandedness(true)
    {
    }
};

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
    aeObject* getTopObject();

    void setTime(float time);

private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::unique_ptr<aeObject> m_node_top;
    std::vector<abcChrono> m_times;
};

#endif // aeContext_h
