#ifndef aiContext_h
#define aiContext_h

#include "aiThreadPool.h"

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
    float getStartTime() const;
    float getEndTime() const;

    void runTask(const std::function<void ()> &task);
    void waitTasks();

private:
    void gatherNodesRecursive(aiObject *n);

private:
#ifdef aiDebug
    int m_magic;
#endif // aiDebug
    abcArchivePtr m_archive;
    std::vector<aiObject*> m_nodes;
    aiTaskGroup m_tasks;
    double m_timeRange[2];
};



#endif // aiContext_h
