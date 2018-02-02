#pragma once
class aiContext;
class aiSchema;
class aiXform;
class aiPolyMesh;
class aiCamera;

class aiObject
{
public:
    aiObject();
    aiObject(aiContext *ctx, aiObject *parent, const abcObject &abc);
    virtual ~aiObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aiObject*   getChild(int i);
    aiObject*   getParent() const;
    void        setEnabled(bool v);

    virtual aiSample* getSample();
    virtual void updateSample(const abcSampleSelector& ss);
    virtual void waitAsync();


    template<class F>
    void eachChild(const F &f)
    {
        for (auto& c : m_children) { f(*c); }
    }

    template<class F>
    void eachChildRecursive(const F &f)
    {
        for (auto& c : m_children) {
            f(*c);
            c->eachChildRecursive(f);
        }
    }

public:
    // for internal use
    aiContext*  getContext() const;
    const aiConfig& getConfig() const;
    abcObject&  getAbcObject();
    aiObject*   newChild(const abcObject &abc);
    void        removeChild(aiObject *c);

protected:
    using ObjectPtr = std::unique_ptr<aiObject>;

    aiContext   *m_ctx = nullptr;
    abcObject   m_abc;
    aiObject    *m_parent = nullptr;
    std::vector<ObjectPtr> m_children;
    bool m_enabled = true;
};
