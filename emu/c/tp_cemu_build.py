# Build a binary executable for the C emulator

import sys
sys.path.append(".") # If launching from ROOT
from build.tinyprocbuild import *

CC_DEFAULT = "<NO WINDOWS CC>" if PLATFORM == "Windows" else "gcc"
CFLAGS = ["-Wall", "-Wextra", "-O2"]

MAIN_SOURCE_FILE_C = f"{ROOT}/emu/c/main.c"
MAIN_EXECUTABLE_FILE = f"{ROOT}/emu/c/main.exe" if PLATFORM == "Windows" else f"{ROOT}/emu/c/main"

def tp_cemu_build():
    # Cannot reassign constant CC_DEFAULT, therefore, the actual CC resides here
    CC = CC_DEFAULT
    if (PLATFORM == "Windows" and len(sys.argv) <= 1):
        builderror(1, "C compiler for Windows not available by default. Specify the location in the CLI.")
    if (len(sys.argv) >= 2):
        CC = sys.argv[-1]
        # maybe use getopt.getopt for CC (i.e. "--cc gcc")
        log(f"Using user-provided C compiler \"{CC}\"")
        
    cmd_run([CC, CFLAGS, MAIN_SOURCE_FILE_C, "-o", MAIN_EXECUTABLE_FILE])

    target_finish()

def print_usage():
    print("Usage:")
    print("python3 tp_cemu_build.py [<CC>]")

def tp_cemu_build_enqueue():
    enqueue_target("CEMU-BUILD", tp_cemu_build)

if __name__ == "__main__":
    tp_cemu_build_enqueue()