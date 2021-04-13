#pragma once

class aiSample
{
 public:
    aiSample(aiSchema* schema);
    virtual ~aiSample();

    virtual aiSchema* getSchema() const
    {
        return m_schema;
    }
    const aiConfig& getConfig() const;

 public:
    bool visibility = true;

 protected:
    aiSchema* m_schema = nullptr;
};

class aiSchema : public aiObject
{
    using super = aiObject;
 public:
    using aiPropertyPtr = std::unique_ptr<aiProperty>;

    aiSchema(aiObject* parent, const abcObject& abc);
    virtual ~aiSchema();

    bool isConstant() const;
    bool isDataUpdated() const;
    void markForceUpdate();
    void markForceSync();
    int getNumProperties() const;
    aiProperty* getPropertyByIndex(int i);
    aiProperty* getPropertyByName(const std::string& name);

 protected:
    virtual abcProperties getAbcProperties() = 0;
    void setupProperties();
    void updateProperties(const abcSampleSelector& ss);

 protected:
    bool m_constant = false;
    bool m_data_updated = false;
    bool m_force_update = false;
    std::vector<aiPropertyPtr> m_properties; // sorted vector
};

template<class Traits>
class aiTSchema : public aiSchema
{
    using super = aiSchema;
 public:
    using Sample = typename Traits::SampleT;
    using SamplePtr = std::shared_ptr<Sample>;
    using SampleCont = std::map<int64_t, SamplePtr>;
    using AbcSchema = typename Traits::AbcSchemaT;
    using AbcSchemaObject = Abc::ISchemaObject<AbcSchema>;

    aiTSchema(aiObject* parent, const abcObject& abc)
        : super(parent, abc)
    {
        AbcSchemaObject abcObj(abc, Abc::kWrapExisting);
        m_schema = abcObj.getSchema();
        m_time_sampling = m_schema.getTimeSampling();
        m_num_samples = static_cast<int64_t>(m_schema.getNumSamples());

        m_visibility_prop = AbcGeom::GetVisibilityProperty(const_cast<abcObject&>(abc));
        m_constant = m_schema.isConstant() && (!m_visibility_prop.valid() || m_visibility_prop.isConstant());

        setupProperties();
    }

    int getTimeSamplingIndex() const
    {
        return getContext()->getTimeSamplingIndex(m_schema.getTimeSampling());
    }

    int getSampleIndex(const abcSampleSelector& ss) const
    {
        return static_cast<int>(ss.getIndex(m_time_sampling, m_num_samples));
    }

    float getSampleTime(const abcSampleSelector& ss) const
    {
        return static_cast<float>(m_time_sampling->getSampleTime(ss.getRequestedIndex()));
    }

    int getNumSamples() const
    {
        return static_cast<int>(m_num_samples);
    }

    Sample* getSample() override
    {
        return m_sample.get();
    }

    virtual Sample* newSample() = 0;

    void updateSample(const abcSampleSelector& ss) override
    {
        updateSampleBody(ss);
    }

    virtual void readSample(Sample& sample, uint64_t idx)
    {
        m_force_update_local = m_force_update;
        readSampleBody(sample, idx);
    }

    virtual void cookSample(Sample& sample)
    {
        cookSampleBody(sample);
    }

 protected:
    virtual void updateSampleBody(const abcSampleSelector& ss)
    {
        if (!m_enabled)
            return;

        Sample* sample = nullptr;
        int64_t sample_index = getSampleIndex(ss);
        auto& config = getConfig();

        auto visible = readVisibility(ss) != 0;
        auto updateVisibility = m_sample && m_sample->visibility != visible;
        if (!m_sample || (!m_constant && sample_index != m_last_sample_index) || m_force_update ||
            updateVisibility)
        {
            m_sample_index_changed = true;
            if (!m_sample)
                m_sample.reset(newSample());
            sample = m_sample.get();
            readSample(*sample, sample_index);
        }
        else
        {
            m_sample_index_changed = false;
            sample = m_sample.get();
            if (m_constant || !config.interpolate_samples)
                sample = nullptr;
        }

        if (sample && config.interpolate_samples)
        {
            sample->visibility = visible; // read the visibility
            auto& ts = *m_time_sampling;
            double requested_time = ss.getRequestedTime();
            double index_time = ts.getSampleTime(sample_index);
            double interval = 0;
            if (ts.getTimeSamplingType().isAcyclic())
            {
                auto tsi = std::min((size_t)sample_index + 1, ts.getNumStoredTimes() - 1);
                interval = ts.getSampleTime(tsi) - index_time;
            }
            else
            {
                interval = ts.getTimeSamplingType().getTimePerCycle();
            }

            float prev_offset = m_current_time_offset;
            m_current_time_offset = interval == 0.0 ? 0.0f :
                                    (float)std::max(0.0, std::min((requested_time - index_time) / interval, 1.0));
            m_current_time_interval = (float)interval;

            // skip if time offset is not changed
            if (sample_index == m_last_sample_index && prev_offset == m_current_time_offset && !m_force_update
                && !updateVisibility)
                sample = nullptr;
        }

        if (sample)
        {

            cookSample(*sample);
            m_data_updated = true;
        }
        else
        {
            m_data_updated = false;
        }
        updateProperties(ss);

        m_last_sample_index = sample_index;
        m_force_update = false;
    }

    virtual void readSampleBody(Sample& sample, uint64_t idx) = 0;
    virtual void cookSampleBody(Sample& sample) = 0;

    AbcGeom::ICompoundProperty getAbcProperties() override
    {
        return m_schema.getUserProperties();
    }

    int8_t readVisibility(const abcSampleSelector& ss)
    {
        if (m_visibility_prop.valid() && m_visibility_prop.getNumSamples() > 0)
        {
            int8_t v;
            m_visibility_prop.get(v, ss);
            return v;
        }

        return 1; // -1 deffered, 0 invisible, 1 visible
    }

 protected:
    AbcSchema m_schema;
    Abc::TimeSamplingPtr m_time_sampling;
    AbcGeom::IVisibilityProperty m_visibility_prop;
    SamplePtr m_sample;
    int64_t m_num_samples = 0;
    int64_t m_last_sample_index = -1;
    float m_current_time_offset = 0;
    float m_current_time_interval = 0;
    bool m_sample_index_changed = false;

    bool m_force_update_local = false; // m_force_update for worker thread
};
