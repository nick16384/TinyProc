# TinyProc custom build system (too stupid for Makefile syntax)
# To build a respective component, just head into the directory with the "tp_....py" file and run it.
# Each file will tell you their usage when used incorrectly.

from pathlib import Path
import os
import subprocess
import atexit

ROOT = os.path.dirname(Path(__file__).resolve().parents[0])
DOTNET = "dotnet"
DOTNET_RUN_ARGS = ["run", "--project", f"{ROOT}/emu/cs/TinyProc"]
DD = "dd"

buildtargets = []
shutdownactions = []

def log(message):
    print(f"BUILD || INFO || {message}")

def log_err(message):
    print(f"BUILD || ERROR || {message}")

def cmd_run(command):
    command = _list_flatten(command)
    log(f"CMD: {command}")
    os.chdir(ROOT)
    status = subprocess.run(command, cwd=ROOT)
    if (status.returncode != 0):
        builderror(status.returncode, f"Build exited with status {status.returncode}. Aborting.")

def enqueueTarget(targetFunction):
    buildtargets.append(targetFunction)

def targetFinish(name):
    log(f"Target {name} finished successfully.")
    # Do nothing

def _run_build():
    for target in buildtargets:
        target()

def builderror(exitcode, errormessage = ""):
    if (len(errormessage) > 0):
        log_err(f"Build failed, aborting:\n{errormessage}")
    else:
        log_err(f"Build failed, aborting.")
    buildexit(exitcode)

def buildexit(exitcode):
    for action in shutdownactions:
        action()
    exit(exitcode)

def addShutdownHook(hook):
    shutdownactions.append(hook)

def rmfile(file):
    if (os.path.isfile(file)):
        os.remove(file)

def _list_flatten(inputlist):
    resultlist = []
    for elem in inputlist:
        if isinstance(elem, list): resultlist.extend(_list_flatten(elem))
        else: resultlist.append(elem)
    return resultlist

atexit.register(_run_build)

if __name__ == "__main__":
    log_err("This is not a standalone runnable, but a library for simple python builds.")
    log_err("To build something, find the according build file and run it.")
    exit(1)