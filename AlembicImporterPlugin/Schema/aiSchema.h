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

    // Note: dataChanged MUST be true if topologyChanged is
    virtual void updateConfig(const aiConfig &config, bool &topologyChanged, bool &dataChanged) = 0;

protected:
    aiSchemaBase *m_schema;
    float m_time;
    aiConfig m_config;
};


class aiSchemaBase
{
public:
    aiSchemaBase(aiObject *obj);
    virtual ~aiSchemaBase();

    aiObject* getObject();
    // config at last update time
    const aiConfig& getConfig() const;

    void setConfigCallback(aiConfigCallback cb, void *arg);
    void setSampleCallback(aiSampleCallback cb, void *arg);
    void invokeConfigCallback(aiConfig *config);
    void invokeSampleCallback(aiSampleBase *sample, bool topologyChanged);

    virtual aiSampleBase* updateSample(float time) = 0;
    virtual int erasePastSamples(float from, float range) = 0;
    virtual const aiSampleBase* findSample(float time) const = 0;
    
    static Abc::ISampleSelector MakeSampleSelector(float time);
    static Abc::ISampleSelector MakeSampleSelector(uint32_t index);

protected:
    aiObject *m_obj;
    aiConfigCallback m_configCb;
    void *m_configCbArg;
    aiSampleCallback m_sampleCb;
    void *m_sampleCbArg;
    aiConfig m_config;
    bool m_constant;
    bool m_varyingTopology;
};


template <class Traits>
class aiTSchema : public aiSchemaBase
{
public:
    typedef typename Traits::SampleT Sample;
    typedef std::unique_ptr<Sample> SamplePtr;
    typedef std::map<float, SamplePtr> SampleCont;
    typedef typename Traits::AbcSchemaT AbcSchema;
    typedef Abc::ISchemaObject<AbcSchema> AbcSchemaObject;


    aiTSchema(aiObject *obj)
        : aiSchemaBase(obj)
    {
        AbcSchemaObject abcObj(obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_constant = m_schema.isConstant();
    }

    aiSampleBase* updateSample(float time) override
    {
        m_config = m_obj->getContext()->getConfig();
        // get object config overrides (if any)
        invokeConfigCallback(&m_config);

        aiSampleBase *sample = 0;
        bool topologyChanged = false;

        if (m_constant)
        {
            if (m_samples.size() == 0)
            {
                // no need to pass config to readSample
                // => aiSchemaBase::getConfig will return it
                auto &sp = m_samples[time];
                sp.reset(readSample(time));
                
                sample = sp.get();
                topologyChanged = true; // first sample ever read, this has to be true
            }
            else
            {
                bool dataChanged = false;

                sample = m_samples.begin()->second.get();
                
                sample->updateConfig(m_config, dataChanged, topologyChanged);
                
                if (!m_config.forceUpdate && !dataChanged)
                {
                    sample = 0;
                }
            }
        }
        else
        {
            auto &sp = m_samples[time];
            
            if (!sp)
            {
                // sample was not cached
                sp.reset(readSample(time));

                sample = sp.get();
                topologyChanged = (m_samples.size() == 1 || m_varyingTopology);
            }
            else
            {
                bool dataChanged = false;

                sample = sp.get();
                
                sample->updateConfig(m_config, dataChanged, topologyChanged);
                
                if (!m_config.forceUpdate && !dataChanged)
                {
                    sample = 0;
                }
            }
        }

        if (sample)
        {
            invokeSampleCallback(sample, topologyChanged);
        }

        return sample;
    }

    int erasePastSamples(float from, float range) override
    {
        if (m_constant)
        {
            return 0;
        }

        int r = 0;

        for (auto i=m_samples.begin(); i!=m_samples.end();)
        {
            if (i->first < from || i->first > (from + range))
            {
                m_samples.erase(i++);
                ++r;
            }
            else
            {
                ++i;
            }
        }

        return r;
    }

    const Sample* findSample(float time) const override
    {
        auto it = (m_constant ? m_samples.begin() : m_samples.find(time));
        return (it == m_samples.end() ? nullptr : it->second.get());
    }

protected:

    virtual Sample* readSample(float time) = 0;

protected:
    AbcSchema m_schema;
    SampleCont m_samples;

};

/*
class aiSchema
{
public:
    aiSchema();
    aiSchema(aiObject *obj);
    virtual ~aiSchema();

    virtual void updateSample() = 0;

protected:
    aiObject *m_obj;
};
*/

#endif
