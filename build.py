#!/usr/bin/python -B
import os
import platform

def packages_list():
    return [
        ("com.unity.formats.alembic", os.path.join("build", "install", "com.unity.formats.alembic"))
    ]

def test_packages_list():
    return [
        "com.unity.formats.alembic"
    ]

def gitlab_ci_build_stage():
    return "build"

def gitlab_ci_test_stage():
    return "test"

def gitlab_ci_build_environment_fixup(build_env, logger):
    import unity_editor_common
    build_env["npm_config_registry"] = "https://staging-packages.unity.com"
    if platform.system() == "Darwin" or platform.system() == "Linux":
        # Assumption we need a mono, and we can grab it from here
        build_env["PATH"] = unity_editor_common.get_mono_path() + ":" + build_env["PATH"]
    elif platform.system() == "Windows":
        # Make sure CMake is in the path
        build_env["PATH"] = build_env["PATH"] + ";" + r'"C:\Program Files\CMake\bin"'

    return build_env

def prepare_playmode_test_project(repo_path, project_path, logger):
    import unity_package_build

if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.abspath(os.path.join("..", "automation-tools")))
    
    try:
        import unity_package_build
        build_log = unity_package_build.setup()
    except ImportError:
        print "No Automation Tools found."

