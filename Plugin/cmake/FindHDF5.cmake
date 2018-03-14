#
# MIT License
#
# Copyright (c) 2015-2017 Unity Technologies Japan
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.
#
if (StaticHDF5_FOUND)
  return()
endif()

# This has no connection to the cmake FindHDF5.cmake ; for our project we
# need to find the *static* HDF5.
find_library(STATIC_HDF5_C_LIBRARY NAMES libhdf5.a hdf5.lib)
find_library(STATIC_LIBZ NAMES libz.a libz.lib)
set(HDF5_C_LIBRARIES ${STATIC_HDF5_C_LIBRARY} ${STATIC_LIBZ})

# On linux we need to link to libdl
if(${CMAKE_SYSTEM_NAME} STREQUAL "Linux")
  find_library(LIBDL dl)
  list(APPEND HDF5_C_LIBRARIES ${LIBDL})
endif()

include(FindPackageHandleStandardArgs)
find_package_handle_standard_args("StaticHDF5" DEFAULT_MSG HDF5_C_LIBRARIES)
