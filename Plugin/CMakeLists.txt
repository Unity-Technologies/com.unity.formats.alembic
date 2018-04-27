CMAKE_MINIMUM_REQUIRED(VERSION 2.8)
PROJECT(AlembicForUnity)

SET(CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/cmake)
FIND_PACKAGE(OpenEXR QUIET)
FIND_PACKAGE(Alembic QUIET)
include(AddPlugin)
include(ISPC)

if(APPLE)
    set(PLATFORM "Mac")
    set(MAC ON)
elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
    set(PLATFORM "Linux")
    set(LINUX ON)
endif()

SET(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -std=c++11 -Wno-deprecated-register -Wno-return-type-c-linkage")
SET(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--no-undefined")

add_subdirectory(abci)
add_subdirectory(Test)
