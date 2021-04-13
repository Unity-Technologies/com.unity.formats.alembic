#pragma once

class aiProperty
{
 public:
    aiProperty();
    virtual ~aiProperty();
    virtual const std::string& getName() const = 0;
    virtual aiPropertyType getPropertyType() const = 0;
    virtual int getNumSamples() const = 0;
    virtual int getTimeSamplingIndex() const = 0;

    // todo: implement caching. currently getData() simply redirect to updateSample()
    virtual aiPropertyData* updateSample(const abcSampleSelector& ss) = 0;
    virtual void getDataPointer(const abcSampleSelector& ss, aiPropertyData& data) = 0;
    virtual void copyData(const abcSampleSelector& ss, aiPropertyData& data) = 0;

    bool isArray() const
    {
        auto t = getPropertyType();
        return t >= aiPropertyType::ArrayTypeBegin && t <= aiPropertyType::ArrayTypeEnd;
    }

    void setActive(bool v);

 protected:
    bool m_active = false;
};

aiProperty* aiMakeProperty(aiSchema* schema, abcProperties cprop, Abc::PropertyHeader header);
