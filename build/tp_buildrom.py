import sys
from build.tinyprocbuild import *

TEMP_BIN_RESET_PATH = f"{ROOT}/sys/rom/00000000_reset.bin"
TEMP_BIN_LOADER_PATH = f"{ROOT}/sys/rom/00000100_loader.bin"
ROM_IMAGE_PATH = f"{ROOT}/sys/rom/rom_boot.bin"
TP_WORDSIZE = 4

def tp_buildrom():
    if (len(sys.argv) > 1):
        printUsage()
        builderror(1, "Supplied more than 0 arguments.")
    
    global binfileReset, binfileLoader, romfile
    addShutdownHook(cleanup)

    log("Creating ROM image from source files.")

    log("Assembling reset and loader...")
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/sys/rom/src/00000000_reset.asm", TEMP_BIN_RESET_PATH, "--raw"])
    cmd_run([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/sys/rom/src/00000100_loader.asm", TEMP_BIN_LOADER_PATH, "--raw"])

    log("Merging reset and loader into ROM image...")
    binfileReset = open(TEMP_BIN_RESET_PATH, "rb")
    binfileLoader = open(TEMP_BIN_LOADER_PATH, "rb")
    bindata = bytearray()
    bindata.extend(binfileReset.read())
    while (len(bindata) < TP_WORDSIZE * 0x00000100):
        bindata.append(0x00)
    bindata.extend(binfileLoader.read())
    while (len(bindata) < TP_WORDSIZE * 0x00010000):
        bindata.append(0x00)
    
    romfile = open(ROM_IMAGE_PATH, "wb")
    romfile.write(bindata)

    log("Image creation successful.")
    targetFinish("BUILD ROM")

def cleanup():
    binfileReset.close()
    binfileLoader.close()
    romfile.close()
    rmfile(TEMP_BIN_RESET_PATH)
    rmfile(TEMP_BIN_LOADER_PATH)

def printUsage():
    print("Usage:")
    print("python3 tp_buildrom.py")

if __name__ == "__main__":
    enqueueTarget(tp_buildrom)