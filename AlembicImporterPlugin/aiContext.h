#ifndef aiContext_h
#define aiContext_h

#include "aiThreadPool.h"

class aiObject;

class aiContext
{
public:
    static aiContext* create(int uid);
    static void destroy(aiContext* ctx);

public:
    aiContext(int uid=-1);
    ~aiContext();
    
    bool load(const char *path);
    aiObject* getTopObject();
    float getStartTime() const;
    float getEndTime() const;

    void runTask(const std::function<void ()> &task);
    void waitTasks();

    Abc::IArchive getArchive() const;
    const std::string& getPath() const;
    int getUid() const;

private:
    std::string normalizePath(const char *path) const;
    void reset();
    void gatherNodesRecursive(aiObject *n);

private:
    std::string m_path;
    Abc::IArchive m_archive;
    std::vector<aiObject*> m_nodes;
    aiTaskGroup m_tasks;
    double m_timeRange[2];
    int m_uid;
};



#endif // aiContext_h
