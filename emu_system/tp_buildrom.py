import sys
from build.tinyprocbuild import *

TEMP_BIN_RESET_PATH = f"{ROOT}/emu_system/rom/00000000_reset.bin"
TEMP_BIN_LOADER_PATH = f"{ROOT}/emu_system/rom/00000100_loader.bin"
ROM_IMAGE_PATH = f"{ROOT}/emu_system/rom/rom_boot.bin"
TP_WORDSIZE = 4

def tp_buildrom():
    if (len(sys.argv) > 1):
        log_err("Error: Supplied more than 0 arguments.")
        printUsage()
        buildexit(1)
    
    global binfileReset, binfileLoader, romfile
    addShutdownHook(cleanup)
    log("Creating ROM image from source files.")

    log("Assembling reset and loader...")
    buildcmd([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/emu_system/rom/src/00000000_reset.asm", f"{ROOT}/emu_system/rom/00000000_reset.bin", "--raw"])
    buildcmd([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/emu_system/rom/src/00000100_loader.asm", f"{ROOT}/emu_system/rom/00000100_loader.bin", "--raw"])

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

    buildexit(0)

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
    tp_buildrom()