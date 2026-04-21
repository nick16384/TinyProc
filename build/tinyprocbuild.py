# TinyProc custom build system (too stupid for Makefile syntax)
# To build a respective component, just head into the directory with the "tp_....py" file and run it.
# Each file will tell you their usage when used incorrectly.

from pathlib import Path
import os
import subprocess
import atexit
import platform
import sys
import traceback

# Variables similar to Make's environment variables
PLATFORM = platform.system()
ROOT = os.path.dirname(Path(__file__).resolve().parents[0])
DOTNET = "dotnet.exe" if PLATFORM == "Windows" else "dotnet"
DOTNET_EMU_RUN_ARGS = ["run", "--project", f"{ROOT}/emu/cs/TinyProc"]
DOTNET_GUI_RUN_ARGS = ["run", "--project", f"{ROOT}/emu/cs/Avalonia/TinyProcVisualizer"]

buildTargets = [] # A list of all targets to run
targetTrace = [] # Keeps track of the past and currently running target
shutdownHooks = [] # After an error or successful completion, run these hooks before exiting

class Target:
    name = ""
    target = lambda: ()
    def __init__(self, name, target):
        self.name = name
        self.target = target
    def run(self):
        self.target()

# Note:
# Functions declared with a prepending underscore are internal use only

# Logging

def log(message):
    print(f"{_get_current_target_name()} || INFO || {message}")

def log_err(message):
    print(f"{_get_current_target_name()} || ERROR || {message}")

# Commands & targets

def cmd_run(commandArgs):
    # Ensure command list is properly readable and standardized:
    commandArgs = _list_flatten([commandArgs])
    # Normalize paths
    for argIdx in range(0, len(commandArgs)):
        if (os.path.exists(os.path.normpath(commandArgs[argIdx]))):
            commandArgs[argIdx] = os.path.normpath(commandArgs[argIdx])
    log(f"CMD: {commandArgs}")
    os.chdir(ROOT)
    status = subprocess.run(commandArgs, cwd=ROOT)
    if (status.returncode != 0):
        builderror(status.returncode, f"Build exited with status {status.returncode}. Aborting.")

def enqueueTarget(targetname, targetfunction):
    log(f"Adding target to queue: {targetname}")
    buildTargets.append(Target(targetname, targetfunction))

def targetFinish():
    log(f"Target {_get_current_target_name()} finished successfully.")
    # Do nothing

# Cleanup

def addShutdownHook(hook):
    log(f"Registered shutdown hook.")
    shutdownHooks.append(hook)

def rmfile(file):
    if (os.path.isfile(file)):
        os.remove(file)

# Internals & helper functions

def _run_build():
    try:
        for i in range(0, len(buildTargets)):
            targetTrace.append(buildTargets[i])
            buildTargets[i].run()
    except Exception:
        builderror(1, f"Python build encountered an error: {traceback.format_exc()}")
    buildexit(0)

def _get_current_target_name():
    return targetTrace[-1].name if len(targetTrace) > 0 else "***"

def builderror(exitcode, errormessage = ""):
    log_err("==========================================================")
    if (len(errormessage) > 0):
        log_err(f"Build failed, aborting:\n{errormessage}")
    else:
        log_err(f"Build failed, aborting.")
    buildexit(exitcode)

def buildexit(exitcode):
    for hook in shutdownHooks:
        hook()
    if (exitcode == 0):
        return exitcode
    exit(exitcode)

def _list_flatten(inputlist):
    resultlist = []
    for elem in inputlist:
        if isinstance(elem, list): resultlist.extend(_list_flatten(elem))
        else: resultlist.append(elem)
    return resultlist

def normpath(pathstr):
    return os.path.normpath(pathstr)

# Annoying __pycache__ stuff:
# https://stackoverflow.com/questions/50752302/python3-pycache-generating-even-if-pythondontwritebytecode-1
sys.dont_write_bytecode = True
atexit.register(_run_build)

if __name__ == "__main__":
    log_err("This is not a standalone runnable, but a library for simple python builds.")
    log_err("To build something, find the according build file and run it.")
    exit(1)