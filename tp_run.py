import sys
sys.path.append(".") # If launching from ROOT
from build.tinyprocbuild import *
from firmware.rom.tp_buildrom import tp_buildrom_enqueue, ROM_IMAGE_PATH

TP_WORDSIZE = 4

CONFIG_ASM_VERBOSE = False
CONFIG_VERBOSE = True

def tp_run():
    if (len(sys.argv) < 2):
        log_err("Error: Supplied less than 1 argument.")
        printUsage()
        buildexit(1)
    
    sourceFileAsm = sys.argv.pop()
    log(f"Assembling and running source file {sourceFileAsm}")
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/{sourceFileAsm}", "--verbose" if CONFIG_ASM_VERBOSE else []])

    targetFileBin = sourceFileAsm[:-4]
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--run", ROM_IMAGE_PATH, f"{ROOT}/{targetFileBin}.bin", "--verbose" if CONFIG_VERBOSE else []])

    targetFinish()

def printUsage():
    print("Usage:")
    print("python3 tp_run.py <ASMFILE>")

def tp_run_enqueue():
    # Ensure latest version of ROM image
    log("Adding dependency for ROM")
    tp_buildrom_enqueue()
    enqueueTarget("EMU-RUN", tp_run)

if __name__ == "__main__":
    tp_run_enqueue()