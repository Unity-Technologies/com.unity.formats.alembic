@echo off

set installdir=%cd%\install
set tgzdir=%cd%
set PATH=%PATH%;%cd%\7z

set HDF5_VERSION=1.8.20

md %installdir%

md hdf5-build
cd hdf5-build
7za x -aoa %tgzdir%\HDF5-%HDF5_VERSION%-win64.zip
xcopy /Q /S /Y HDF5-%HDF5_VERSION%-win64\* %installdir%
cd ..

md ilmbase-build
cd ilmbase-build
cmake ..\OpenExr\IlmBase -DCMAKE_BUILD_TYPE=Release ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_PREFIX_PATH=%installdir% ^
    -DNAMESPACE_VERSIONING=OFF ^
    -DBUILD_SHARED_LIBS=OFF ^
    -G "Visual Studio 14 2015 Win64"
cmake --build . --target install --config Release
cd ..

md alembic-build
cd alembic-build
cmake ..\alembic -DCMAKE_BUILD_TYPE=Release ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_PREFIX_PATH=%installdir% ^
    -DUSE_BINARIES=OFF ^
    -DUSE_TESTS=OFF ^
    -DALEMBIC_SHARED_LIBS=OFF ^
    -DUSE_HDF5=ON ^
    -DHDF5_USE_STATIC_LIBRARIES=ON ^
    -DALEMBIC_ILMBASE_LINK_STATIC=ON ^
    -DUSE_STATIC_HDF5=ON ^
    -DILMBASE_ROOT=%installdir% ^
    -DHDF5_ROOT=%installdir% ^
    -G "Visual Studio 14 2015 Win64"
cmake --build . --target install --config Release
cd ..