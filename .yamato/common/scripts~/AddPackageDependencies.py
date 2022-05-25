import json
import os
import sys
from os import path

addedDependencies = {}

def handlePackageDependencies(packagesRoot, packageName, projectPath):
    global addedDependencies
    packagePath = os.path.join(packagesRoot, packageName)
    packageJsonFile = os.path.join(packagePath,"package.json")

    if (not os.path.exists(packageJsonFile)):
        print("Package.json file does not exists at location " + packageJsonFile)
        return -1

    with open(packageJsonFile, "r") as readFile:
        packageJson = json.load(readFile)

    if (not 'dependencies' in packageJson.keys()):
        return 0

    for dependency in packageJson['dependencies']:
        dependencyPathFromRoot = os.path.join(packagesRoot, dependency)
        if (path.exists(dependencyPathFromRoot) and not dependency in addedDependencies.keys()):
            addedDependencies[dependency] = 1
            dependencyRelativePath = os.path.relpath(dependencyPathFromRoot, os.path.dirname(os.path.join(projectPath, "Packages/manifest.json")))
            os.system("unity-config project add dependency " + dependency + "@file:" + dependencyRelativePath + " -p " + projectPath)
            handlePackageDependencies(packagesRoot, dependency, projectPath)
    return 0

def main():
    global addedDependencies
    if (len(sys.argv) != 4):
        print("usage: myscript [package name] [packages root] [project path]")
        print("package name : Name of the package to be analyzed.")
        print("packages root: Directory containing all the packages.")
        print("project path : Unity project used.")
        return -1

    packagesRoot = sys.argv[2]
    packageName = sys.argv[1]
    projectPath = sys.argv[3]
    projectManifestFile = os.path.join(sys.argv[3], "Packages/manifest.json")

    if (not os.path.exists(projectManifestFile)):
        print("Error project: Manifest.json file not found at location " + projectManifestFile)
        return -1

    print("Analyzing package " + packageName)
    addedDependencies[packageName] = 1
    handlePackageDependencies(packagesRoot, packageName, projectPath)

if __name__ == "__main__" :
    main()