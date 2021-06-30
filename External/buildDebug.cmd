@echo off

set PATH=%PATH%;%cd%\7z

set installdir=%cd%\install
set tgzdir=%cd%

if exist install (
    rmdir /s /q install
)
mkdir %installdir%

if exist ilmbase-build (
    rmdir /s /q ilmbase-build
)
mkdir ilmbase-build
cd ilmbase-build
cmake ..\OpenExr\IlmBase -DCMAKE_BUILD_TYPE=Debug ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_PREFIX_PATH=%installdir% ^
    -DNAMESPACE_VERSIONING=OFF ^
    -DBUILD_SHARED_LIBS=OFF ^
    -DCMAKE_CXX_FLAGS="/MP"
cmake --build . --target install --config Debug 
cd ..

if exist alembic-build (
    rmdir /s /q alembic-build
)
mkdir alembic-build
cd alembic-build
cmake ..\alembic -DCMAKE_BUILD_TYPE=Debug ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -DCMAKE_PREFIX_PATH=%installdir% ^
    -DUSE_BINARIES=OFF ^
    -DUSE_TESTS=OFF ^
    -DALEMBIC_SHARED_LIBS=OFF ^
    -DALEMBIC_ILMBASE_LINK_STATIC=ON ^
    -DILMBASE_ROOT=%installdir% ^
    -DCMAKE_CXX_FLAGS="/MP"
cmake --build . --target install --config Debug
cd ..
