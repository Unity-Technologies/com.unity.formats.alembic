set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC")

if (NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE "Release" CACHE PATH "" FORCE)
endif()
if (CMAKE_INSTALL_PREFIX_INITIALIZED_TO_DEFAULT)
    set(CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/dist" CACHE PATH "" FORCE)
endif()

if(APPLE)
    set(CMAKE_FIND_LIBRARY_SUFFIXES ".a")
    option(ENABLE_OSX_BUNDLE "Build bundle." ON)
    set(CMAKE_MACOSX_RPATH ON)

    if(ENABLE_OSX_BUNDLE)
        set(CMAKE_SKIP_RPATH ON)
    else()
        set(CMAKE_SKIP_RPATH OFF)
        set(CMAKE_INSTALL_RPATH_USE_LINK_PATH ON)
    endif()
elseif(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    option(ENABLE_LINUX_USE_LINK_PATH "" ON)

    if(ENABLE_LINUX_USE_LINK_PATH)
        set(CMAKE_INSTALL_RPATH_USE_LINK_PATH ON)
    endif()
endif()
option(ENABLE_DEPLOY "Copy built binaries to plugins directory." ON)


# We create a ${name} target, which gets installed appropriately.
# We create a ${name}_test_lib target, which does *not* get installed.
# 
# The reason is OSX: Unity needs a .bundle because it won't find a .dylib.
# To do that you need a MODULE target. But you can't link to a MODULE to run
# unit tests. So we create a ${name}_test_lib target on top of the ${name}
# target. Oh, and it's STATIC because it's a pain to get the @rpath stuff to
# work in a unit testing setting.
#
# On other platforms, the two targets are the same, just make sure you
# link tests against the ${name}_test_lib target for your OSX colleagues.
#
function(add_plugin name)
    cmake_parse_arguments(arg "" "PLUGINS_DIR" "SOURCES" ${ARGN})

    # In all cases we link to these source files. Make sure that
    # compile-relevant properties set on ${name} are applied to the objects.
    add_library(${name}_objects OBJECT ${arg_SOURCES})
    set_target_properties(${name}_objects PROPERTIES 
            C_STANDARD $<TARGET_PROPERTY:${name},C_STANDARD>
            COMPILE_DEFINITIONS $<TARGET_PROPERTY:${name},COMPILE_DEFINITIONS>
            COMPILE_OPTIONS $<TARGET_PROPERTY:${name},COMPILE_OPTIONS>
            INCLUDE_DIRECTORIES $<TARGET_PROPERTY:${name},INCLUDE_DIRECTORIES>
    )

    if(ENABLE_OSX_BUNDLE)
        # To use in Unity, it must be a bundle. A bundle must be a MODULE.
        add_library(${name} MODULE $<TARGET_OBJECTS:${name}_objects>)
        set_target_properties(${name} PROPERTIES BUNDLE ON)

        # A MODULE can't be used to link tests against. Also, dynamic linking
        # gets you into rpath hell on OSX. So build a static lib on the side
        # for unit testing.
        add_library(${name}_test_lib STATIC $<TARGET_OBJECTS:${name}_objects>)
        target_link_libraries(${name}_test_lib $<TARGET_PROPERTY:${name},LINK_LIBRARIES>)
    else()
        # It's simpler on other platforms: we have a SHARED and that's it.
        # Add the ALIAS so that the tests don't have to think about this mess.
        add_library(${name} SHARED ${arg_SOURCES})
        add_library(${name}_test_lib ALIAS ${name})
    endif()


    if(ENABLE_DEPLOY)
        if(ENABLE_OSX_BUNDLE)
            SET(target_filename "${name}.bundle")
        else()
            SET(target_filename $<TARGET_FILE:${name}>)
        endif()
        add_custom_target("Deploy${name}" ALL
            COMMAND rm -rf ${arg_PLUGINS_DIR}/${target_filename}
            COMMAND cp -r ${target_filename} ${arg_PLUGINS_DIR}
            DEPENDS ${name}
        )
    endif()
endfunction()
