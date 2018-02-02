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
    ~aiObject();

    const char* getName() const;
    const char* getFullName() const;
    uint32_t    getNumChildren() const;
    aiObject*   getChild(int i);
    aiObject*   getParent() const;
    aiSchema*   getSchema() const;
    void        setEnabled(bool v);

    void        updateSample(const abcSampleSelector& ss);

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
    abcObject&  getAbcObject();
    aiObject*   newChild(const abcObject &abc);
    void        removeChild(aiObject *c);

private:
    using ObjectPtr = std::unique_ptr<aiObject>;

    aiContext   *m_ctx = nullptr;
    abcObject   m_abc;
    aiObject    *m_parent = nullptr;
    std::vector<ObjectPtr> m_children;
    bool m_enabled = true;

    std::unique_ptr<aiSchema> m_schema;
};
