CMAKE_MINIMUM_REQUIRED(VERSION 3.4)
PROJECT(AlembicForUnity VERSION 0.1 LANGUAGES C CXX)

set(CMAKE_VERBOSE_MAKEFILE ON)

add_subdirectory("Source/")

# create package
install(DIRECTORY proto.com.unity.formats.alembic/ DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic)
install(FILES
        ${CMAKE_SOURCE_DIR}/CHANGELOG.md
        ${CMAKE_SOURCE_DIR}/LICENSE.md
        "${CMAKE_SOURCE_DIR}/Third Party Notices.md"
        DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic)
