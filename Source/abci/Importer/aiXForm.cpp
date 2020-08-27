#include "pch.h"
#include "aiInternal.h"
#include "aiContext.h"
#include "aiObject.h"
#include "aiSchema.h"
#include "aiXForm.h"


aiXformSample::aiXformSample(aiXform *schema)
    : super(schema)
{
}

void aiXformSample::getData(aiXformData &dst) const
{
    dst = data;
}

aiXform::aiXform(aiObject *parent, const abcObject &abc)
    : super(parent, abc)
{
}

aiXform::Sample* aiXform::newSample()
{
    return new Sample(this);
}

void aiXform::readSampleBody(Sample& sample, uint64_t idx)
{
    auto ss = aiIndexToSampleSelector(idx);
    auto ss2 = aiIndexToSampleSelector(idx + 1);
    
    m_schema.get(sample.xf_sp, ss);
    m_schema.get(sample.xf_sp2, ss2);
}

void aiXform::cookSampleBody(Sample& sample)
{
    auto& config = getConfig();

    Imath::V3d scale;
    Imath::V3d shear;
    Imath::Quatd rot;
    Imath::V3d trans;
    decompose(sample.xf_sp.getMatrix(), scale, shear, rot, trans);

    if (config.interpolate_samples && m_current_time_offset != 0)
    {
        Imath::V3d scale2;
        Imath::Quatd rot2;
        Imath::V3d trans2;
        decompose(sample.xf_sp2.getMatrix(), scale2, shear, rot2, trans2);
        scale += (scale2 - scale) * m_current_time_offset;
        trans += (trans2 - trans) * m_current_time_offset;
        rot = Imath::slerpShortestArc(rot, rot2, (double)m_current_time_offset);
    }

    auto rot_final = abcV4(
        static_cast<float>(rot.v[0]),
        static_cast<float>(rot.v[1]),
        static_cast<float>(rot.v[2]),
        static_cast<float>(rot.r)
    );

    if (config.swap_handedness)
    {
        trans.x *= -1.0f;
        rot_final.x = -rot_final.x;
        rot_final.w = -rot_final.w;
    }
    auto& dst = sample.data;
    dst.visibility = sample.visibility;
    dst.inherits = sample.xf_sp.getInheritsXforms();
    dst.translation = trans;
    dst.rotation = rot_final;
    dst.scale = scale;
}

void aiXform::decompose(const Imath::M44d &mat, Imath::V3d &scale, Imath::V3d &shear, Imath::Quatd &rotation, Imath::V3d &translation) const
{
    Imath::M44d mat_remainder(mat);

    // Extract Scale, Shear
    Imath::extractAndRemoveScalingAndShear(mat_remainder, scale, shear, false);

    // Extract translation
    translation.x = mat_remainder[3][0];
    translation.y = mat_remainder[3][1];
    translation.z = mat_remainder[3][2];
    translation *= getConfig().scale_factor;

    // Extract rotation
    rotation = extractQuat(mat_remainder);
}
