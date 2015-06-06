import os
import sys
import glob
import excons
from excons.tools import tbb
from excons.tools import unity

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

if sys.platform == "win32" and excons.Build64() and excons.GetArgument("use-externals", 1, int) != 0:
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
                   "Plugin/external/tbb"])
  
  lib_dirs.append("Plugin/external/libs/x86_64")
  
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


plugins = [
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
    "srcs": ["Plugin/AddLibraryPath.cpp"]
  }
]

unity.AsPlugin(plugins[0])
unity.AsPlugin(plugins[1])

excons.DeclareTargets(env, plugins)

Default(["AlembicImporter", "AddLibraryPath"])
