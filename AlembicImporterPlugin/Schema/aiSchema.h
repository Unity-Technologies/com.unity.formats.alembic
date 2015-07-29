#ifndef aiSchema_h
#define aiSchema_h

class aiSampleBase;
class aiSchemaBase;


class aiSampleBase
{
public:
    aiSampleBase(aiSchemaBase *schema);
    virtual ~aiSampleBase();

    aiSchemaBase* getSchema() const { return m_schema; }

    // Note: dataChanged MUST be true if topologyChanged is
    virtual void updateConfig(const aiConfig &config, bool &topologyChanged, bool &dataChanged) = 0;

protected:
    aiSchemaBase *m_schema;
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
    virtual const aiSampleBase* findSample(float time) const = 0;
    
    static Abc::ISampleSelector MakeSampleSelector(float time);
    static Abc::ISampleSelector MakeSampleSelector(int64_t index);

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
    typedef std::map<int64_t, SamplePtr> SampleCont;
    typedef typename Traits::AbcSchemaT AbcSchema;
    typedef Abc::ISchemaObject<AbcSchema> AbcSchemaObject;


    aiTSchema(aiObject *obj)
        : aiSchemaBase(obj)
        , m_theSample(0)
        , m_numSamples(0)
        , m_lastSampleIndex(0)
    {
        AbcSchemaObject abcObj(obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_constant = m_schema.isConstant();
        m_timeSampling = m_schema.getTimeSampling();
        m_numSamples = (int64_t) m_schema.getNumSamples();
    }

    virtual ~aiTSchema()
    {
        if (m_theSample)
        {
            delete m_theSample;
        }
    }

    int64_t getSampleIndex(float time) const
    {
        Abc::ISampleSelector ss(double(time), Abc::ISampleSelector::kFloorIndex);
        return ss.getIndex(m_timeSampling, m_numSamples);
    }

    aiSampleBase* updateSample(float time) override
    {
        DebugLog("aiTSchema::updateSample()");
       
        m_config = m_obj->getContext()->getConfig();

        DebugLog("  Original config: %s", m_config.toString().c_str());

        // get object config overrides (if any)
        invokeConfigCallback(&m_config);

        DebugLog("  Override config: %s", m_config.toString().c_str());

        Sample *sample = 0;
        bool topologyChanged = false;
        int64_t sampleIndex = getSampleIndex(time);
        
        if (dontUseCache())
        {
            if (m_samples.size() > 0)
            {
                DebugLog("  Clear cached samples");
                
                m_samples.clear();
            }
            
            if (m_theSample == 0)
            {
                DebugLog("  Create sample for constant object");
                
                sample = readSample(time, topologyChanged);
                
                m_theSample = sample;
            }
            else
            {
                if (!m_constant && sampleIndex != m_lastSampleIndex)
                {
                    // note: readSmaple should use m_theSample internally
                    sample = readSample(time, topologyChanged);
                }
                else
                {
                    DebugLog("  Update constant object sample");

                    bool dataChanged = false;

                    sample = m_theSample;
                    
                    sample->updateConfig(m_config, topologyChanged, dataChanged);
                    
                    if (!m_config.forceUpdate && !dataChanged)
                    {
                        DebugLog("  Data didn't change, nor update is forced");

                        sample = 0;
                    }
                }
            }
        }
        else
        {
            auto &sp = m_samples[sampleIndex];
            
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
                
                if (sampleIndex == m_lastSampleIndex && !m_config.forceUpdate && !dataChanged)
                {
                    DebugLog("  Data didn't change, nor update is forced");
                    
                    sample = 0;
                }
            }
        }

        if (sample)
        {
            invokeSampleCallback(sample, topologyChanged);
        }

        m_lastSampleIndex = sampleIndex;

        if (useCache() && m_samples.size() > size_t(m_config.cacheSamples))
        {
            erasePastSamples();
        }

        return sample;
    }

    const Sample* findSample(float time) const override
    {
        DebugLog("aiTSchema::findSample(t=%f)", time);

        int64_t sampleIndex = getSampleIndex(time);

        if (dontUseCache())
        {
            return (sampleIndex == m_lastSampleIndex ? m_theSample : nullptr);
        }
        else
        {
            auto it = m_samples.find(sampleIndex);
            return (it != m_samples.end() ? it->second.get() : nullptr);
        }
    }

protected:

    inline bool useCache() const
    {
        return (!m_constant && m_config.cacheSamples > 1);
    }

    inline bool dontUseCache() const
    {
        return (m_constant || m_config.cacheSamples <= 1);
    }

    inline Sample* getSample()
    {
       if (dontUseCache() && m_theSample)
       {
          return m_theSample;
       }
       else
       {
          return 0;
       }
    }

    virtual Sample* readSample(float time, bool &topologyChanged) = 0;

    void erasePastSamples()
    {
        DebugLog("aiTSchema::erasePastSamples()");
        
        size_t maxSamples = (size_t) m_config.cacheSamples;

        auto first = m_samples.begin();
        auto last = m_samples.end();
        --last;

        while (m_samples.size() > maxSamples)
        {
            int64_t diff0 = m_lastSampleIndex - first->first;
            int64_t diff1 = last->first - m_lastSampleIndex;

            if (diff1 > diff0)
            {
                m_samples.erase(last);
                last = m_samples.end();
                --last;
            }
            else if (diff0 != 0)
            {
                m_samples.erase(first);
                first = m_samples.begin();
            }
            else
            {
                // first == last == current
                // note: should never each here as we required at least 2 samples cache
                break;
            }
        }
    }

protected:
    AbcSchema m_schema;
    SampleCont m_samples;
    Sample* m_theSample;
    AbcCoreAbstract::TimeSamplingPtr m_timeSampling;
    int64_t m_numSamples;
    int64_t m_lastSampleIndex;
};

#endif
