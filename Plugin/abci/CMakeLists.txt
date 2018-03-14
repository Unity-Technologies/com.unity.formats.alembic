option(ENABLE_ISPC "Use Intel ISPC to generate SIMDified code. It can significantly boost performance." ON)

if(ENABLE_ISPC)
    FIND_PACKAGE(ISPC REQUIRED)

    set(ISPC_OUTDIR ${CMAKE_CURRENT_BINARY_DIR}/ISPC)
    set(ISPC_HEADERS
        "${CMAKE_CURRENT_SOURCE_DIR}/Foundation/ispcmath.h"
    )
    file(GLOB ISPC_SOURCES Foundation/*.ispc)
    add_ispc_targets(SOURCES ${ISPC_SOURCES} HEADERS ${ISPC_HEADERS} OUTDIR ${ISPC_OUTDIR})
    set(ISPC_OUTPUTS ${_ispc_outputs})
endif()

file(GLOB sources *.cpp *.h Foundation/*.h Foundation/*.cpp Importer/*.h Importer/*.cpp Exporter/*.h Exporter/*.cpp ${ISPC_OUTPUTS})
set(plugins_dir "${CMAKE_SOURCE_DIR}/../AlembicImporter/UnityPackageManager/com.unity.alembic/Runtime/Plugins/x86_64")
add_plugin(abci SOURCES ${sources} PLUGINS_DIR ${plugins_dir})

target_include_directories(abci PUBLIC
    .
    ./Foundation
    ${OPENEXR_INCLUDE_DIR}
    ${OPENEXR_INCLUDE_DIR}/OpenEXR
    ${ALEMBIC_INCLUDE_DIR}
)

target_link_libraries(abci
    ${ALEMBIC_LIBRARY}
    ${OPENEXR_Half_LIBRARY}
    ${OPENEXR_Iex_LIBRARY}
    ${OPENEXR_IexMath_LIBRARY}
    ${HDF5_LIBRARIES}
)
install(TARGETS abci DESTINATION .)

if(ENABLE_ISPC)
    add_definitions(-DaiEnableISPC)
    target_include_directories(abci PUBLIC ${ISPC_OUTDIR})
endif()

########################################
# Unit tests
add_subdirectory(googletest)
include_directories(./googletest/googletest/include)
enable_testing()
add_subdirectory(test)
