import sys
from build.tinyprocbuild import *

RESET_SOURCE_PATH = f"{ROOT}/firmware/rom/src/00000000_reset.asm"
BIOS_SOURCE_PATH = f"{ROOT}/firmware/rom/src/00000100_bios.asm"
RESET_BIN_PATH = f"{ROOT}/firmware/rom/00000000_reset.bin"
BIOS_BIN_PATH = f"{ROOT}/firmware/rom/00000100_bios.bin"
ROM_IMAGE_PATH = f"{ROOT}/firmware/rom/rom_firmware.bin"
TP_WORDSIZE = 4

CONFIG_ASM_VERBOSE = True


def tp_buildrom():
    if (len(sys.argv) > 1):
        log("Supplied more than 0 arguments to buildrom. Ignoring.")

    log("Creating ROM image from source files.")

    log("Assembling reset and bios...")
    cmd_run([DOTNET, DOTNET_EMU_RUN_ARGS, "--assemble", RESET_SOURCE_PATH,
            RESET_BIN_PATH, "--raw", "--verbose" if CONFIG_ASM_VERBOSE else []])
    cmd_run([DOTNET, DOTNET_EMU_RUN_ARGS, "--assemble", BIOS_SOURCE_PATH,
            BIOS_BIN_PATH, "--raw", "--verbose" if CONFIG_ASM_VERBOSE else []])

    log("Merging reset and BIOS into ROM image...")
    binfileReset = open(RESET_BIN_PATH, "rb")
    binfileBios = open(BIOS_BIN_PATH, "rb")
    bindata = bytearray()
    bindata.extend(binfileReset.read())
    while (len(bindata) < TP_WORDSIZE * 0x00000100):
        bindata.append(0x00)
    bindata.extend(binfileBios.read())
    while (len(bindata) < TP_WORDSIZE * 0x00010000):
        bindata.append(0x00)
    binfileReset.close()
    binfileBios.close()

    romfile = open(ROM_IMAGE_PATH, "wb")
    romfile.write(bindata)
    romfile.close()

    log("Image creation successful.")
    target_finish()


def cleanup():
    rmfile(RESET_BIN_PATH)
    rmfile(BIOS_BIN_PATH)


def print_usage():
    print("Usage:")
    print("python3 tp_buildrom.py")


def tp_buildrom_enqueue():
    register_shutdown_hook(cleanup)
    enqueue_target("BUILD-ROM", tp_buildrom)


if __name__ == "__main__":
    tp_buildrom_enqueue()
