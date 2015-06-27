#ifndef aiSchema_h
#define aiSchema_h

class aiSampleBase;
class aiSchemaBase;


class aiSampleBase
{
public:
    aiSampleBase(aiSchemaBase *schema, float time);
    virtual ~aiSampleBase();

    aiSchemaBase* getSchema() const { return m_schema; }
    float getTime() const { return m_time; }

protected:
    aiSchemaBase *m_schema;
    float m_time;
};


class aiSchemaBase
{
public:
    aiSchemaBase(aiObject *obj);
    virtual ~aiSchemaBase();

    aiObject* getObject();
    const aiImportConfig& getImportConfig() const;
    void setCallback(aiSampleCallback cb, void *arg);
    void invokeCallback(aiSampleBase *sample);

    virtual aiSampleBase* updateSample(float time) = 0;
    virtual int erasePastSamples(float time, float range_keep) = 0;
    virtual const aiSampleBase* findSample(float time) const = 0;
    virtual void debugDump() const {}

    static Abc::ISampleSelector makeSampleSelector(float time);
    static Abc::ISampleSelector makeSampleSelector(aiIndex index);

protected:
    aiObject *m_obj;
    aiSampleCallback m_cb;
    void *m_cb_arg;
};



template<class Traits>
class aiTSchema : public aiSchemaBase
{
public:
    typedef typename Traits::SampleT Sample;
    typedef std::unique_ptr<Sample> SamplePtr;
    typedef std::map<float, SamplePtr> SampleCont;
    typedef typename Traits::AbcSchemaT AbcSchema;


    aiTSchema(aiObject *obj)
        : aiSchemaBase(obj)
    {
    }

    aiSampleBase* updateSample(float time) override
    {
        auto &sp = m_samples[time];
        if (!sp) {
            sp.reset(readSample(time));
        }
        invokeCallback(sp.get());
        return sp.get();
    }

    int erasePastSamples(float time, float range_keep) override
    {
        int r = 0;
        for (auto i = m_samples.begin(); i != m_samples.end();) {
            if (i->first < time || i->first > (time + range_keep)) {
                m_samples.erase(i++);
                ++r;
            }
            else {
                ++i;
            }
        }
        return r;
    }

    const Sample* findSample(float time) const override
    {
        auto it = m_samples.find(time);
        return it == m_samples.end() ? nullptr : it->second.get();
    }

protected:
    virtual Sample* readSample(float time) = 0;

protected:
    AbcSchema m_schema;
    SampleCont m_samples;
};


#endif // aiSchema_h
