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
ROOT = os.path.dirname(Path(__file__).resolve().parent)
DOTNET = "dotnet.exe" if PLATFORM == "Windows" else "dotnet"
DOTNET_EMU_RUN_ARGS = ["run", "--project", f"{ROOT}/emu/_cs_main/TinyProc"]
DOTNET_GUI_RUN_ARGS = ["run", "--project", f"{ROOT}/emu/_cs_main/Avalonia/TinyProcVisualizer"]

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
    # Normalize paths (esp. important on Windows)
    for argIdx in range(0, len(commandArgs)):
        path = Path(commandArgs[argIdx])
        if (path.exists() or path.parent.is_dir()):
            commandArgs[argIdx] = str(path)
    log(f"CMD: {commandArgs}")
    os.chdir(ROOT)
    status = subprocess.run(commandArgs, cwd=ROOT)
    if (status.returncode != 0):
        builderror(status.returncode, f"Build exited with status {status.returncode}. Aborting.")

def enqueue_target(targetname, targetfunction):
    log(f"Adding target to queue: {targetname}")
    buildTargets.append(Target(targetname, targetfunction))

def target_finish():
    log(f"Target {_get_current_target_name()} finished successfully.")
    # Do nothing

# Cleanup

def register_shutdown_hook(hook):
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
        log_err("Build failed, aborting:")
        log_err(errormessage)
    else:
        log_err("Build failed, aborting.")
    buildexit(exitcode)

def buildexit(exitcode):
    for hook in shutdownHooks:
        hook()
    # Exit immediately (prevents "Exception ignored in atexit callback")
    # Might find a cleaner way to do this later.
    os._exit(exitcode)

def _list_flatten(inputlist):
    resultlist = []
    for elem in inputlist:
        if isinstance(elem, list): resultlist.extend(_list_flatten(elem))
        else: resultlist.append(elem)
    return resultlist

# Annoying __pycache__ stuff:
# https://stackoverflow.com/questions/50752302/python3-pycache-generating-even-if-pythondontwritebytecode-1
sys.dont_write_bytecode = True
atexit.register(_run_build)

if __name__ == "__main__":
    log_err("This is not a standalone runnable, but a library for simple python builds.")
    log_err("To build something, find the according build file and run it.")
    exit(1)