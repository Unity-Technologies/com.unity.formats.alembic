CMAKE_MINIMUM_REQUIRED(VERSION 2.6)
PROJECT(AlembicPlugins)

OPTION (BUILD_SHARED_LIBS "Build Shared Libraries" ON)
OPTION (WITH_GRAPHICS_DEVICE "With Graphics Devices (Required by write-data-to-texture feature)" ON)

IF(MSVC)
    SET(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} /MT")
    SET(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} /MT")
ENDIF()

INCLUDE_DIRECTORIES (
    .
    Foundation
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

LINK_DIRECTORIES (
    ${CMAKE_SOURCE_DIR}/external/libs/x86_64
)


SET( EXPORTER_CXX_FILES
    pch.cpp
    Exporter/aeCamera.cpp
    Exporter/aeContext.cpp
    Exporter/aeObject.cpp
    Exporter/aePoints.cpp
    Exporter/aePolyMesh.cpp
    Exporter/aeXForm.cpp
    Exporter/AlembicExporter.cpp
)
SET( EXPORTER_H_FILES
    pch.h
    Exporter/aeCamera.h
    Exporter/aeContext.h
    Exporter/aeObject.h
    Exporter/aePoints.h
    Exporter/aePolyMesh.h
    Exporter/aeXForm.h
    Exporter/AlembicExporter.h
)

SET( IMPORTER_CXX_FILES
    pch.cpp
    Importer/aiCamera.cpp
    Importer/aiContext.cpp
    Importer/aiObject.cpp
    Importer/aiPoints.cpp
    Importer/aiPolyMesh.cpp
    Importer/aiSchema.cpp
    Importer/aiXForm.cpp
    Importer/AlembicImporter.cpp
)
SET( IMPORTER_H_FILES
    pch.h
    Importer/aiCamera.h
    Importer/aiContext.h
    Importer/aiObject.h
    Importer/aiPoints.h
    Importer/aiPolyMesh.h
    Importer/aiSchema.h
    Importer/aiXForm.h
    Importer/AlembicImporter.h
)

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

SET( FOUNDATION_CXX_FILES
  Foundation/aiLogger.cpp
  Foundation/aiThreadPool.cpp
)
SET( FOUNDATION_H_FILES
  Foundation/aiLogger.h
  Foundation/aiThreadPool.h
)


SET( EXPORTER_SOURCE_FILES
    ${EXPORTER_CXX_FILES} ${EXPORTER_H_FILES}
)
ADD_LIBRARY( AlembicExporter ${EXPORTER_SOURCE_FILES} )

SET( IMPORTER_SOURCE_FILES
    ${IMPORTER_CXX_FILES} ${IMPORTER_H_FILES}
    ${FOUNDATION_CXX_FILES} ${FOUNDATION_H_FILES}
)
IF(WITH_GRAPHICS_DEVICE)
    SET( IMPORTER_SOURCE_FILES
        ${IMPORTER_SOURCE_FILES}
        ${GRAPHICS_CXX_FILES} ${GRAPHICS_H_FILES}
    )
ENDIF()

ADD_LIBRARY( AlembicImporter ${IMPORTER_SOURCE_FILES} )
