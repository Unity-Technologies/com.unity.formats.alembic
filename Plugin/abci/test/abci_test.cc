#include "gtest/gtest.h"
#include <math.h>

TEST(AllTests, BasicTest) {
    EXPECT_EQ(1.0, fabs(-1.0));
}

#if 0
int main(int argc, char **argv)
{
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}
#endif
