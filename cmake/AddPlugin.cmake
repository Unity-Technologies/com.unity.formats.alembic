if(UNIX)
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC")
endif()

if (NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE "Release" CACHE PATH "" FORCE)
endif()
if (CMAKE_INSTALL_PREFIX_INITIALIZED_TO_DEFAULT)
    set(CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/dist" CACHE PATH "" FORCE)
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
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

function(add_plugin name)
    cmake_parse_arguments(arg "" "PLUGINS_DIR" "SOURCES" ${ARGN})

    if(ENABLE_OSX_BUNDLE)
        # To use in Unity, it must be a bundle. A bundle must be a MODULE.
        add_library(${name} MODULE ${arg_SOURCES})
        set_target_properties(${name} PROPERTIES BUNDLE ON)
    else()
        add_library(${name} MODULE ${arg_SOURCES})
        set_target_properties(${name} PROPERTIES PREFIX "")
    endif()

    # Hide symbols so they can be stripped.
    # The -x and --gc-sections removes some stuff but strip -x removes yet more
    # (at least on Centos7).
    if(${CMAKE_CXX_COMPILER_ID} STREQUAL "GNU")
        target_link_libraries(${name} PRIVATE "-Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/version-script.txt -Wl,-x,--gc-sections")
    elseif(${CMAKE_SYSTEM_NAME} STREQUAL "Darwin")
        target_link_libraries(${name} PRIVATE "-exported_symbols_list ${CMAKE_CURRENT_SOURCE_DIR}/version-script-macos.txt -Wl,-x,-dead_strip")
    endif()

    if(ENABLE_DEPLOY)
        if(ENABLE_OSX_BUNDLE)
            SET(target_filename "${name}.bundle")
            SET(strip_filename "${name}.bundle/Contents/MacOS/${name}")
        else()
            SET(target_filename $<TARGET_FILE:${name}>)
            SET(strip_filename $<TARGET_FILE_NAME:${name}>)
        endif()
        add_custom_target("Deploy${name}" ALL
            COMMAND rm -rf "${arg_PLUGINS_DIR}/${target_filename}"
            COMMAND cp -rp "${target_filename}" "${arg_PLUGINS_DIR}"
            COMMAND strip -x "${arg_PLUGINS_DIR}/${strip_filename}"
            DEPENDS ${name}
        )
    endif()
endfunction()

#fix_default_compiler_settings_()
