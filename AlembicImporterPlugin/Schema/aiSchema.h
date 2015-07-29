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
        , m_lastTime(0.0f)
    {
        AbcSchemaObject abcObj(obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_constant = m_schema.isConstant();
    }

    aiSampleBase* updateSample(float time) override
    {
        DebugLog("aiTSchema::updateSample()");
       
        m_config = m_obj->getContext()->getConfig();

        DebugLog("  Original config: %s", m_config.toString().c_str());

        // get object config overrides (if any)
        invokeConfigCallback(&m_config);

        DebugLog("  Override config: %s", m_config.toString().c_str());

        aiSampleBase *sample = 0;
        bool topologyChanged = false;

        if (m_constant)
        {
            if (m_samples.size() == 0)
            {
                DebugLog("  Create sample for constant object");
                // no need to pass config to readSample
                // => aiSchemaBase::getConfig will return it
                auto &sp = m_samples[time];
                sp.reset(readSample(time, topologyChanged));
                
                sample = sp.get();
            }
            else
            {
                DebugLog("  Update constant object sample");

                bool dataChanged = false;

                sample = m_samples.begin()->second.get();
                
                sample->updateConfig(m_config, topologyChanged, dataChanged);
                
                if (!m_config.forceUpdate && !dataChanged)
                {
                    DebugLog("  Data didn't change, nor update is forced");

                    sample = 0;
                }
            }
        }
        else
        {
            auto &sp = m_samples[time];
            
            if (!sp)
            {
                DebugLog("  Create new time sample");

                // sample was not cached
                sp.reset(readSample(time, topologyChanged));

                sample = sp.get();
            }
            else
            {
                DebugLog("  Update matching time sample");

                bool dataChanged = false;

                sample = sp.get();
                
                sample->updateConfig(m_config, topologyChanged, dataChanged);
                
                if (!m_config.forceUpdate && !dataChanged)
                {
                    DebugLog("  Data didn't change, nor update is forced");
                    
                    if (fabs(time - m_lastTime) <= 0.000001f)
                    {
                        sample = 0;
                    }
                }
            }
        }

        if (sample)
        {
            invokeSampleCallback(sample, topologyChanged);
        }

        m_lastTime = time;

        return sample;
    }

    int erasePastSamples(float from, float range) override
    {
        DebugLog("aiTSchema::erasePastSamples()");
        
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
        DebugLog("aiTSchema::findSample(t=%f)", time);
        
        auto it = (m_constant ? m_samples.begin() : m_samples.find(time));
        return (it == m_samples.end() ? nullptr : it->second.get());
    }

protected:

    virtual Sample* readSample(float time, bool &topologyChanged) = 0;

protected:
    AbcSchema m_schema;
    SampleCont m_samples;
    float m_lastTime;

};

#endif
