file(GLOB sources *.cpp *.h Foundation/*.h Foundation/*.cpp Importer/*.h Importer/*.cpp Exporter/*.h Exporter/*.cpp)
set(plugins_dir "${CMAKE_SOURCE_DIR}/../AlembicImporter/Assets/UTJ/Alembic/Plugins/x86_64")
add_plugin(abci SOURCES ${sources} PLUGINS_DIR ${plugins_dir})

target_include_directories(abci PUBLIC . ./Foundation ${OPENEXR_INCLUDE_DIR} ${OPENEXR_INCLUDE_DIR}/OpenEXR)
target_link_libraries(abci
    ${OPENEXR_Half_LIBRARY}
    ${OPENEXR_Iex_LIBRARY}
    ${OPENEXR_IexMath_LIBRARY}
    ${HDF5_C_LIBRARIES}
    ${HDF5_HL_LIBRARIES}
    ${ALEMBIC_LIBRARY}
)
install(TARGETS abci DESTINATION .)
