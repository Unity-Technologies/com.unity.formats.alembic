#ifndef aeContext_h
#define aeContext_h

struct aeConfig
{

};

class aeContext
{
public:
    aeContext();
    bool openArchive(const char *path);

    aeObject* getOrCreateTopObject();

private:
    std::string m_path;
    Abc::OArchive m_archive;
    std::vector<aeObject*> m_nodes;
    aeConfig m_config;
};

#endif // aeContext_h
