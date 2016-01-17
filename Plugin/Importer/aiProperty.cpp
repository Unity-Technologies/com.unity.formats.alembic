#include "pch.h"
#include "AlembicImporter.h"
#include "aiLogger.h"
#include "aiContext.h"
#include "aiProperty.h"
#include "aiObject.h"
#include "aiSchema.h"


aiProperty::aiProperty()
    : m_active(false)
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
template<class T> struct aiGetPropertyTypeID { static const aiPropertyType value = aiPropertyType_Unknown; };

template<> struct aiGetPropertyTypeID<abcBoolProperty> { static const aiPropertyType value = aiPropertyType_Bool; };
template<> struct aiGetPropertyTypeID<abcIntProperty> { static const aiPropertyType value = aiPropertyType_Int; };
template<> struct aiGetPropertyTypeID<abcUIntProperty> { static const aiPropertyType value = aiPropertyType_UInt; };
template<> struct aiGetPropertyTypeID<abcFloatProperty> { static const aiPropertyType value = aiPropertyType_Float; };
template<> struct aiGetPropertyTypeID<abcFloat2Property> { static const aiPropertyType value = aiPropertyType_Float2; };
template<> struct aiGetPropertyTypeID<abcFloat3Property> { static const aiPropertyType value = aiPropertyType_Float3; };
template<> struct aiGetPropertyTypeID<abcFloat4Property> { static const aiPropertyType value = aiPropertyType_Float4; };
template<> struct aiGetPropertyTypeID<abcFloat4x4Property> { static const aiPropertyType value = aiPropertyType_Float4x4; };

template<> struct aiGetPropertyTypeID<abcBoolArrayProperty> { static const aiPropertyType value = aiPropertyType_BoolArray; };
template<> struct aiGetPropertyTypeID<abcIntArrayProperty> { static const aiPropertyType value = aiPropertyType_IntArray; };
template<> struct aiGetPropertyTypeID<abcUIntArrayProperty> { static const aiPropertyType value = aiPropertyType_UIntArray; };
template<> struct aiGetPropertyTypeID<abcFloatArrayProperty> { static const aiPropertyType value = aiPropertyType_FloatArray; };
template<> struct aiGetPropertyTypeID<abcFloat2ArrayProperty> { static const aiPropertyType value = aiPropertyType_Float2Array; };
template<> struct aiGetPropertyTypeID<abcFloat3ArrayProperty> { static const aiPropertyType value = aiPropertyType_Float3Array; };
template<> struct aiGetPropertyTypeID<abcFloat4ArrayProperty> { static const aiPropertyType value = aiPropertyType_Float4Array; };
template<> struct aiGetPropertyTypeID<abcFloat4x4ArrayProperty> { static const aiPropertyType value = aiPropertyType_Float4x4Array; };


// scalar properties

template<class T>
class aiTScalarProprty : public aiProperty
{
    typedef aiProperty super;
public:
    typedef T property_type;
    typedef typename T::value_type value_type;

    aiTScalarProprty(aiSchemaBase *schema, abcProperties cprop, const std::string &name)
        : m_schema(schema), m_abcprop(new property_type(cprop, name))
    {
        aeDebugLog("aeTScalarProprty::aeTScalarProprty() %s", m_abcprop->getName().c_str());
    }
    const std::string& getName() const override { return m_abcprop->getName(); }
    aiPropertyType getPropertyType() const override { return aiGetPropertyTypeID<T>::value; }
    int getNumSamples() const override { return (int)m_abcprop->getNumSamples(); }

    int getTimeSamplingIndex() const override
    {
        return m_schema->getObject()->getContext()->getTimeSamplingIndex(m_abcprop->getTimeSampling());
    }

    aiPropertyData* updateSample(const abcSampleSelector& ss) override
    {
        if (m_active) {
            m_value = m_abcprop->getValue(ss);
            m_data = aiPropertyData(getPropertyType(), (T*)&m_value, 1);
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
        if (dst.data) {
            memcpy(dst.data, src->data, sizeof(T)*src->size);
        }
        dst.size = src->size;
        dst.type = src->type;
    }

private:
    aiSchemaBase *m_schema;
    std::unique_ptr<property_type> m_abcprop;
    value_type m_value;
    aiPropertyData m_data;
};
template class aiTScalarProprty<abcBoolProperty>;
template class aiTScalarProprty<abcIntProperty>;
template class aiTScalarProprty<abcUIntProperty>;
template class aiTScalarProprty<abcFloatProperty>;
template class aiTScalarProprty<abcFloat2Property>;
template class aiTScalarProprty<abcFloat3Property>;
template class aiTScalarProprty<abcFloat4Property>;
template class aiTScalarProprty<abcFloat4x4Property>;


// array properties

template<class T>
class aiTArrayProprty : public aiProperty
{
    typedef aiProperty super;
public:
    typedef T property_type;
    typedef typename T::value_type value_type;
    typedef typename T::sample_ptr_type sample_ptr_type;

    aiTArrayProprty(aiSchemaBase *schema, abcProperties cprop, const std::string &name)
        : m_schema(schema), m_abcprop(new property_type(cprop, name))
    {
        aeDebugLog("aeTScalarProprty::aeTScalarProprty() %s", m_abcprop->getName().c_str());
    }
    const std::string& getName() const override { return m_abcprop->getName(); }
    aiPropertyType getPropertyType() const override { return aiGetPropertyTypeID<T>::value; }
    int getNumSamples() const override { return (int)m_abcprop->getNumSamples(); }

    int getTimeSamplingIndex() const override
    {
        return m_schema->getObject()->getContext()->getTimeSamplingIndex(m_abcprop->getTimeSampling());
    }

