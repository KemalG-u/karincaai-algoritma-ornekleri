"""Offline build-and-run test runner.

Builds AlgorithmSamples.csproj into a temporary directory (NuGet sources cleared,
no external packages) and runs the synthetic tests. Works on any OS with a .NET 10 SDK.

Usage:
    python run-tests.py

The dotnet executable is taken from the DOTNET_EXE environment variable, or from PATH.
"""
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def find_dotnet():
    exe = os.environ.get('DOTNET_EXE') or shutil.which('dotnet')
    if not exe or not Path(exe).is_file():
        raise SystemExit('dotnet not found: install the .NET 10 SDK or set DOTNET_EXE.')
    return exe


def main():
    dotnet = find_dotnet()
    with tempfile.TemporaryDirectory(prefix='algorithm-test-') as work:
        work = Path(work)
        env = os.environ.copy()
        env.update(DOTNET_CLI_HOME=str(work / 'home'), NUGET_PACKAGES=str(work / 'pkg'),
                   DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1',
                   DOTNET_NOLOGO='1', MSBUILDDISABLENODEREUSE='1')
        build = [dotnet, 'build', str(ROOT / 'AlgorithmSamples.csproj'),
                 '--configfile', str(ROOT / 'NuGet.Config'),
                 '-p:BaseIntermediateOutputPath=' + str(work / 'obj') + '/',
                 '-p:OutputPath=' + str(work / 'bin') + '/',
                 '-p:UseSharedCompilation=false', '-nodeReuse:false']
        rc = subprocess.run(build, cwd=ROOT, env=env).returncode
        if rc != 0:
            return rc
        return subprocess.run([dotnet, str(work / 'bin' / 'AlgorithmSamples.dll')], cwd=ROOT, env=env).returncode


if __name__ == '__main__':
    sys.exit(main())
