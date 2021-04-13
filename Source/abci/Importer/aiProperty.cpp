#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiProperty.h"
#include "aiObject.h"
#include "aiSchema.h"

aiProperty::aiProperty()
{
}

aiProperty::~aiProperty()
{
}

void aiProperty::setActive(bool v)
{
    m_active = v;
}

// type -> type ID
template<class T>
struct aiGetPropertyTypeID
{
    static const aiPropertyType value = aiPropertyType::Unknown;
};

template<>
struct aiGetPropertyTypeID<abcBoolProperty>
{
    static const aiPropertyType value = aiPropertyType::Bool;
};
template<>
struct aiGetPropertyTypeID<abcIntProperty>
{
    static const aiPropertyType value = aiPropertyType::Int;
};
template<>
struct aiGetPropertyTypeID<abcUIntProperty>
{
    static const aiPropertyType value = aiPropertyType::UInt;
};
template<>
struct aiGetPropertyTypeID<abcFloatProperty>
{
    static const aiPropertyType value = aiPropertyType::Float;
};
template<>
struct aiGetPropertyTypeID<abcFloat2Property>
{
    static const aiPropertyType value = aiPropertyType::Float2;
};
template<>
struct aiGetPropertyTypeID<abcFloat3Property>
{
    static const aiPropertyType value = aiPropertyType::Float3;
};
template<>
struct aiGetPropertyTypeID<abcFloat4Property>
{
    static const aiPropertyType value = aiPropertyType::Float4;
};
template<>
struct aiGetPropertyTypeID<abcFloat4x4Property>
{
    static const aiPropertyType value = aiPropertyType::Float4x4;
};

template<>
struct aiGetPropertyTypeID<abcBoolArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::BoolArray;
};
template<>
struct aiGetPropertyTypeID<abcIntArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::IntArray;
};
template<>
struct aiGetPropertyTypeID<abcUIntArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::UIntArray;
};
template<>
struct aiGetPropertyTypeID<abcFloatArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::FloatArray;
};
template<>
struct aiGetPropertyTypeID<abcFloat2ArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::Float2Array;
};
template<>
struct aiGetPropertyTypeID<abcFloat3ArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::Float3Array;
};
template<>
struct aiGetPropertyTypeID<abcFloat4ArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::Float4Array;
};
template<>
struct aiGetPropertyTypeID<abcFloat4x4ArrayProperty>
{
    static const aiPropertyType value = aiPropertyType::Float4x4Array;
};


// scalar properties

template<class T>
class aiTScalarProprty : public aiProperty
{
    using super = aiProperty;
 public:
    using property_type = T;
    using value_type = typename T::value_type;

    aiTScalarProprty(aiSchema* schema, abcProperties cprop, const std::string& name)
        : m_schema(schema), m_abcprop(new property_type(cprop, name))
    {
        DebugLog("aeTScalarProprty::aeTScalarProprty() %s", m_abcprop->getName().c_str());
    }

    const std::string& getName() const override
    {
        return m_abcprop->getName();
    }
    aiPropertyType getPropertyType() const override
    {
        return aiGetPropertyTypeID<T>::value;
    }
    int getNumSamples() const override
    {
        return (int)m_abcprop->getNumSamples();
    }

    int getTimeSamplingIndex() const override
    {
        return m_schema->getContext()->getTimeSamplingIndex(m_abcprop->getTimeSampling());
    }

    aiPropertyData* updateSample(const abcSampleSelector& ss) override
    {
        if (m_active)
        {
            m_value = m_abcprop->getValue(ss);
            m_data = { &m_value, 1, getPropertyType() };
        }
        return &m_data;
    }

    void getDataPointer(const abcSampleSelector& ss, aiPropertyData& dst) override
    {
        dst = *updateSample(ss);
    }

    void copyData(const abcSampleSelector& ss, aiPropertyData& dst) override
    {
        auto src = updateSample(ss);
        if (dst.data)
        {
            memcpy(dst.data, src->data, sizeof(T) * src->size);
        }
        dst.size = src->size;
        dst.type = src->type;
    }

