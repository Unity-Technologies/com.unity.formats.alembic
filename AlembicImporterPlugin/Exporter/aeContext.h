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

    aeConfig()
        : archive_type(aeArchiveType_Ogawa)
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

    aeObject* getTopObject();
    aeObject* createObject(aeObject *parent, const char *name);
    void destroyObject(aeObject *obj);

    void setTime(float time);

private:
    bool destroyObject(aeObject *obj, NodeIter searchFrom, NodeIter &next);

private:
    aeConfig m_config;
    Abc::OArchive m_archive;
    std::vector<aeObject*> m_nodes;
    std::vector<abcChrono> m_times;
};

#endif // aeContext_h
