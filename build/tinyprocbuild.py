# TinyProc custom build system (fuck Makefile)
# To build a respective component, just head into the directory with the "tp_....py" file and run it.
# Each file will tell you their usage when used incorrectly.

from pathlib import Path
import os
import subprocess
import string

ROOT = os.path.dirname(Path(__file__).resolve().parents[0])
DOTNET = "dotnet"
DOTNET_RUN_ARGS = ["run", "--project", f"{ROOT}/emu/cs/TinyProc"]
DD = "dd"

shutdownactions = []

def log(message):
    print(f"BUILD || INFO || {message}")

def log_err(message):
    print(f"BUILD || ERROR || {message}")

def buildcmd(command):
    command = list_flatten(command)
    log(f"CMD: {command}")
    os.chdir(ROOT)
    status = subprocess.run(command, cwd=ROOT)
    if (status.returncode != 0):
        log_err(f"Build exited with status {status.returncode}. Aborting.")
        buildexit(1)

def buildexit(exitcode):
    log("Running shutdown hooks...")
    for action in shutdownactions:
        action()
    exit(exitcode)

def addShutdownHook(hook):
    shutdownactions.append(hook)

def rmfile(file):
    os.remove(file)

def list_flatten(inputlist):
    resultlist = []
    for elem in inputlist:
        if isinstance(elem, list): resultlist.extend(list_flatten(elem))
        else: resultlist.append(elem)
    return resultlist

if __name__ == "__main__":
    log_err("This is not a standalone runnable, but a library for simple python builds.")
    log_err("To build something, find the according build file and run it.")
    exit(1)