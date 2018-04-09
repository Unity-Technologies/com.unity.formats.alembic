#! /bin/bash

set -e

builddir=`pwd`/build
pushd `dirname $0`
srcdir=`pwd`
popd

# You need cmake 3.2 or later to compile some of the dependences.
# You can change the cmake binary using e.g. 'CMAKE=/bin/cmake3 build.sh'
CMAKE=${CMAKE:-cmake}
#
# Go to https://www.hdfgroup.org/downloads/ and put the tarball in external/sources/ then update the version number here:
HDF5=${HDF5:-hdf5-1.10.1}
#
# Go to http://www.openexr.com/downloads.html and download ilmbase; put the tarball into external/sources/ then update the version number here:
ILMBASE=${ILMBASE:-ilmbase-2.2.1}
#
# Go to https://github.com/alembic/alembic/releases and download Alembic; put the tarball into external/sources/ then update the version number here:
ALEMBIC=${ALEMBIC:-alembic-1.7.5}
#
# Install ispc with your fave package manager e.g. on OSX:
#       sudo port install ispc
# If that doesn't work, go to https://ispc.github.io/downloads.html and download ISPC binaries; put the tarball into external/sources/ then update the version number here:
ISPC=${ISPC:-ispc-v1.9.2-linux}

# Produce fast and small code (but not debuggable), and produce it to be
# relocatable since in the end we'll link it all together in a shared object.
# Note that cmake seems to clobber the -O3 with a -O2, but we can dream.
export CXXFLAGS="-O3 -fomit-frame-pointer -fPIC"
export CFLAGS="-O3 -fomit-frame-pointer -fPIC"

message() {
    echo ""
    echo ""
    echo "************************************************************"
    echo "Building $1... started at `date`"
    echo "************************************************************"
    echo ""
}

# Find ISPC, either in the path or unpack it and put it in the path
message "ispc"
if command -v ispc > /dev/null 2>&1 ; then
	echo "using built-in ispc"
else
	if [ ! -d "${ISPC}" ] ; then
		tar xzvf "${srcdir}/external/sources/${ISPC}.tar.gz"
	fi
	export PATH="`pwd`/${ISPC}:$PATH"
	echo "using ${ISPC}"
fi

# Build HDF5.
message "HDF5"
if [ ! -d "${HDF5}" ] ; then
	bzip2 -cd "${srcdir}/external/sources/${HDF5}.tar.bz2" | tar xvf -
fi
if [ -d hdf5-build ] ; then
	echo "Already built"
else
	mkdir hdf5-build
	pushd hdf5-build
	# Sometimes cmake works better, sometimes configure, so keep both
	# paths going. The important thing: we disable shared, and we enable
	# thread safety.
	"${CMAKE}" ../${HDF5} -DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_INSTALL_PREFIX="${builddir}" \
		-DCMAKE_PREFIX_PATH="${builddir}" \
		-DBUILD_SHARED_LIBS=OFF \
		-DHDF5_ENABLE_THREADSAFE=ON \
		-DBUILD_TESTING=OFF \
		-DHDF5_BUILD_TOOLS=OFF \
		-DHDF5_BUILD_EXAMPLES=OFF \
		-DHDF5_BUILD_HL_LIB=OFF \
		-DHDF5_BUILD_CPP_LIB=OFF
#	"../${HDF5}/configure" \
#		--disable-shared \
#		--enable-build-mode=production \
#		--enable-threadsafe \
#		CXXFLAGS="${CXXFLAGS}" \
#		CFLAGS="${CFLAGS}" \
#		--prefix="${builddir}"
	make -j4
	#make test # takes about 10 minutes, plus time to build
	make install
	popd
fi

message "ilmbase"
if [ ! -d "${ILMBASE}" ] ; then
	tar xzvf "${srcdir}/external/sources/${ILMBASE}.tar.gz"
fi
if [ -d ilmbase-build ] ; then
	echo "Already built"
else
	mkdir ilmbase-build
	pushd ilmbase-build
	"${CMAKE}" ../${ILMBASE} -DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_INSTALL_PREFIX="${builddir}" \
		-DCMAKE_PREFIX_PATH="${builddir}" \
		-DNAMESPACE_VERSIONING=OFF \
		-DBUILD_SHARED_LIBS=OFF
	make -j4
	make test
	make install
	popd
fi

message "alembic"
if [ ! -d "${ALEMBIC}" ] ; then
	tar xzvf "${srcdir}/external/sources/${ALEMBIC}.tar.gz"
fi
if [ -d "alembic-build" ] ; then
	echo "Already built"
else
	mkdir alembic-build
	pushd alembic-build
	"${CMAKE}" "../${ALEMBIC}" -DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_INSTALL_PREFIX="${builddir}" \
		-DCMAKE_PREFIX_PATH="${builddir}" \
		-DALEMBIC_SHARED_LIBS=OFF \
		-DUSE_HDF5=ON \
		-DHDF5_USE_STATIC_LIBRARIES=ON \
		-DUSE_STATIC_HDF5=ON \
		-DHDF5_LIBRARIES="${builddir}/lib/libhdf5.a;-ldl;-lpthread" \
		-DHDF5_ROOT="${builddir}"
	make -j4
	make test
	make install
	popd
fi


message "the abci plugin"
if [ -d "plugin-build" ] ; then
	echo "Already built"
else
	mkdir plugin-build
	pushd plugin-build
	"${CMAKE}" "${srcdir}" -DCMAKE_BUILD_TYPE=Release \
		-DCMAKE_INSTALL_PREFIX="${builddir}" \
		-DCMAKE_PREFIX_PATH="${builddir}" \
		-DALEMBIC_DIR="${buildir}" \
		-DHDF5_USE_STATIC_LIBRARIES=ON \
		-DHDF5_LIBRARIES="${builddir}/lib/libhdf5.a;-ldl;-lpthread" \
		-DHDF5_ROOT="${builddir}" \
		-DUSE_STATIC=ON
	make -j4
	make test
	popd
fi
