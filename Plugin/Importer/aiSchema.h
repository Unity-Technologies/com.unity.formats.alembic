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
    typedef std::unique_ptr<aiProperty> aiPropertyPtr;

    aiSchemaBase(aiObject *obj);
    virtual ~aiSchemaBase();

    aiObject* getObject();
    // config at last update time
    const aiConfig& getConfig() const;

    void setConfigCallback(aiConfigCallback cb, void *arg);
    void setSampleCallback(aiSampleCallback cb, void *arg);
    void invokeConfigCallback(aiConfig *config);
    void invokeSampleCallback(aiSampleBase *sample, bool topologyChanged);

    virtual int             getTimeSamplingIndex() const = 0;
    virtual int             getNumSamples() const = 0;
    virtual aiSampleBase*   updateSample(const abcSampleSelector& ss) = 0;
    virtual aiSampleBase*   getSample(const abcSampleSelector& ss) const = 0;
    virtual int             getSampleIndex(const abcSampleSelector& ss) const = 0;
    virtual float           getSampleTime(const abcSampleSelector& ss) const = 0;

    // for multithreaded updates, don't invoke C# callbacks from work threads
    void readConfig();
    void notifyUpdate();

    int getNumProperties() const;
    aiProperty* getPropertyByIndex(int i);
    aiProperty* getPropertyByName(const std::string& name);

protected:
    virtual abcProperties getAbcProperties() = 0;
    void setupProperties();
    void updateProperties(const abcSampleSelector& ss);

protected:
    aiObject *m_obj;
    aiConfigCallback m_configCb;
    void *m_configCbArg;
    aiSampleCallback m_sampleCb;
    void *m_sampleCbArg;
    aiConfig m_config;
    bool m_constant;
    bool m_varyingTopology;
    aiSampleBase *m_pendingSample;
    bool m_pendingTopologyChanged;
    std::vector<aiPropertyPtr> m_properties; // sorted vector
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
        , m_lastSampleIndex(-1)
    {
        AbcSchemaObject abcObj(obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_constant = m_schema.isConstant();
        m_timeSampling = m_schema.getTimeSampling();
        m_numSamples = (int64_t) m_schema.getNumSamples();
        setupProperties();
    }

    virtual ~aiTSchema()
    {
        if (m_theSample)
        {
            delete m_theSample;
        }
    }

    int getTimeSamplingIndex() const override
    {
        return m_obj->getContext()->getTimeSamplingIndex(m_schema.getTimeSampling());
    }

    int getSampleIndex(const abcSampleSelector& ss) const override
    {
        return (int)ss.getIndex(m_timeSampling, m_numSamples);
    }

    float getSampleTime(const abcSampleSelector& ss) const override
    {
        return (float)m_timeSampling->getSampleTime(ss.getRequestedIndex());
    }

    int getNumSamples() const override
    {
        return (int)m_numSamples;
    }

    aiSampleBase* updateSample(const abcSampleSelector& ss) override
    {
        DebugLog("aiTSchema::updateSample()");
        
        bool useThreads = m_obj->getContext()->getConfig().useThreads;

        if (!useThreads)
        {
            readConfig();
        }

        Sample *sample = 0;
        bool topologyChanged = false;
        int64_t sampleIndex = getSampleIndex(ss);
        
        if (dontUseCache())
        {
            if (m_samples.size() > 0)
            {
                DebugLog("  Clear cached samples");
                
                m_samples.clear();
            }
            
            // don't need to check m_constant here, sampleIndex wouldn't change
            if (m_theSample == 0 || sampleIndex != m_lastSampleIndex)
            {
                DebugLog("  Read sample");
                
                sample = readSample(ss, topologyChanged);
                
                m_theSample = sample;
            }
            else
            {
                DebugLog("  Update sample");

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
        else
        {
            auto &sp = m_samples[sampleIndex];
            
            if (!sp)
            {
                DebugLog("  Create new cache sample");
                
                sp.reset(readSample(ss, topologyChanged));

                sample = sp.get();
            }
            else
            {
                DebugLog("  Update cached sample");
                
                bool dataChanged = false;

                sample = sp.get();
                
                sample->updateConfig(m_config, topologyChanged, dataChanged);

                // force update if sample has changed from previously queried one
                if (sampleIndex != m_lastSampleIndex)
                {
                    if (m_varyingTopology)
                    {
                        topologyChanged = true;
                    }
                    dataChanged = true;
                }
                else if (!dataChanged && !m_config.forceUpdate)
                {
                    DebugLog("  Data didn't change, nor update is forced");
                    
                    sample = 0;
                }
            }
        }

        m_lastSampleIndex = sampleIndex;

        if (!useThreads)
        {
            if (sample)
            {
                invokeSampleCallback(sample, topologyChanged);
            }
        }
        else
        {
            m_pendingSample = sample;
            m_pendingTopologyChanged = topologyChanged;
        }

        if (useCache() && m_samples.size() > size_t(m_config.cacheSamples))
        {
            erasePastSamples();
        }

        updateProperties(ss);

        return sample;
    }

    Sample* getSample(const abcSampleSelector& ss) const override
    {
        DebugLog("aiTSchema::findSample(t=%f)", (float)ss.getRequestedTime());

        int sampleIndex = getSampleIndex(ss);

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

    virtual Sample* readSample(const abcSampleSelector& ss, bool &topologyChanged) = 0;

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
    AbcGeom::ICompoundProperty getAbcProperties() override
    {
        return m_schema.getUserProperties();
    }

protected:
    AbcSchema m_schema;
    SampleCont m_samples;
    Sample* m_theSample;
    Abc::TimeSamplingPtr m_timeSampling;
    int64_t m_numSamples;
    int64_t m_lastSampleIndex;
};

#endif
