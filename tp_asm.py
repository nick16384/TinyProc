import sys
sys.path.append(".") # If launching from ROOT
from build.tinyprocbuild import *

TP_WORDSIZE = 4

CONFIG_ASM_VERBOSE = False

def tp_asm():
    if (len(sys.argv) < 2):
        log_err("Error: Supplied less than 1 argument.")
        printUsage()
        buildexit(1)
    
    sourceFileAsm = sys.argv[-1]
    log(f"Assembling and running source file {sourceFileAsm}")
    cmd_run([
        DOTNET, DOTNET_EMU_RUN_ARGS, "--assemble",
        f"{ROOT}/{sourceFileAsm}",
        "--verbose" if CONFIG_ASM_VERBOSE else []
        ])

    targetFinish()

def printUsage():
    print("Usage:")
    print("python3 tp_asm.py <ASMFILE>")

def tp_asm_enqueue():
    enqueueTarget("ASM", tp_asm)

if __name__ == "__main__":
    tp_asm_enqueue()