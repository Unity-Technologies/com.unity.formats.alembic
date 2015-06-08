import os
import sys
import glob
import shutil
import excons
from excons.tools import tbb
from excons.tools import unity
from excons.tools import dl

use_externals = (sys.platform == "win32" and excons.Build64() and excons.GetArgument("use-externals", 1, int) != 0)
if use_externals:
  # Provided externals are built using runtime version 12.0
  # 'mscver' has to be set before excons.MakeBaseEnv is called for the toolchain to be properly setup by SCons
  excons.SetArgument("mscver", "12.0")

env = excons.MakeBaseEnv()

# I don't know whst this whole PatchLibrary is. Looks like a hack that we don't
# really need. Let's disable it for now by defining aiMaster
defines = ["aiMaster"]
inc_dirs = []
lib_dirs = []
libs = []
customs = []
install_files = {"unity/AlembicImporter/Scripts": glob.glob("UnityAlembicImporter/Assets/AlembicImporter/Scripts/*.cs")}
sources = filter(lambda x: os.path.basename(x) not in ["pch.cpp", "AddLibraryPath.cpp"], glob.glob("Plugin/*.cpp"))

if excons.GetArgument("debug", 0, int):
  defines.append("aiDebug")

if use_externals:
  if excons.GetArgument("d3d11", 0, int) == 0:
    defines.append("UNITY_ALEMBIC_NO_D3D11")
  
  inc_dirs.extend(["Plugin/external/ilmbase-2.2.0/Half",
                   "Plugin/external/ilmbase-2.2.0/Iex",
                   "Plugin/external/ilmbase-2.2.0/IexMath",
                   "Plugin/external/ilmbase-2.2.0/Imath",
                   "Plugin/external/ilmbase-2.2.0/IlmThread",
                   "Plugin/external/ilmbase-2.2.0/config",
                   "Plugin/external/hdf5-1.8.14/hl/src",
                   "Plugin/external/hdf5-1.8.14/src",
                   "Plugin/external/hdf5-1.8.14",
                   "Plugin/external/alembic-1_05_08/lib",
                   "Plugin/external"])
  
  lib_dirs.append("Plugin/external/libs/x86_64")
  
  embed_libs = ["Plugin/external/libs/x86_64/tbb.dll"]
  
  # Cleanup build using custom alembic
  inc_dir = os.path.join(excons.OutputBaseDirectory(), "include")
  if os.path.isdir(inc_dir):
    shutil.rmtree(inc_dir)

  lib_dir = os.path.join(excons.OutputBaseDirectory(), "lib")
  if os.path.isdir(lib_dir):
    shutil.rmtree(lib_dir)

  dll_pattern = os.path.join(excons.OutputBaseDirectory(), "unity", "AlembicImporter", "Plugins", "x86_64", "*.dll")
  keep_names = ["tbb", "AlembicImporter", "AddLibraryPath"]
  dlls_to_remove = filter(lambda x: os.path.splitext(os.path.basename(x))[0] not in keep_names, glob.glob(dll_pattern))
  for dll in dlls_to_remove:
    os.remove(dll)

else:
  SConscript("alembic/SConstruct")
  
  Import("RequireAlembic")
  
  customs.append(RequireAlembic())
  
  defines.append("UNITY_ALEMBIC_NO_AUTOLINK")
  
  tbb_incdir, tbb_libdir = excons.GetDirs("tbb")
  if not tbb_incdir and not tbb_libdir:
    defines.append("UNITY_ALEMBIC_NO_TBB")
  
  else:
    customs.append(tbb.Require)
  
  if sys.platform != "win32" or excons.GetArgument("d3d11", 0, int) == 0:
    defines.append("UNITY_ALEMBIC_NO_D3D11")
  
  embed_libs = excons.GetArgument("embed-libs", None)
  
  if embed_libs:
    if os.path.isdir(embed_libs):
      if sys.platform == "darwin":
        embed_libs = glob.glob(embed_libs + "/*.dylib")
      
      elif sys.platform == "win32":
        embed_libs = glob.glob(embed_libs + "/*.dll")
      
      else:
        embed_libs = glob.glob(embed_libs + "/*.so")
    
    else:
      embed_libs = [embed_libs]
  
  else:
    embed_libs = []

  if sys.platform == "win32":
    # Cleanup build using provided external
    tbb_dll = os.path.join(excons.OutputBaseDirectory(), "unity", "AlembicImporter", "Plugins", "x86_64", "tbb.dll")
    if os.path.isfile(tbb_dll):
      os.remove(tbb_dll)

targets = [
  { "name": "AlembicImporter",
    "type": "dynamicmodule",
    "defs": defines,
    "incdirs": inc_dirs,
    "libdirs": lib_dirs,
    "libs": libs,
    "custom": customs,
    "srcs": sources,
    "install": install_files
  },
  # This also looks like a ugly hack that may no be necessary if unity provided
  # us with some per project directory where we can drop dependencies in...
  { "name": "AddLibraryPath",
    "type": "dynamicmodule",
    "custom": [dl.Require],
    "srcs": ["Plugin/AddLibraryPath.cpp"]
  }
]

unity.Plugin(targets[0], libs=embed_libs)
unity.Plugin(targets[1], package="AlembicImporter")

excons.DeclareTargets(env, targets)

Default(["AlembicImporter", "AddLibraryPath"])
