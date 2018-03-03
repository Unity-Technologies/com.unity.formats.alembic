#include "gtest/gtest.h"
#include "../abci.h"
#include <stdio.h>
#include <unistd.h>

TEST(AllTests, Basic) {
    aiContext *ctx = aiContextCreate(5);
    ASSERT_NE(nullptr, ctx);

    ASSERT_TRUE(aiContextLoad(ctx, "cube.abc"));

    // Clean up... and make sure there's no crash.
    aiContextDestroy(ctx);
}
