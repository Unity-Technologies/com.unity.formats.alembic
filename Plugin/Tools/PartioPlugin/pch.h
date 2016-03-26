#include <cstdint>
#include <cmath>
#include <algorithm>
#include <map>
#include <set>
#include <vector>
#include <deque>
#include <memory>
#include <thread>
#include <mutex>
#include <future>
#include <functional>
#include <limits>
#include <random>
#include <chrono>
#include <sstream>
#include <type_traits>
#include <Partio.h>
#include <OpenExr/ImathVec.h>

#ifdef _WIN32
#include <windows.h>
#ifdef max
    #undef max
    #undef min
#endif
#endif
#pragma warning(disable: 4996)

typedef Imath::V2f ppV2;
typedef Imath::V3f ppV3;
typedef std::future<int> ppIOAsync;
