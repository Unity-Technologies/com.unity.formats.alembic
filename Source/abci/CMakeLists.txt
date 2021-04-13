option(ENABLE_ISPC "Use Intel ISPC to generate SIMDified code. It can significantly boost performance." ON)

if (ENABLE_ISPC)
    find_package(ISPC REQUIRED)
    set(ISPC_OUTDIR ${CMAKE_CURRENT_BINARY_DIR}/ISPC)
    set(ISPC_HEADERS
            "${CMAKE_CURRENT_SOURCE_DIR}/Foundation/ispcmath.h"
            )
    file(GLOB ISPC_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/Foundation/*.ispc)
    add_ispc_targets(SOURCES ${ISPC_SOURCES} HEADERS ${ISPC_HEADERS} OUTDIR ${ISPC_OUTDIR})
    set(ISPC_OUTPUTS ${_ispc_outputs})
    set(ISPC_OBJECTS ${_ispc_objects})
endif ()

file(GLOB sources *.cpp *.h Foundation/*.h Foundation/*.cpp Importer/*.h Importer/*.cpp Exporter/*.h Exporter/*.cpp)
list(APPEND sources ${ISPC_SOURCES})
list(APPEND sources ${ISPC_OBJECTS})

add_plugin(abci
        SOURCES ${sources}
        PLUGINS_DIR ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic/Runtime/Plugins/x86_64
        )

target_include_directories(abci
        PRIVATE
        .
        ./Foundation
        ${OPENEXR_INCLUDE_DIR}
        ${OPENEXR_INCLUDE_DIR}/OpenEXR
        ${ALEMBIC_INCLUDE_DIR}
        )

if (ENABLE_ISPC)
    target_compile_definitions(abci PRIVATE -DaiEnableISPC)
    target_include_directories(abci PRIVATE ${ISPC_OUTDIR})
endif ()

target_link_libraries(abci
        PRIVATE
        ${ALEMBIC_LIBRARY}
        ${OPENEXR_Half_LIBRARY}
        ${OPENEXR_Iex_LIBRARY}
        ${OPENEXR_IexMath_LIBRARY}
        )

install(TARGETS abci DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic/Runtime/Plugins/x86_64)

########################################
# Unit tests
#add_subdirectory(googletest)
#include_directories(./googletest/googletest/include)
#enable_testing()
#add_subdirectory(test)
