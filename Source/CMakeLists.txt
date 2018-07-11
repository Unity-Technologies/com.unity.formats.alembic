enable_testing()

OPTION(USE_STATIC "Link with static libraries" ON)

SET(CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/cmake)
FIND_PACKAGE(OpenEXR REQUIRED)
FIND_PACKAGE(HDF5 REQUIRED)
FIND_PACKAGE(Alembic REQUIRED)
include(AddPlugin)

if(WIN32)
    set(HDF5_LIBRARIES "${HDF5_LIBRARIES};${HDF5_ROOT}/lib/libszip.lib;${HDF5_ROOT}/lib/zlibstatic.lib")
else()
    set(HDF5_LIBRARIES "${HDF5_LIBRARIES};-ldl;-lpthread")
endif()

if(APPLE OR UNIX)
SET(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -std=c++11 -Wno-deprecated-register -Wno-return-type-c-linkage")
SET(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--no-undefined")
elseif(WIN32)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
endif()

add_subdirectory(abci)
add_subdirectory(Test)
