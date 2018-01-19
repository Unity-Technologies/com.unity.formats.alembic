call buildtools.bat
msbuild abci.sln /t:Build /p:Configuration=Master /p:Platform=Win32 /m /nologo
msbuild abci.sln /t:Build /p:Configuration=Master /p:Platform=x64 /m /nologo
