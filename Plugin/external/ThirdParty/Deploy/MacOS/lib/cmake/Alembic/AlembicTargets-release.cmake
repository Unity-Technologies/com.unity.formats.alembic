#----------------------------------------------------------------
# Generated CMake target import file for configuration "Release".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "Alembic::Alembic" for configuration "Release"
set_property(TARGET Alembic::Alembic APPEND PROPERTY IMPORTED_CONFIGURATIONS RELEASE)
set_target_properties(Alembic::Alembic PROPERTIES
  IMPORTED_LINK_INTERFACE_LANGUAGES_RELEASE "CXX"
  IMPORTED_LINK_INTERFACE_LIBRARIES_RELEASE "/Users/inwoods/Development/alembic-importer-build/build/lib/libImath.a;/Users/inwoods/Development/alembic-importer-build/build/lib/libIlmThread.a;/Users/inwoods/Development/alembic-importer-build/build/lib/libIex.a;/Users/inwoods/Development/alembic-importer-build/build/lib/libIexMath.a;/Users/inwoods/Development/alembic-importer-build/build/lib/libHalf.a;-lm;-ldl;-lpthread;/Users/inwoods/Development/alembic-importer-build/build/lib/libhdf5.a"
  IMPORTED_LOCATION_RELEASE "${_IMPORT_PREFIX}/lib/libAlembic.a"
  )

list(APPEND _IMPORT_CHECK_TARGETS Alembic::Alembic )
list(APPEND _IMPORT_CHECK_FILES_FOR_Alembic::Alembic "${_IMPORT_PREFIX}/lib/libAlembic.a" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
