if(ISPC_FOUND)
    return()
endif()

include(CMakeParseArguments)

find_program(7ZA 7za HINTS ${CMAKE_SOURCE_DIR}/External/7z)

set(ISPC_VERSION 1.14.1)
    if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
        set(ISPC_DIR "ispc-v${ISPC_VERSION}-linux")
        execute_process(
            WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
            COMMAND tar -xf ${CMAKE_SOURCE_DIR}/External/ispc/${ISPC_DIR}.tar.gz
        )
    elseif(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
        set(ISPC_DIR "ispc-v${ISPC_VERSION}-macOS")
        execute_process(
            WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
            COMMAND tar -xf ${CMAKE_SOURCE_DIR}/External/ispc/${ISPC_DIR}.tar.gz
        )
        execute_process(
            WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
            COMMAND xattr -d com.apple.quarantine  ${CMAKE_SOURCE_DIR}/build/ispc-v1.14.1-macOS/bin/ispc
        )

    elseif(CMAKE_SYSTEM_NAME STREQUAL "Windows")
        set(ISPC_DIR "ispc-v${ISPC_VERSION}-windows")
        execute_process(
            WORKING_DIRECTORY ${CMAKE_BINARY_DIR}
            COMMAND ${7ZA} x -aoa -o* ${CMAKE_SOURCE_DIR}/External/ispc/${ISPC_DIR}.zip
        )
    endif()

    set(ISPC "${CMAKE_BINARY_DIR}/${ISPC_DIR}/bin/ispc" CACHE PATH "" FORCE)

# e.g:
#add_ispc_targets(
#    SOURCES "src1.ispc" "src2.ispc"
#    HEADERS "header1.h" "header2.h"
#    OUTDIR "path/to/outputs")
function(add_ispc_targets)
    cmake_parse_arguments(arg "" "OUTDIR" "SOURCES;HEADERS" ${ARGN})

    if(UNIX)
        set(ISPC_OPTS --pic)
    endif()

    foreach(source ${arg_SOURCES})
        get_filename_component(name ${source} NAME_WE)
        set(header "${arg_OUTDIR}/${name}.h")
        set(object "${arg_OUTDIR}/${name}${CMAKE_CXX_OUTPUT_EXTENSION}")
        set(objects 
            ${object}
            "${arg_OUTDIR}/${name}_sse4${CMAKE_CXX_OUTPUT_EXTENSION}"
            "${arg_OUTDIR}/${name}_avx${CMAKE_CXX_OUTPUT_EXTENSION}"
            "${arg_OUTDIR}/${name}_avx512skx${CMAKE_CXX_OUTPUT_EXTENSION}"
        )
       
        if( CMAKE_SYSTEM_NAME STREQUAL "Darwin")
            set(object_arm64 "${arg_OUTDIR}/${name}_arm64${CMAKE_CXX_OUTPUT_EXTENSION}")
            add_custom_target(ISPC-ARM64 ALL
                COMMAND ${ISPC} ${source} -o ${object_arm64} ${ISPC_OPTS} --target-os=ios --target=neon-i32x8 --opt=fast-masked-vload --opt=fast-math
                DEPENDS ${source} ${arg_HEADERS}
            )

            list(APPEND objects ${object_arm64})
        endif()

        set(outputs ${header} ${objects})
        add_custom_command(
            OUTPUT ${outputs}
            COMMAND ${ISPC}
            ARGS ${source} -o ${object} -h ${header} ${ISPC_OPTS} --target=sse4-i32x4,avx1-i32x8,avx512skx-i32x16 --arch=x86-64 --opt=fast-masked-vload --opt=fast-math
            COMMENT "running:  ${ISPC} ${source} -o ${object} -h ${header} ${ISPC_OPTS} --target=sse4-i32x4,avx1-i32x8,avx512skx-i32x16 --arch=x86-64 --opt=fast-masked-vload --opt=fast-math"
            DEPENDS ${source} ${arg_HEADERS}
            MAIN_DEPENDENCY ${source}
        )

        list(APPEND _ispc_headers ${header})
        list(APPEND _ispc_objects ${objects})
        list(APPEND _ispc_outputs ${outputs})
    endforeach()

    set(_ispc_headers ${_ispc_headers} PARENT_SCOPE)
    set(_ispc_objects ${_ispc_objects} PARENT_SCOPE)
    set(_ispc_outputs ${_ispc_outputs} PARENT_SCOPE)

    file(MAKE_DIRECTORY ${arg_OUTDIR})
endfunction()

FIND_PACKAGE_HANDLE_STANDARD_ARGS(ISPC DEFAULT_MSG ISPC)
