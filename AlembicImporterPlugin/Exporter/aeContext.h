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
    aeContext(aeConfig &conf);
    ~aeContext();
    void reset();
    bool openArchive(const char *path);

    aeObject* getTopObject();

private:
    std::string m_path;
    Abc::OArchive m_archive;
    std::vector<aeObject*> m_nodes;
    aeConfig m_config;
};

#endif // aeContext_h
