import sys
from build.tinyprocbuild import *
from build.tp_buildrom import tp_buildrom

TP_WORDSIZE = 4

def tp_run():
    if (len(sys.argv) < 2):
        log_err("Error: Supplied less than 1 argument.")
        printUsage()
        buildexit(1)
    
    sourceFileAsm = sys.argv.pop()
    log(f"Assembling and running source file {sourceFileAsm}")
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/{sourceFileAsm}"])

    # Ensure latest version of ROM image
    tp_buildrom()

    targetFileBin = sourceFileAsm[:-4]
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--run", f"{ROOT}/{targetFileBin}.bin"])

    targetFinish("EMU RUN")

def printUsage():
    print("Usage:")
    print("python3 tp_run.py <ASMFILE>")

if __name__ == "__main__":
    enqueueTarget(tp_run)