@echo off

set PATH=%PATH%;%cd%\7z

set installdir=%cd%\install
set tgzdir=%cd%

if exist install (
    rd /s /q install
)
md %installdir%

set hdf5_version=1.10.1

if exist hdf5-build (
    rd /s /q hdf5-build
)
md hdf5-build
cd hdf5-build
7za x -aoa %tgzdir%\HDF5-%hdf5_version%-win64.zip
xcopy /Q /S /Y HDF5-%hdf5_version%-win64\* %installdir%
cd ..

if exist ilmbase-build (
    rd /s /q ilmbase-build
)
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

if exist alembic-build (
    rd /s /q alembic-build
)
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