    aiPropertyData* updateSample(const abcSampleSelector& ss) override
    {
        if (m_active) {
            m_value = m_abcprop->getValue(ss);
            m_data = aiPropertyData(getPropertyType(), (T*)m_value->getData(), m_value->size());
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
        if (dst.data && src->size <= dst.size) {
            memcpy(dst.data, src->data, sizeof(T)*src->size);
        }
        dst.size = src->size;
        dst.type = src->type;
    }

private:
    aiSchemaBase *m_schema;
    std::unique_ptr<property_type> m_abcprop;
    sample_ptr_type m_value;
    aiPropertyData m_data;
};
template class aiTArrayProprty<abcBoolArrayProperty>;
template class aiTArrayProprty<abcIntArrayProperty>;
template class aiTArrayProprty<abcUIntArrayProperty>;
template class aiTArrayProprty<abcFloatArrayProperty>;
template class aiTArrayProprty<abcFloat2ArrayProperty>;
template class aiTArrayProprty<abcFloat3ArrayProperty>;
template class aiTArrayProprty<abcFloat4ArrayProperty>;
template class aiTArrayProprty<abcFloat4x4ArrayProperty>;


// aiMakeProperty() impl

template<class T, bool is_array = std::is_base_of<Abc::IArrayProperty, T>::value>
struct aiMakePropertyImpl;

template<class T>
struct aiMakePropertyImpl<T, false>
{
    static aiProperty* make(aiSchemaBase *schema, abcProperties cp, const std::string &name)
    {
        return new aiTScalarProprty<T>(schema, cp, name);
    }
};
template<class T>
struct aiMakePropertyImpl<T, true>
{
    static aiProperty* make(aiSchemaBase *schema, abcProperties cp, const std::string &name)
    {
        return new aiTArrayProprty<T>(schema, cp, name);
    }
};

static aiPropertyType aiGetPropertyType(const Abc::PropertyHeader& header)
{
    const auto &dt = header.getDataType();
    if (header.getPropertyType() == Abc::kScalarProperty) {
        if (dt.getPod() == Abc::kBooleanPOD) {
            switch (dt.getNumBytes()) {
            case 1: return aiPropertyType_Bool;
            }
        }
        else if (dt.getPod() == Abc::kInt32POD) {
            switch (dt.getNumBytes()) {
            case 4: return aiPropertyType_Int;
            }
        }
        else if (dt.getPod() == Abc::kUint32POD) {
            switch (dt.getNumBytes()) {
            case 4: return aiPropertyType_UInt;
            }
        }
        else if (dt.getPod() == Abc::kFloat32POD) {
            switch (dt.getNumBytes()) {
            case 4: return aiPropertyType_Float;
            case 8: return aiPropertyType_Float2;
            case 12: return aiPropertyType_Float3;
            case 16: return aiPropertyType_Float4;
            case 64: return aiPropertyType_Float4x4;
            }
        }
    }
    else if (header.getPropertyType() == Abc::kArrayProperty) {
        if (dt.getPod() == Abc::kBooleanPOD) {
            switch (dt.getNumBytes()) {
            case 1: return aiPropertyType_BoolArray;
            }
        }
        else if (dt.getPod() == Abc::kInt32POD) {
            switch (dt.getNumBytes()) {
            case 4: return aiPropertyType_IntArray;
            }
        }
        else if (dt.getPod() == Abc::kUint32POD) {
            switch (dt.getNumBytes()) {
            case 4: return aiPropertyType_UIntArray;
            }
        }
        else if (dt.getPod() == Abc::kFloat32POD) {
            switch (dt.getNumBytes()) {
            case 4: return aiPropertyType_FloatArray;
            case 8: return aiPropertyType_Float2Array;
            case 12: return aiPropertyType_Float3Array;
            case 16: return aiPropertyType_Float4Array;
            case 64: return aiPropertyType_Float4x4Array;
            }
        }
    }
    return aiPropertyType_Unknown;
}

aiProperty* aiMakeProperty(aiSchemaBase *schema, abcProperties cprop, Abc::PropertyHeader header)
{
    aiPropertyType ptype = aiGetPropertyType(header);
    if (ptype == aiPropertyType_Unknown) { return nullptr; } // not supported type

    switch (ptype)
    {
    case aiPropertyType_Bool: return aiMakePropertyImpl<abcBoolProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Int: return aiMakePropertyImpl<abcIntProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_UInt: return aiMakePropertyImpl<abcUIntProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Float: return aiMakePropertyImpl<abcFloatProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Float2: return aiMakePropertyImpl<abcFloat2Property>::make(schema, cprop, header.getName());
    case aiPropertyType_Float3: return aiMakePropertyImpl<abcFloat3Property>::make(schema, cprop, header.getName());
    case aiPropertyType_Float4: return aiMakePropertyImpl<abcFloat4Property>::make(schema, cprop, header.getName());
    case aiPropertyType_Float4x4: return aiMakePropertyImpl<abcFloat4x4Property>::make(schema, cprop, header.getName());

    case aiPropertyType_BoolArray: return aiMakePropertyImpl<abcBoolArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_IntArray: return aiMakePropertyImpl<abcIntArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_UIntArray: return aiMakePropertyImpl<abcUIntArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_FloatArray: return aiMakePropertyImpl<abcFloatArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Float2Array: return aiMakePropertyImpl<abcFloat2ArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Float3Array: return aiMakePropertyImpl<abcFloat3ArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Float4Array: return aiMakePropertyImpl<abcFloat4ArrayProperty>::make(schema, cprop, header.getName());
    case aiPropertyType_Float4x4Array: return aiMakePropertyImpl<abcFloat4x4ArrayProperty>::make(schema, cprop, header.getName());
    }
    return nullptr;
}
