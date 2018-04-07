#! /bin/bash -x

set -e

builddir=`pwd`/build
pushd `dirname $0`
srcdir=`pwd`
popd

CMAKE=${CMAKE:-cmake}
HDF5=${HDF5:-hdf5-1.10.1}
ILMBASE=${ILMBASE:-ilmbase-2.2.1}
ALEMBIC=${ALEMBIC:-alembic-1.7.5}
ISPC=${ISPC:-ispc-v1.9.2-linux}

# Produce fast and small code (but not debuggable), and produce it to be relocatable
# since in the end we'll link it all together in a shared object.
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
        if [ -f "${srcdir}/external/sources/${HDF5}.tar.bz2" ] ; then
	    bzip2 -cd "${srcdir}/external/sources/${HDF5}.tar.bz2" | tar xvf -
        elif [ -f "${srcdir}/external/sources/${HDF5}.tar.gz" ] ; then
            tar xzvf "${srcdir}/external/sources/${HDF5}.tar.gz"
        fi
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
