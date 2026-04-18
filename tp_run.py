import sys
from build.tinyprocbuild import *
from emu_system.tp_buildrom import tp_buildrom

TP_WORDSIZE = 4

def tp_run():
    if (len(sys.argv) < 2):
        log_err("Error: Supplied less than 1 argument.")
        printUsage()
        buildexit(1)
    
    sourceFileAsm = sys.argv[1]
    log(f"Assembling and running source file {sourceFileAsm}")
    buildcmd([DOTNET, DOTNET_RUN_ARGS, "--assemble", f"{ROOT}/{sourceFileAsm}"])

    # Ensure latest version of ROM image
    sys.argv = sys.argv[:-1]
    # TODO: Prevent this from exiting early (do some kind of wrapping in tinyprocbuild)
    tp_buildrom()

    targetFileBin = sourceFileAsm[:-4]
    buildcmd([DOTNET, DOTNET_RUN_ARGS, "--run", f"{ROOT}/{targetFileBin}.bin"])

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

def printUsage():
    print("Usage:")
    print("python3 tp_run.py <ASMFILE>")

if __name__ == "__main__":
    tp_run()