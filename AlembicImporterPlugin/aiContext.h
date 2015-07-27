#ifndef aiContext_h
#define aiContext_h

#include "aiThreadPool.h"

class aiObject;

class aiContext
{
public:
    typedef std::function<void ()> task_t;

    static aiContext* create(int uid);
    static void destroy(aiContext* ctx);

public:
    aiContext(int uid=-1);
    ~aiContext();
    
    bool load(const char *path);
    
    const aiConfig& getConfig() const;
    void setConfig(const aiConfig &config);

    aiObject* getTopObject();
    void destroyObject(aiObject *obj);

    float getStartTime() const;
    float getEndTime() const;

    void setTimeRangeToKeepSamples(float time, float range);
    void updateSamples(float time, bool useThreads);
    void updateSamplesBegin(float time);
    void updateSamplesEnd();
    void erasePastSamples(float time, float rangeKeep);

    void enqueueTask(const task_t &task);
    void waitTasks();

    Abc::IArchive getArchive() const;
    const std::string& getPath() const;
    int getUid() const;

private:
    std::string normalizePath(const char *path) const;
    void reset();
    void gatherNodesRecursive(aiObject *n);
    std::vector<aiObject*>::iterator destroyObject(aiObject *obj, std::vector<aiObject*>::iterator searchFrom);

private:
    std::string m_path;
    Abc::IArchive m_archive;
    std::vector<aiObject*> m_nodes;
    aiTaskGroup m_tasks;
    double m_timeRange[2];
    int m_uid;
    aiConfig m_config;
    std::tuple<float, float> m_timeRangeToKeepSamples; // start, range
};



#endif // aiContext_h
