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

#ifndef UNITY_ALEMBIC_NO_TBB
    void runTask(const std::function<void ()> &task);
    void waitTasks();
#endif // UNITY_ALEMBIC_NO_TBB

private:
    void gatherNodesRecursive(aiObject *n);

private:
#ifdef aiDebug
    int m_magic;
#endif // aiDebug
    abcArchivePtr m_archive;
    std::vector<aiObject*> m_nodes;
    
#ifndef UNITY_ALEMBIC_NO_TBB
    tbb::task_group m_tasks;
#endif // UNITY_ALEMBIC_NO_TBB
};



#endif // aiContext_h