 private:
    aiSchema* m_schema;
    std::unique_ptr<property_type> m_abcprop;
    value_type m_value;
    aiPropertyData m_data;
};
template
class aiTScalarProprty<abcBoolProperty>;
template
class aiTScalarProprty<abcIntProperty>;
template
class aiTScalarProprty<abcUIntProperty>;
template
class aiTScalarProprty<abcFloatProperty>;
template
class aiTScalarProprty<abcFloat2Property>;
template
class aiTScalarProprty<abcFloat3Property>;
template
class aiTScalarProprty<abcFloat4Property>;
template
class aiTScalarProprty<abcFloat4x4Property>;


// array properties

template<class T>
class aiTArrayProprty : public aiProperty
{
    using super = aiProperty;
 public:
    using property_type = T;
    using value_type = typename T::value_type;
    using sample_ptr_type = typename T::sample_ptr_type;

    aiTArrayProprty(aiSchema* schema, abcProperties cprop, const std::string& name)
        : m_schema(schema), m_abcprop(new property_type(cprop, name))
    {
        DebugLog("aeTScalarProprty::aeTScalarProprty() %s", m_abcprop->getName().c_str());
    }

    const std::string& getName() const override
    {
        return m_abcprop->getName();
    }
    aiPropertyType getPropertyType() const override
    {
        return aiGetPropertyTypeID<T>::value;
    }
    int getNumSamples() const override
    {
        return (int)m_abcprop->getNumSamples();
    }

    int getTimeSamplingIndex() const override
    {
        return m_schema->getContext()->getTimeSamplingIndex(m_abcprop->getTimeSampling());
    }

    aiPropertyData* updateSample(const abcSampleSelector& ss) override
    {
        if (m_active)
        {
            m_value = m_abcprop->getValue(ss);
            m_data = { const_cast<void*>(m_value->getData()), (int)m_value->size(), getPropertyType() };
        }
        return &m_data;
    }

    void getDataPointer(const abcSampleSelector& ss, aiPropertyData& dst) override
    {
        dst = *updateSample(ss);
    }

    void copyData(const abcSampleSelector& ss, aiPropertyData& dst) override
    {
        auto src = updateSample(ss);
        if (dst.data && src->size <= dst.size)
        {
            memcpy(dst.data, src->data, sizeof(T) * src->size);
        }
        dst.size = src->size;
        dst.type = src->type;
    }

 private:
    aiSchema* m_schema;
    std::unique_ptr<property_type> m_abcprop;
    sample_ptr_type m_value;
    aiPropertyData m_data;
};
template
class aiTArrayProprty<abcBoolArrayProperty>;
template
class aiTArrayProprty<abcIntArrayProperty>;
template
class aiTArrayProprty<abcUIntArrayProperty>;
template
class aiTArrayProprty<abcFloatArrayProperty>;
template
class aiTArrayProprty<abcFloat2ArrayProperty>;
template
class aiTArrayProprty<abcFloat3ArrayProperty>;
template
class aiTArrayProprty<abcFloat4ArrayProperty>;
template
class aiTArrayProprty<abcFloat4x4ArrayProperty>;


// aiMakeProperty() impl

template<class T, bool is_array = std::is_base_of<Abc::IArrayProperty, T>::value>
struct aiMakePropertyImpl;

template<class T>
struct aiMakePropertyImpl<T, false>
{
    static aiProperty* make(aiSchema* schema, abcProperties cp, const std::string& name)
    {
        return new aiTScalarProprty<T>(schema, cp, name);
    }
};
template<class T>
struct aiMakePropertyImpl<T, true>
{
    static aiProperty* make(aiSchema* schema, abcProperties cp, const std::string& name)
    {
        return new aiTArrayProprty<T>(schema, cp, name);
    }
};

