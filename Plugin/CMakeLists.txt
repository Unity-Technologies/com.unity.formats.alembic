CMAKE_MINIMUM_REQUIRED(VERSION 3.4)
PROJECT(AlembicPlugins)

OPTION (BUILD_SHARED_LIBS "Build Shared Libraries" ON)
OPTION (USE_GRAPHICS_DEVICE "Use Graphics Devices (Required by write-data-to-texture feature)" ON)
OPTION (USE_EXTERNALS "Use Externals (Add externals/alembic etc to include & library path)" ON)

IF(MSVC)
    SET(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
    SET(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MT")
ENDIF()

SET( INCLUDES
    .
    Foundation
)
IF(USE_EXTERNALS)
    SET (INCLUDES
        ${INCLUDES}
        external/glew-1.13.0/include
        external/alembic-1_05_08/lib
        external/ilmbase-2.2.0/Imath
        external/ilmbase-2.2.0/IlmThread
        external/ilmbase-2.2.0/IexMath
        external/ilmbase-2.2.0/Iex
        external/ilmbase-2.2.0/Half
        external/ilmbase-2.2.0/config
        external/hdf5-1.8.14/hl/src
        external/hdf5-1.8.14/src
        external/hdf5-1.8.14
    )
ENDIF()
INCLUDE_DIRECTORIES(${INCLUDES})

IF(USE_EXTERNALS)
    LINK_DIRECTORIES (
        ${CMAKE_SOURCE_DIR}/external/libs/x86_64
    )
ENDIF()

FILE(GLOB EXPORTER_CXX_FILES Exporter/*.cpp)
FILE(GLOB EXPORTER_H_FILES Exporter/*.h)

FILE(GLOB IMPORTER_CXX_FILES Importer/*.cpp)
FILE(GLOB IMPORTER_H_FILES Importer/*.h)

FILE(GLOB FOUNDATION_CXX_FILES Foundation/*.cpp)
FILE(GLOB FOUNDATION_H_FILES Foundation/*.h)

SET( GRAPHICS_CXX_FILES
    GraphicsDevice/aiGraphicsDevice.cpp
    GraphicsDevice/aiGraphicsDeviceOpenGL.cpp
)
SET( GRAPHICS_H_FILES
    GraphicsDevice/aiGraphicsDevice.h
)
IF(WIN32)
    SET( GRAPHICS_CXX_FILES
      ${GRAPHICS_CXX_FILES}
      GraphicsDevice/aiGraphicsDeviceD3D11.cpp
    )
ENDIF()


SET( EXPORTER_SOURCE_FILES
    pch.cpp pch.h
    ${EXPORTER_CXX_FILES} ${EXPORTER_H_FILES}
)
ADD_LIBRARY( AlembicExporter ${EXPORTER_SOURCE_FILES} )

SET( IMPORTER_SOURCE_FILES
    pch.cpp pch.h
    ${IMPORTER_CXX_FILES} ${IMPORTER_H_FILES}
    ${FOUNDATION_CXX_FILES} ${FOUNDATION_H_FILES}
)
IF(USE_GRAPHICS_DEVICE)
    SET( IMPORTER_SOURCE_FILES
        ${IMPORTER_SOURCE_FILES}
        ${GRAPHICS_CXX_FILES} ${GRAPHICS_H_FILES}
    )
ENDIF()

ADD_LIBRARY( AlembicImporter ${IMPORTER_SOURCE_FILES} )
