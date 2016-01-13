import os
import sys
import glob
import shutil
import excons
from excons.tools import unity
from excons.tools import tbb
from excons.tools import dl
from excons.tools import glew

use_externals = (sys.platform == "win32" and excons.Build64() and excons.GetArgument("use-externals", 1, int) != 0)
if use_externals:
  # Provided externals are built using runtime version 12.0
  # 'mscver' has to be set before excons.MakeBaseEnv is called for the toolchain to be properly setup by SCons
  excons.SetArgument("mscver", "12.0")

env = excons.MakeBaseEnv()

# I don't know whst this whole PatchLibrary is. Looks like a hack that we don't
# really need. Let's disable it for now by defining aiMaster
defines = ["aiMaster"]
inc_dirs = ["Plugin/Foundation", "Plugin/Importer"]
lib_dirs = []
libs = []
embed_libs = []
customs = []
install_files = {"unity/AlembicImporter/Scripts": glob.glob("AlembicImporter/Assets/AlembicImporter/Scripts/*.cs*"),
                 "unity/AlembicImporter/Editor": glob.glob("AlembicImporter/Assets/AlembicImporter/Editor/*.cs*"),
                 "unity/AlembicImporter/Materials": glob.glob("AlembicImporter/Assets/AlembicImporter/Materials/AlembicPoints*"),
                 "unity/AlembicImporter/Meshes": glob.glob("AlembicImporter/Assets/AlembicImporter/Meshes/Quad*") +
                                                 glob.glob("AlembicImporter/Assets/AlembicImporter/Meshes/Cube*") +
                                                 glob.glob("AlembicImporter/Assets/AlembicImporter/Meshes/IcoSphere*"),
                 "unity/AlembicImporter/Shaders": glob.glob("AlembicImporter/Assets/AlembicImporter/Shaders/DataViz.shader*") +
                                                  glob.glob("AlembicImporter/Assets/AlembicImporter/Shaders/AlembicPoints*.shader*")}
sources = filter(lambda x: os.path.basename(x) not in ["pch.cpp"], glob.glob("Plugin/Importer/*.cpp"))
sources.extend(glob.glob("Plugin/Foundation/*.cpp"))

if excons.GetArgument("debug", 0, int) != 0:
  defines.append("aiDebug")

if excons.GetArgument("debug-log", 0, int) != 0:
  defines.append("aiDebugLog")

if excons.GetArgument("texture-mesh", 0, int) != 0:
  defines.append("aiSupportTextureMesh")
  sources.extend(["Plugin/GraphicsDevice/aiGraphicsDevice.cpp"])
  install_files["unity/AlembicImporter/Meshes"] = glob.glob("AlembicImporter/Assets/AlembicImporter/Meshes/IndexOnlyMesh.asset*")
  install_files["unity/AlembicImporter/Materials"] = glob.glob("AlembicImporter/Assets/AlembicImporter/Materials/AlembicStandard.mat*")
  install_files["unity/AlembicImporter/Shaders"].extend(glob.glob("AlembicImporter/Assets/AlembicImporter/Shaders/AICommon.cginc*") +
                                                        glob.glob("AlembicImporter/Assets/AlembicImporter/Shaders/AIStandard.shader*"))
  
  if excons.GetArgument("opengl", 1, int) != 0:
    defines.extend(["aiSupportOpenGL", "aiDontForceStaticGLEW"])
    sources.append("Plugin/GraphicsDevice/aiGraphicsDeviceOpenGL.cpp")
    customs.append(glew.Require)

  if sys.platform == "win32":
    if excons.GetArgument("d3d9", 1, int) != 0:
      defines.append("aiSupportD3D9")
      sources.append("Plugin/GraphicsDevice/aiGraphicsDeviceD3D9.cpp")

    if excons.GetArgument("d3d11", 1, int) != 0:
      defines.append("aiSupportD3D11")
      sources.append("Plugin/GraphicsDevice/aiGraphicsDeviceD3D11.cpp")

if use_externals:
  # we're on windows if we fall here
  
  inc_dirs.extend(["Plugin/external/ilmbase/include",
                   "Plugin/external/HDF5/include",
                   "Plugin/external/alembic/include",
                   "Plugin/external/glew/include"])
  
  lib_dirs.append("Plugin/external/libs/x86_64")
  
  # Cleanup build using custom alembic
  inc_dir = os.path.join(excons.OutputBaseDirectory(), "include")
  if os.path.isdir(inc_dir):
    shutil.rmtree(inc_dir)

  lib_dir = os.path.join(excons.OutputBaseDirectory(), "lib")
  if os.path.isdir(lib_dir):
    shutil.rmtree(lib_dir)

  dll_pattern = os.path.join(excons.OutputBaseDirectory(), "unity", "AlembicImporter", "Plugins", "x86_64", "*.dll")
  keep_names = ["AlembicImporter"]
  dlls_to_remove = filter(lambda x: os.path.splitext(os.path.basename(x))[0] not in keep_names, glob.glob(dll_pattern))
  for dll in dlls_to_remove:
    os.remove(dll)

else:
  excons.SetArgument("c++1", 1)
  SConscript("alembic/SConstruct")
  
  Import("RequireAlembic")
  
  customs.append(RequireAlembic())
  
  defines.append("aiNoAutoLink")

  if excons.GetArgument("tbb", 0, int) != 0:
    defines.append("aiWithTBB")
    customs.append(tbb.Require)
  
  embed_libs = excons.GetArgument("embed-libs", [])
  if embed_libs:
    if os.path.isdir(embed_libs):
      pat = ("/*.dylib" if sys.platform == "darwin" else ("/*.dll" if sys.platform == "win32" else "/*.so"))
      embed_libs = glob.glob(embed_libs + pat)
    else:
      embed_libs = [embed_libs]

importer = {"name": "AlembicImporter",
            "type": "dynamicmodule",
            "defs": defines,
            "incdirs": inc_dirs,
            "libdirs": lib_dirs,
            "libs": libs,
            "custom": customs,
            "srcs": sources,
            "install": install_files}

unity.Plugin(importer, libs=embed_libs)

tester = {"name": "tester",
          "type": "program",
          "custom": [dl.Require],
          "srcs": ["TestData/tester.cpp"]}

targets = [importer, tester]

excons.DeclareTargets(env, targets)

Default(["AlembicImporter"])
