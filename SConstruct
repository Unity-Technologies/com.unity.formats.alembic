import os
import sys
import glob
import excons


install_files = {"unity/AlembicImporter/Scripts": glob.glob("UnityAlembicImporter/Assets/AlembicImporter/Scripts/*.cs")}

if sys.platform == "win32":
  prefix = "unity/AlembicImporter/Plugins/x86"
  if excons.arch_dir == "x64":
    prefix += "_64"

elif sys.platform == "darwin":
  bundle_contents = "unity/AlembicImporter/Plugins/AlembicImporter.bundle/Contents"
  prefix = bundle_contents + "/MacOS"
  install_files[bundle_contents] = ["Plugin/Info.plist"]

else:
  print("Unsupported platform")
  sys.exit(1)

sources = filter(lambda x: os.path.basename(x) not in ["pch.cpp", "AddLibraryPath.cpp"], glob.glob("Plugin/*.cpp"))


SConscript("alembic/SConstruct")

Import("RequireAlembic")

env = excons.MakeBaseEnv()

plugins = [
  { "name": "AlembicImporter",
    "type": "dynamicmodule",
    "prefix": prefix,
    "defs": ["UNITY_ALEMBIC_NO_TBB"],
    "srcs": sources,
    "custom": [RequireAlembic()],
    "install": install_files
  }
]

if sys.platform == "win32":
  plugins.append({"name": "AddLibraryPath",
                  "type": "dynamicmodule",
                  "prefix": prefix,
                  "srcs": ["Plugin/AddLibraryPath.cpp"]})


excons.DeclareTargets(env, plugins)

Default(["AlembicImporter", "AddLibraryPath"])
