import sys
sys.path.append(".") # If launching from ROOT
from build.tinyprocbuild import *
from firmware.rom.tp_buildrom import tp_buildrom_enqueue, ROM_IMAGE_PATH
from tp_asm import tp_asm_enqueue

TP_WORDSIZE = 4

CONFIG_VERBOSE = True

def tp_run():
    if (len(sys.argv) < 2):
        log_err("Error: Supplied less than 1 argument.")
        printUsage()
        buildexit(1)
    
    sourceFileAsm = sys.argv[-1]
    targetFileBin = sourceFileAsm[:-4] + ".bin"
    log(f"Running target file {targetFileBin}")

    cmd_run([DOTNET, DOTNET_EMU_RUN_ARGS, "--run", ROM_IMAGE_PATH, f"{ROOT}/{targetFileBin}", "--verbose" if CONFIG_VERBOSE else []])

    targetFinish()

def printUsage():
    print("Usage:")
    print("python3 tp_run.py <ASMFILE>")

def tp_run_enqueue():
    # Ensure latest version of ROM image
    log("Adding dependency for ROM")
    tp_buildrom_enqueue()
    # Assemble source file
    log("Adding dependency for ASM")
    tp_asm_enqueue()
    enqueueTarget("EMU-RUN", tp_run)

if __name__ == "__main__":
    tp_run_enqueue()