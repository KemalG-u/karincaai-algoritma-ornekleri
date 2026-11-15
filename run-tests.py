"""Windows-only offline test runner; uses the portfolio lock and existing local SDK."""
import msvcrt
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parent
SDK = ROOT.parent / 'sdk' / '10.0.402'
DOTNET = SDK / 'dotnet.exe'
LOCK = ROOT.parent.parent / 'test-kapisi' / 'portfoy-seri-isci.lock'
BASH = Path('D:/antigravity/hermes-home/tools/git-2.53.0+3-win32-x64/usr/bin/bash.exe')
GATE = 'D:/antigravity/_tmp/r133-kilit/agir.sh'


def main():
    if not (DOTNET.is_file() and LOCK.is_file() and BASH.is_file() and Path(GATE).is_file()):
        raise SystemExit('Yerel SDK, kilit veya kapı bulunamadı; kurulum yapılmadı.')
    with LOCK.open('r+b') as lock:
        lock.seek(0)
        try:
            msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        except OSError:
            raise SystemExit('Portföy kilidi dolu; başka iş bittiğinde yeniden deneyin.')
        work = Path(tempfile.mkdtemp(prefix='.algorithm-test-', dir=ROOT))
        try:
            for name in ('home', 'tmp', 'pkg'):
                (work / name).mkdir()
            env = os.environ.copy()
            for key in list(env):
                if key.startswith('AGIR_'):
                    del env[key]
            env.update(AGIR_KAPATMA='0', AGIR_BAYAT_TEMIZLE='0', AGIR_ANAHTAR='portfoy-seri-karincaai',
                       AGIR_SURE='0', AGIR_GPU='0', DOTNET_CLI_HOME=str(work / 'home'),
                       DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1',
                       DOTNET_NOLOGO='1', DOTNET_ROOT=str(SDK), NUGET_PACKAGES=str(work / 'pkg'),
                       TEMP=str(work / 'tmp'), TMP=str(work / 'tmp'), MSBUILDDISABLENODEREUSE='1')
            prefix = [str(BASH), GATE, 'portfoy-karincaai', str(DOTNET)]
            build = ['build', str(ROOT / 'AlgorithmSamples.csproj'), '--configfile', str(ROOT / 'NuGet.Config'),
                     '-p:BaseIntermediateOutputPath=' + str(work / 'obj') + '/',
                     '-p:OutputPath=' + str(work / 'bin') + '/', '-p:UseSharedCompilation=false', '-nodeReuse:false']
            rc = subprocess.run(prefix + build, cwd=ROOT, env=env).returncode
            if rc == 0:
                rc = subprocess.run(prefix + [str(work / 'bin' / 'AlgorithmSamples.dll')], cwd=ROOT, env=env).returncode
            return rc
        finally:
            # Never traverse a reparse point, including a junction.
            unsafe = any((entry.stat(follow_symlinks=False).st_file_attributes & 0x400)
                         for directory, dirs, files in os.walk(work, followlinks=False)
                         for entry in (Path(directory) / name for name in dirs + files))
            if unsafe or work.parent != ROOT or not work.name.startswith('.algorithm-test-'):
                print('Güvenli temizlik doğrulanamadı:', work, file=sys.stderr)
            else:
                shutil.rmtree(work)
            lock.seek(0)
            msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)


if __name__ == '__main__':
    sys.exit(main())
