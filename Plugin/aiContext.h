#ifndef aiContext_h
#define aiContext_h


typedef std::shared_ptr<Abc::IArchive> abcArchivePtr;

class aiObject;
const int aiMagicCtx = 0x00585443; // "CTX"


class aiContext
{
public:
    static aiContext* create();
    static void destroy(aiContext* ctx);

public:
    aiContext();
    ~aiContext();
    bool load(const char *path);
    aiObject* getTopObject();

private:
    void gatherNodesRecursive(aiObject *n);

private:
#ifdef aiWithDebugLog
    int m_magic;
#endif // aiWithDebugLog
    abcArchivePtr m_archive;
    std::vector<aiObject*> m_nodes;
};



#endif // aiContext_h
