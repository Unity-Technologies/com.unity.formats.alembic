#ifndef aeObject_h
#define aeObject_h

class aeProperty
{
public:
    aeProperty();
    virtual ~aeProperty();
    virtual const char* getName() const = 0;
    virtual bool isArray() const = 0; // true if property is array (abcFloatArrayProperty etc.)
    virtual void writeSample(const void *data, int data_num) = 0; // data_num is ignored on scalar property
};


class aeObject
{
public:
    aeObject(aeContext *ctx, aeObject *parent, abcObject *abc);
    virtual ~aeObject();

    const char* getName() const;
    const char* getFullName() const;
    size_t      getNumChildren() const;
    aeObject*   getChild(int i);
    aeObject*   getParent();

    aeContext*          getContext();
    const aeConfig&     getConfig() const;
    virtual abcObject&  getAbcObject();
    virtual abcProperties getAbcProperties();

    /// T: aeCamera, aeXForm, aePoint, aePolyMesh
    template<class T> T*    newChild(const char *name);
    void                    removeChild(aeObject *c);

    /// T: abcFloatArrayProperty, abcFloatProperty, etc
    template<class T>
    aeProperty*             newProperty(const char *name);

    virtual size_t  getNumSamples();
    virtual void    setFromPrevious();

protected:
    typedef std::unique_ptr<aeProperty> aePropertyPtr;
    aeContext                   *m_ctx;
    aeObject                    *m_parent;
    std::unique_ptr<abcObject>  m_abc;
    std::vector<aePropertyPtr>  m_properties;
    std::vector<aeObject*>      m_children;
};

#endif // aeObject_h
