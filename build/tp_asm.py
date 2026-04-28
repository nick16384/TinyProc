# Assemble a HLTP32 assembly program without running it

import sys
from build.tinyprocbuild import *

TP_WORDSIZE = 4

CONFIG_ASM_VERBOSE = False


def tp_asm():
    if (len(sys.argv) < 2):
        log_err("Error: Supplied less than 1 argument.")
        print_usage()
        buildexit(1)

    sourceFileAsm = sys.argv[-1]
    log(f"Assembling and running source file {sourceFileAsm}")
    cmd_run([
        DOTNET, DOTNET_EMU_RUN_ARGS, "--assemble",
        f"{ROOT}/{sourceFileAsm}",
        "--verbose" if CONFIG_ASM_VERBOSE else []
    ])

    target_finish()


def print_usage():
    print("Usage:")
    print("python3 tp_asm.py <ASMFILE>")


def tp_asm_enqueue():
    enqueue_target("ASM", tp_asm)


if __name__ == "__main__":
    tp_asm_enqueue()
