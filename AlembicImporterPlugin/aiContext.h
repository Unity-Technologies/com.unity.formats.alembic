#ifndef aiContext_h
#define aiContext_h

#include "aiThreadPool.h"

typedef std::shared_ptr<Abc::IArchive> abcArchivePtr;

class aiObject;


struct aiImportConfig
{
    uint8_t triangulate;
    uint8_t revert_x;
    uint8_t revert_face;

    aiImportConfig() : triangulate(true), revert_x(true), revert_face(false) {}
};


class aiContext
{
public:
    typedef std::function<void()> task_t;

    static aiContext* create();
    static void destroy(aiContext* ctx);

public:
    aiContext();
    ~aiContext();
    bool load(const char *path);
    aiObject* getTopObject();
    const aiImportConfig& getImportConfig() const;
    void setImportConfig(const aiImportConfig &conf);
    void setTimeRangeToKeepSamples(float time, float range);

    float getStartTime() const;
    float getEndTime() const;

    void updateSamples(float time);
    void updateSamplesBegin(float time);
    void updateSamplesEnd();
    void erasePastSamples(float time, float range_keep);

    void enqueueTask(const task_t &task);
    void waitTasks();

    void debugDump() const;

private:
    void gatherNodesRecursive(aiObject *n);

private:
    abcArchivePtr m_archive;
    std::vector<aiObject*> m_nodes;
    std::tuple<float, float> m_time_range_to_keep_samples;

    aiImportConfig m_iconfig;
    aiTaskGroup m_tasks;
    double m_time_range[2];
};



#endif // aiContext_h
