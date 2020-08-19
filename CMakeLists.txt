CMAKE_MINIMUM_REQUIRED(VERSION 3.4)
PROJECT(AlembicForUnity LANGUAGES C CXX)

set(CMAKE_VERBOSE_MAKEFILE ON)

add_subdirectory("Source/")

# create package
install(DIRECTORY com.unity.formats.alembic/ DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic PATTERN *.in EXCLUDE)
install(FILES
        DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic)
