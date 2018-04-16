#
# Copyright by The HDF Group.
# All rights reserved.
#
# This file is part of HDF5.  The full HDF5 copyright notice, including
# terms governing use, modification, and redistribution, is contained in
# the COPYING file, which can be found at the root of the source code
# distribution tree, or in https://support.hdfgroup.org/ftp/HDF5/releases.
# If you do not have access to either file, you may request a copy from
# help@hdfgroup.org.
#
#-----------------------------------------------------------------------------
# HDF5 Version file for install directory
#-----------------------------------------------------------------------------
#
# The created file sets PACKAGE_VERSION_EXACT if the current version string and
# the requested version string are exactly the same and it sets
# PACKAGE_VERSION_COMPATIBLE if the current version is >= requested version,
# but only if the requested major.minor version is the same as the current one.
# The variable HDF5_VERSION_STRING must be set before calling configure_file().

set (PACKAGE_VERSION "1.10.1")

if("${PACKAGE_VERSION}" VERSION_LESS "${PACKAGE_FIND_VERSION}" )
  set(PACKAGE_VERSION_COMPATIBLE FALSE)
else ()
  if ("${PACKAGE_FIND_VERSION_MAJOR}" STREQUAL "1")

    # exact match for version 1.10
    if ("${PACKAGE_FIND_VERSION_MINOR}" STREQUAL "10")

      # compatible with any version 1.10.x
      set (PACKAGE_VERSION_COMPATIBLE TRUE)

      if ("${PACKAGE_FIND_VERSION_PATCH}" STREQUAL "1")
        set (PACKAGE_VERSION_EXACT TRUE)

        if ("${PACKAGE_FIND_VERSION_TWEAK}" STREQUAL "")
          # not using this yet
        endif ()
      endif ()
    else ()
      set (PACKAGE_VERSION_COMPATIBLE FALSE)
    endif ()
  endif ()
endif ()

# if the installed or the using project don't have CMAKE_SIZEOF_VOID_P set, ignore it:
if("${CMAKE_SIZEOF_VOID_P}"  STREQUAL ""  OR "8" STREQUAL "")
   return()
endif ()

# check that the installed version has the same 32/64bit-ness as the one which is currently searching:
if(NOT "${CMAKE_SIZEOF_VOID_P}" STREQUAL "8")
  math(EXPR installedBits "8 * 8")
  set(PACKAGE_VERSION "${PACKAGE_VERSION} (${installedBits}bit)")
  set(PACKAGE_VERSION_UNSUITABLE TRUE)
endif ()
