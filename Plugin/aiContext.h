#ifndef aiContext_h
#define aiContext_h


typedef std::shared_ptr<Abc::IArchive> abcArchivePtr;

class aiObject;



class aiContext
{
public:
public:
    static aiContext* create();
    static void destroy(aiContext* ctx);

    aiContext();
    ~aiContext();
    bool load(const char *path);
    aiObject* getTopObject();
    void setCurrentObject(abcObject *obj);

private:
    void gatherNodesRecursive(aiObject *n);

private:
    std::vector<aiObject*> m_nodes;
    bool m_reverse_x;
    bool m_triangulate;
    bool m_reverse_index;

#ifdef aiWithDebugLog
    std::string m_dbg_current_object_name;
#endif // aiWithDebugLog
    abcArchivePtr m_archive;
    abcObject m_top_object;
    abcObject m_current;
    Abc::ISampleSelector m_sample_selector;
};



#endif // aiContext_h
