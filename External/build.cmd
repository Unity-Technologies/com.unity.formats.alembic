@echo off

set PATH=%PATH%;%cd%\7z

set installdir=%cd%\install
set tgzdir=%cd%

if exist install (
    rmdir /s /q install
)
mkdir %installdir%

set hdf5_version=1.10.1

if exist hdf5-build (
    rmdir /s /q hdf5-build
)
mkdir hdf5-build
cd hdf5-build
7za x -aoa %tgzdir%\HDF5-%hdf5_version%-win64.zip
xcopy /Q /S /Y HDF5-%hdf5_version%-win64\* %installdir%
cd ..

if exist ilmbase-build (
    rmdir /s /q ilmbase-build
)
mkdir ilmbase-build
cd ilmbase-build
cmake ..\OpenExr -DCMAKE_BUILD_TYPE=Release ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_PREFIX_PATH=%installdir% ^
    -DOPENEXR_NAMESPACE_VERSIONING=OFF ^
    -DOPENEXR_BUILD_SHARED=OFF ^
    -DOPENEXR_BUILD_STATIC=ON ^
    -DOPENEXR_BUILD_OPENEXR=OFF ^
    -DOPENEXR_BUILD_PYTHON_LIBS=OFF ^
    -DBUILD_TESTING=OFF ^
    -G "Visual Studio 16 2019"
cmake --build . --target install --config Release -j
cd ..

if exist alembic-build (
    rmdir /s /q alembic-build
)
mkdir alembic-build
cd alembic-build
cmake ..\alembic -DCMAKE_BUILD_TYPE=Release ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_PREFIX_PATH=%installdir% ^
    -DALEMBIC_ILMBASE_HALF_LIB=%installdir%\lib\Half_s.lib ^
    -DALEMBIC_ILMBASE_IEXMATH_LIB=%installdir%\lib\IexMath_s.lib ^
    -DALEMBIC_ILMBASE_ILMTHREAD_LIB=%installdir%\lib\IlmThread_s.lib ^
    -DALEMBIC_ILMBASE_IEX_LIB=%installdir%\lib\Iex_s.lib ^
    -DALEMBIC_ILMBASE_IMATH_LIB=%installdir%\lib\Imath_s.lib ^
    -DUSE_BINARIES=OFF ^
    -DUSE_TESTS=OFF ^
    -DALEMBIC_SHARED_LIBS=OFF ^
    -DUSE_HDF5=ON ^
    -DHDF5_USE_STATIC_LIBRARIES=ON ^
    -DALEMBIC_ILMBASE_LINK_STATIC=ON ^
    -DUSE_STATIC_HDF5=ON ^
    -DILMBASE_ROOT=%installdir% ^
    -DHDF5_ROOT=%installdir% ^
    -G "Visual Studio 16 2019"
cmake --build . --target install --config Release -j
cd ..
