set(CMAKE_PREFIX_PATH
    "${ALEMBIC_DIR}"
    "${HDF5_DIR}"
    "/usr/local"
    "/usr/local/HDF_Group/HDF5/1.10.2"
)

find_path(ALEMBIC_INCLUDE_DIR Alembic/AbcGeom/All.h)
mark_as_advanced(ALEMBIC_INCLUDE_DIR)

foreach(lib Alembic hdf5 szip z)
    find_library(ALEMBIC_${lib}_LIBRARY NAMES lib${lib}.a ${lib})
    mark_as_advanced(ALEMBIC_${lib}_LIBRARY)
    if(ALEMBIC_${lib}_LIBRARY)
        list(APPEND ALEMBIC_LIBRARIES ${ALEMBIC_${lib}_LIBRARY})
    endif()
endforeach()

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args("Alembic"
    DEFAULT_MSG
    ALEMBIC_INCLUDE_DIR
    ALEMBIC_LIBRARIES
)
