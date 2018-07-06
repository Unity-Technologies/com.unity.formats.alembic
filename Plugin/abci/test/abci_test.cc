#include "gtest/gtest.h"
#include "../abci.h"

TEST(AllTests, Basic) {
    aiContext *ctx = aiCreateContext(5);
    ASSERT_NE(nullptr, ctx);

    ASSERT_TRUE(aiLoad(ctx, "cube.abc"));
    EXPECT_EQ(12, getFrameCount(ctx));

    // Clean up... and make sure there's no crash.
    aiDestroyContext(ctx);
}
