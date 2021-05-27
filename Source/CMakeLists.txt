enable_testing()

OPTION(USE_STATIC "Link with static libraries" ON)

cmake_policy(SET CMP0074 NEW)

SET(CMAKE_MODULE_PATH ${CMAKE_SOURCE_DIR}/cmake)
FIND_PACKAGE(OpenEXR REQUIRED)
FIND_PACKAGE(Alembic REQUIRED)
include(AddPlugin)
set(CMAKE_CXX_STANDARD 14)

if(APPLE OR UNIX)
    SET(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
    SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -Wno-deprecated-register -Wno-return-type-c-linkage")
    SET(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Wl,--no-undefined")
elseif(WIN32)
    set(CMAKE_CXX_FLAGS ${CMAKE_CXX_FLAGS})
endif()

add_subdirectory(abci)
#add_subdirectory(Test)