static aiPropertyType aiGetPropertyType(const Abc::PropertyHeader& header)
{
    const auto& dt = header.getDataType();
    if (header.getPropertyType() == Abc::kScalarProperty)
    {
        if (dt.getPod() == Abc::kBooleanPOD)
        {
            switch (dt.getNumBytes())
            {
            case 1:
                return aiPropertyType::Bool;
            }
        }
        else if (dt.getPod() == Abc::kInt32POD)
        {
            switch (dt.getNumBytes())
            {
            case 4:
                return aiPropertyType::Int;
            }
        }
        else if (dt.getPod() == Abc::kUint32POD)
        {
            switch (dt.getNumBytes())
            {
            case 4:
                return aiPropertyType::UInt;
            }
        }
        else if (dt.getPod() == Abc::kFloat32POD)
        {
            switch (dt.getNumBytes())
            {
            case 4:
                return aiPropertyType::Float;
            case 8:
                return aiPropertyType::Float2;
            case 12:
                return aiPropertyType::Float3;
            case 16:
                return aiPropertyType::Float4;
            case 64:
                return aiPropertyType::Float4x4;
            }
        }
    }
    else if (header.getPropertyType() == Abc::kArrayProperty)
    {
        if (dt.getPod() == Abc::kBooleanPOD)
        {
            switch (dt.getNumBytes())
            {
            case 1:
                return aiPropertyType::BoolArray;
            }
        }
        else if (dt.getPod() == Abc::kInt32POD)
        {
            switch (dt.getNumBytes())
            {
            case 4:
                return aiPropertyType::IntArray;
            }
        }
        else if (dt.getPod() == Abc::kUint32POD)
        {
            switch (dt.getNumBytes())
            {
            case 4:
                return aiPropertyType::UIntArray;
            }
        }
        else if (dt.getPod() == Abc::kFloat32POD)
        {
            switch (dt.getNumBytes())
            {
            case 4:
                return aiPropertyType::FloatArray;
            case 8:
                return aiPropertyType::Float2Array;
            case 12:
                return aiPropertyType::Float3Array;
            case 16:
                return aiPropertyType::Float4Array;
            case 64:
                return aiPropertyType::Float4x4Array;
            }
        }
    }
    return aiPropertyType::Unknown;
}

aiProperty* aiMakeProperty(aiSchema* schema, abcProperties cprop, Abc::PropertyHeader header)
{
    aiPropertyType ptype = aiGetPropertyType(header);
    if (ptype == aiPropertyType::Unknown)
    { return nullptr; } // not supported type

    switch (ptype)
    {
    case aiPropertyType::Bool:
        return aiMakePropertyImpl<abcBoolProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Int:
        return aiMakePropertyImpl<abcIntProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::UInt:
        return aiMakePropertyImpl<abcUIntProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Float:
        return aiMakePropertyImpl<abcFloatProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Float2:
        return aiMakePropertyImpl<abcFloat2Property>::make(schema, cprop, header.getName());
    case aiPropertyType::Float3:
        return aiMakePropertyImpl<abcFloat3Property>::make(schema, cprop, header.getName());
    case aiPropertyType::Float4:
        return aiMakePropertyImpl<abcFloat4Property>::make(schema, cprop, header.getName());
    case aiPropertyType::Float4x4:
        return aiMakePropertyImpl<abcFloat4x4Property>::make(schema, cprop, header.getName());

    case aiPropertyType::BoolArray:
        return aiMakePropertyImpl<abcBoolArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::IntArray:
        return aiMakePropertyImpl<abcIntArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::UIntArray:
        return aiMakePropertyImpl<abcUIntArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::FloatArray:
        return aiMakePropertyImpl<abcFloatArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Float2Array:
        return aiMakePropertyImpl<abcFloat2ArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Float3Array:
        return aiMakePropertyImpl<abcFloat3ArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Float4Array:
        return aiMakePropertyImpl<abcFloat4ArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType::Float4x4Array:
        return aiMakePropertyImpl<abcFloat4x4ArrayProperty>::make(schema, cprop, header.getName());
    default:
        break;
    }
    return nullptr;
}
