@echo off

cd External
call build.cmd
cd ..

SET depsdir=%cd%\External\install
SET installdir=%cd%


if exist build (
    rmdir /s /q build
)
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release ^
    -DALEMBIC_DIR=%depsdir% ^
    -DOPENEXR_LIBRARY_DIR=%depsdir% ^
    -DHDF5_USE_STATIC_LIBRARIES=ON ^
    -DHDF5_ROOT=%depsdir% ^
    -DOPENEXR_IexMath_LIBRARY=%depsdir%\lib\IexMath_s.lib ^
    -DOPENEXR_Half_LIBRARY=%depsdir%\lib\Half_s.lib ^
    -DOPENEXR_Iex_LIBRARY=%depsdir%\lib\Iex_s.lib ^
    -DOPENEXR_IlmThread_LIBRARY=%depsdir%\lib\IlmThread_s.lib ^
    -DOPENEXR_Imath_LIBRARY=%depsdir%\lib\Imath_s.lib ^
    -DUSE_STATIC=ON ^
    -DENABLE_DEPLOY=OFF ^
    -DCMAKE_PREFIX_PATH=%depsdir% ^
    -DCMAKE_INSTALL_PREFIX=%installdir% ^
    -G "Visual Studio 16 2019"
cmake --build . --target INSTALL --config Release
cd ..
