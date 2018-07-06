CMAKE_MINIMUM_REQUIRED(VERSION 3.5)
PROJECT(AlembicForUnity VERSION 0.1 LANGUAGES C CXX)

add_subdirectory("Source/")

# create package
install(DIRECTORY proto.com.unity.formats.alembic/ DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic)

#
