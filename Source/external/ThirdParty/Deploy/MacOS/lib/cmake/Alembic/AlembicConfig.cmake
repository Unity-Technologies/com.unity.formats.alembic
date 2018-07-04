INCLUDE("${CMAKE_CURRENT_LIST_DIR}/AlembicTargets.cmake")

SET(Alembic_HAS_HDF5 ON)
SET(Alembic_HAS_SHARED_LIBS OFF)

SET(Alembic_USES_BOOST OFF)
if(OFF AND NOT OFF)
    SET(Alembic_USES_TR1 TRUE)
else()
    SET(Alembic_USES_TR1 FALSE)
endif()
