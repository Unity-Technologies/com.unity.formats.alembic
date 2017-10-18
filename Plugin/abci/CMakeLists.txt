find_package(OpenEXR QUIET)
find_package(HDF5 QUIET)
find_package(Alembic QUIET)

file(GLOB ABCI_SOURCES Importer/*.cpp Exporter/*.cpp Foundation/*.cpp)
file(GLOB ABCI_HEADERS Importer/*.h Exporter/*.h Foundation/*.h)
set(PLUGINS_DIR "${CMAKE_SOURCE_DIR}/../AlembicImporter/Assets/UTJ/Alembic/Plugins/x86_64")
add_plugin(abci SOURCES ${ABCI_SOURCES} ${ABCI_HEADERS} PLUGINS_DIR ${PLUGINS_DIR})

target_include_directories(abci
    PUBLIC ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Foundation
    PRIVATE ${OPENEXR_INCLUDE_DIRS} ${ALEMBIC_INCLUDE_DIR}
)
target_link_libraries(abci
    ${OPENEXR_Half_LIBRARY}
    ${OPENEXR_Iex_LIBRARY}
    ${OPENEXR_IexMath_LIBRARY}
    ${HDF5_C_LIBRARIES}
    ${HDF5_HL_LIBRARIES}
    ${ALEMBIC_LIBRARY}
)
