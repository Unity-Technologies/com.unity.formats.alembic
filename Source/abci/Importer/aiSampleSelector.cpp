#include "aiSampleSelector.h"

abcSampleSelector aiTimeToSampleSelector(double time)
{
    return abcSampleSelector(time, abcSampleSelector::kFloorIndex);
}

abcSampleSelector aiIndexToSampleSelector(int64_t index)
{
    return abcSampleSelector(index);
}
