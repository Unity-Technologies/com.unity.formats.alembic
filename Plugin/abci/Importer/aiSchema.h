#pragma once

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

public:
    AbcCoreAbstract::chrono_t m_currentTimeOffset;
    AbcCoreAbstract::chrono_t m_currentTimeInterval;
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

    aiObject* getObject() const;
    // config at last update time
    const aiConfig& getConfig() const;

    void setConfigCallback(aiConfigCallback cb, void *arg);
    void setSampleCallback(aiSampleCallback cb, void *arg);
    void invokeConfigCallback(aiConfig *config) const;
    void invokeSampleCallback(aiSampleBase *sample, bool topologyChanged) const;
    virtual void            cacheAllSamples() =0;
    virtual void cacheSamples(int64_t startIndex, int64_t endIndex)=0;
    virtual int             getTimeSamplingIndex() const = 0;
    virtual int             getNumSamples() const = 0;
    virtual aiSampleBase*   updateSample(const abcSampleSelector& ss) = 0;
    virtual aiSampleBase*   getSample(const abcSampleSelector& ss) const = 0;
    virtual int             getSampleIndex(const abcSampleSelector& ss) const = 0;
    virtual float           getSampleTime(const abcSampleSelector& ss) const = 0;

    void readConfig();
    bool hasVaryingTopology() const { return m_varyingTopology; }

    int getNumProperties() const;
    aiProperty* getPropertyByIndex(int i);
    aiProperty* getPropertyByName(const std::string& name);

protected:
    virtual abcProperties getAbcProperties() = 0;
    void setupProperties();
    void updateProperties(const abcSampleSelector& ss);

protected:
    aiObject *m_obj = nullptr;
    aiConfigCallback m_configCb = nullptr;
    void *m_configCbArg = nullptr;
    aiSampleCallback m_sampleCb = nullptr;
    void *m_sampleCbArg = nullptr;
    aiConfig m_config;
    bool m_constant = false;
    bool m_varyingTopology = false;
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
    {
        AbcSchemaObject abcObj(obj->getAbcObject(), Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_constant = m_schema.isConstant();
        m_timeSampling = m_schema.getTimeSampling();
        m_numSamples = static_cast<int64_t>(m_schema.getNumSamples());
        setupProperties();
    }

    virtual ~aiTSchema()
    {
        if (dontUseCache() && m_theSample)
        {
            delete m_theSample;
        }
        m_samples.clear();
    }

    int getTimeSamplingIndex() const override
    {
        return m_obj->getContext()->getTimeSamplingIndex(m_schema.getTimeSampling());
    }

    int getSampleIndex(const abcSampleSelector& ss) const override
    {
        return static_cast<int>(ss.getIndex(m_timeSampling, m_numSamples));
    }

    float getSampleTime(const abcSampleSelector& ss) const override
    {
        return static_cast<float>(m_timeSampling->getSampleTime(ss.getRequestedIndex()));
    }

    int getNumSamples() const override
    {
        return static_cast<int>(m_numSamples);
    }

    void cacheAllSamples() override
    {
        if (m_constant)
            return;
        readConfig();
        for (int64_t i=0; i< m_numSamples ; i++)
        {
            auto &sp = m_samples[i];
            bool unused = i == 0;
            sp.reset(readSample(i, unused));
        }
    }

    void cacheSamples(int64_t startIndex, int64_t endIndex) override
    {
        if (m_constant) 
            return;
        if (startIndex==0) readConfig();
        for (int64_t i = startIndex; i< endIndex; i++)
        {
            auto &sp = m_samples[i];   
            bool unused = i == 0;
            sp.reset(readSample(i, unused));
        }
    }

    aiSampleBase* updateSample(const abcSampleSelector& ss) override;

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
        return (!m_constant && m_config.cacheSamples);
    }

    inline bool dontUseCache() const
    {
        return (m_constant || !m_config.cacheSamples);
    }

    inline Sample* getSample()
    {
       if (dontUseCache() && m_theSample)
       {
          return m_theSample;
       }
       else
       {
          return nullptr;
       }
    }

    virtual Sample* readSample(const uint64_t idx, bool &topologyChanged) = 0;
protected:
    AbcGeom::ICompoundProperty getAbcProperties() override
    {
        return m_schema.getUserProperties();
    }

protected:
    AbcSchema m_schema;
    SampleCont m_samples;
    Sample* m_theSample = nullptr;
    Abc::TimeSamplingPtr m_timeSampling;
    int64_t m_numSamples = 0;
    int64_t m_lastSampleIndex = -1;
};

template<class Traits>
aiSampleBase* aiTSchema<Traits>::updateSample(const abcSampleSelector& ss)
{
    DebugLog("aiTSchema::updateSample()");

    readConfig();

    Sample* sample = NULL;
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

            sample = readSample(sampleIndex, topologyChanged);

            m_theSample = sample;
        }
        else
        {
            DebugLog("  Update sample");

            bool dataChanged = false;

            sample = m_theSample;

            sample->updateConfig(m_config, topologyChanged, dataChanged);
            

            if (!m_config.forceUpdate && !dataChanged && !m_config.interpolateSamples)
            {
                DebugLog("  Data didn't change, nor update is forced");

                sample = 0;
            }
        }
    }
    else
    {
        auto& sp = m_samples[sampleIndex];

        if (!sp)
        {
            DebugLog("  Create new cache sample");

            sp.reset(readSample(sampleIndex, topologyChanged));

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
            }
            else if (!dataChanged && !m_config.forceUpdate && !m_config.interpolateSamples)
            {
                DebugLog("  Data didn't change, nor update is forced");

                sample = 0;
            }
        }
    }

    m_lastSampleIndex = sampleIndex;

    if (sample)
    {
        if (!m_varyingTopology && m_config.interpolateSamples)
        {   
            AbcCoreAbstract::chrono_t interval = m_schema.getTimeSampling()->getTimeSamplingType().getTimePerCycle();
            auto indexTime = m_timeSampling->getSampleTime(sampleIndex);
            sample->m_currentTimeOffset = std::max(0.0, std::min((ss.getRequestedTime() - indexTime) / interval, 1.0));
            sample->m_currentTimeInterval = interval;
        }
        invokeSampleCallback(sample, topologyChanged);
    }

    updateProperties(ss);

    return sample;
}
