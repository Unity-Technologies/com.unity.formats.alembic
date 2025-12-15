import subprocess
import argparse
import glob
import sys
import os

def verify_signatures(args):
    architecture = args.architecture
    codesign_list_file = args.codesign_list_file
    signtool_pattern = os.path.join(
        'C:\\build\\output\\unity',
        '*.cds.ci.code-signing-*',
        'powershell',
        'artifacts',
        'winsdk-signtool_*',
        'bin',
        '*',
        architecture,
        'signtool.exe'
    )

    signtool_path = glob.glob(signtool_pattern)
    if not signtool_path:
        print(f"signtool.exe not found with pattern: {signtool_pattern}")
        sys.exit(1)
    
    files_to_verify = []
    with open(codesign_list_file, 'r') as f:
        files_to_verify = [line.strip() for line in f if line.strip()]

    if len(files_to_verify) == 0:
        print("No binary found to verify.")
        sys.exit(0)

    for f in files_to_verify:
        result = subprocess.run([signtool_path[0], 'verify', '/pa', f], capture_output=True, text=True)
        if result.returncode != 0:
            print(f"Verification failed for {f}")
            print(result.stdout)
            print(result.stderr)
            sys.exit(1)
        else:
            print(result.stdout)

    print(f"All {len(files_to_verify)} files are signed.")
    sys.exit(0)


def parse_args():
    parser = argparse.ArgumentParser(description='Verifies that Windows executables are signed using the SignTool utility.')
    parser.add_argument('--architecture', required=True, choices=['x64', 'arm64'])
    parser.add_argument('--codesign-list-file', required=True)

    return parser.parse_args()

if __name__ == '__main__':
    args = parse_args()
    verify_signatures(args)