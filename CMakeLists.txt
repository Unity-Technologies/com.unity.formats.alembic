CMAKE_MINIMUM_REQUIRED(VERSION 3.4)
PROJECT(AlembicForUnity LANGUAGES C CXX)

set(CMAKE_VERBOSE_MAKEFILE ON)

# Get git tag as package version
find_package(Git REQUIRED)
execute_process(COMMAND ${GIT_EXECUTABLE} describe --tags HEAD
        OUTPUT_VARIABLE PACKAGE_VERSION
        OUTPUT_STRIP_TRAILING_WHITESPACE)
# only keep x.y.z[-preview.n]
string(REGEX MATCH "[0-9]\.[0-9]\.[0-9].*" PACKAGE_VERSION "${PACKAGE_VERSION}")

# Get git revision hash as package revision
execute_process(COMMAND ${GIT_EXECUTABLE} rev-parse HEAD
        OUTPUT_VARIABLE PACKAGE_REVISION
        OUTPUT_STRIP_TRAILING_WHITESPACE)

configure_file(${CMAKE_SOURCE_DIR}/proto.com.unity.formats.alembic/package.json.in ${CMAKE_BINARY_DIR}/package.json @ONLY)

add_subdirectory("Source/")

# create package
install(DIRECTORY proto.com.unity.formats.alembic/ DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic PATTERN *.in EXCLUDE)
install(FILES ${CMAKE_BINARY_DIR}/package.json DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic)
install(FILES
        ${CMAKE_SOURCE_DIR}/CHANGELOG.md
        ${CMAKE_SOURCE_DIR}/LICENSE.md
        "${CMAKE_SOURCE_DIR}/Third Party Notices.md"
        DESTINATION ${CMAKE_INSTALL_PREFIX}/com.unity.formats.alembic)
