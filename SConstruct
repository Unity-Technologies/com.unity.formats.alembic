import os
import sys
import glob
import excons


install_files = {"unity/AlembicImporter/Scripts": glob.glob("UnityAlembicImporter/Assets/AlembicImporter/Scripts/*.cs")}

if sys.platform == "win32":
  prefix = "unity/AlembicImporter/Plugins/x86"
  if excons.Build64():
    prefix += "_64"

elif sys.platform == "darwin":
  bundle_contents = "unity/AlembicImporter/Plugins/AlembicImporter.bundle/Contents"
  prefix = bundle_contents + "/MacOS"
  install_files[bundle_contents] = ["Plugin/Info.plist"]

else:
  print("Unsupported platform")
  sys.exit(1)

sources = filter(lambda x: os.path.basename(x) not in ["pch.cpp", "AddLibraryPath.cpp"], glob.glob("Plugin/*.cpp"))


env = excons.MakeBaseEnv()

default_targets = ["AlembicImporter"]

loadable_ext = ("" if sys.platform == "darwin" else (".dll" if sys.platform == "win32" else ".so"))

# I don't know whst this whole PatchLibrary is. Looks like a hack that we don't
# really need. Let's disable it for now by defining aiMaster
defines = ["aiMaster"]
inc_dirs = []
lib_dirs = []
libs = []
customs = []

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
    print("TBB inc='%s', lib='%s'" % (tbb_incdir, tbb_libdir))
    if tbb_incdir:
      inc_dirs.append(tbb_incdir)
    
    if tbb_libdir:
      lib_dirs.append(tbb_libdir)
    
    libname = excons.GetArgument("tbb-libname", None)
    if not libname:
      libname = "tbb%s" % (excons.GetArgument("tbb-libsuffix", ""))
    libs.append(libname)
  
  if sys.platform != "win32" or excons.GetArgument("d3d11", 0, int) == 0:
    defines.append("UNITY_ALEMBIC_NO_D3D11")


plugins = [
  { "name": "AlembicImporter",
    "type": "dynamicmodule",
    "prefix": prefix,
    "defs": defines,
    "ext": loadable_ext,
    "incdirs": inc_dirs,
    "libdirs": lib_dirs,
    "libs": libs,
    "custom": customs,
    "srcs": sources,
    "install": install_files
  }
]

if sys.platform == "win32":
  # This also looks like a ugly hack that may no be necessary if unity provided
  # us with some per project directory where we can drop dependencies in...
  plugins.append({"name": "AddLibraryPath",
                  "type": "dynamicmodule",
                  "prefix": prefix,
                  "ext": loadable_ext,
                  "srcs": ["Plugin/AddLibraryPath.cpp"]})
  
  default_targets.append("AddLibraryPath")


excons.DeclareTargets(env, plugins)

Default(default_targets)
