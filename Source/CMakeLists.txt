enable_testing()

OPTION(USE_STATIC "Link with static libraries" ON)

cmake_policy(SET CMP0074 NEW)
set(CMAKE_CXX_STANDARD 11)

SET(CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/cmake)
FIND_PACKAGE(OpenEXR REQUIRED)
FIND_PACKAGE(HDF5 REQUIRED)
FIND_PACKAGE(Alembic REQUIRED)
include(AddPlugin)

if(WIN32)
    set(HDF5_LIBRARIES "${HDF5_LIBRARIES};${HDF5_ROOT}/lib/szip.lib;${HDF5_ROOT}/lib/zlib.lib")
elseif(APPLE)
    set(HDF5_LIBRARIES "${HDF5_LIBRARIES};-ldl;-lpthread;${HDF5_ROOT}/lib/libz.a;${HDF5_ROOT}/lib/libszip.a")
else()
    set(HDF5_LIBRARIES "${HDF5_LIBRARIES};-ldl;-lpthread;${HDF5_ROOT}/lib/libz.a;${HDF5_ROOT}/lib/libszip.a")
endif()

if(APPLE OR UNIX)
    SET(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
    SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -std=c++11 -Wno-deprecated-register -Wno-return-type-c-linkage")
    SET(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--no-undefined")
endif()

add_subdirectory(abci)
#add_subdirectory(Test)
