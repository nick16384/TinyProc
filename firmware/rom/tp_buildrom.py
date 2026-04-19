import sys
sys.path.append(".") # If launching from ROOT
from build.tinyprocbuild import *

RESET_SOURCE_PATH  = f"{ROOT}/firmware/rom/src/00000000_reset.asm"
LOADER_SOURCE_PATH = f"{ROOT}/firmware/rom/src/00000100_loader.asm"
RESET_BIN_PATH     = f"{ROOT}/firmware/rom/00000000_reset.bin"
LOADER_BIN_PATH    = f"{ROOT}/firmware/rom/00000100_loader.bin"
ROM_IMAGE_PATH     = f"{ROOT}/firmware/rom/rom_firmware.bin"
TP_WORDSIZE = 4

CONFIG_ASM_VERBOSE = True

def tp_buildrom():
    if (len(sys.argv) > 1):
        log("Supplied more than 0 arguments to buildrom. Ignoring.")

    log("Creating ROM image from source files.")

    log("Assembling reset and loader...")
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--assemble", RESET_SOURCE_PATH, RESET_BIN_PATH, "--raw", "--verbose" if CONFIG_ASM_VERBOSE else []])
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--assemble", LOADER_SOURCE_PATH, LOADER_BIN_PATH, "--raw", "--verbose" if CONFIG_ASM_VERBOSE else []])

    log("Merging reset and loader into ROM image...")
    binfileReset = open(RESET_BIN_PATH, "rb")
    binfileLoader = open(LOADER_BIN_PATH, "rb")
    bindata = bytearray()
    bindata.extend(binfileReset.read())
    while (len(bindata) < TP_WORDSIZE * 0x00000100):
        bindata.append(0x00)
    bindata.extend(binfileLoader.read())
    while (len(bindata) < TP_WORDSIZE * 0x00010000):
        bindata.append(0x00)
    binfileReset.close()
    binfileLoader.close()
    
    romfile = open(ROM_IMAGE_PATH, "wb")
    romfile.write(bindata)
    romfile.close()

    log("Image creation successful.")
    targetFinish()

def cleanup():
    rmfile(RESET_BIN_PATH)
    rmfile(LOADER_BIN_PATH)

def printUsage():
    print("Usage:")
    print("python3 tp_buildrom.py")

def tp_buildrom_enqueue():
    addShutdownHook(cleanup)
    enqueueTarget("BUILD-ROM", tp_buildrom)

if __name__ == "__main__":
    tp_buildrom_enqueue()