call "%VS140COMNTOOLS%..\..\VC\vcvarsall.bat"
msbuild abci.sln /t:Build /p:Configuration=MasterDLL /p:Platform=x64 /m /nologo
msbuild abci.sln /t:Build /p:Configuration=MasterDLL /p:Platform=Win32 /m /nologo
