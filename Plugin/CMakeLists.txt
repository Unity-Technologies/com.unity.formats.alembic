cmake_minimum_required(VERSION 2.8)
project(abci)

set(CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/cmake)
include(ISPC)
include(AddPlugin)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
include_directories(${CMAKE_SOURCE_DIR})

add_subdirectory(MeshUtils)
add_subdirectory(abci)
add_subdirectory(Test)
