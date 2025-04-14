DOTNET = "dotnet"

SRC_CODE_DIR = "src/TinyProc"

SOURCE_FILE_ASM = ./Test Programs/HelloWorld.lltp-x25-32.asm
TARGET_FILE_BIN = ./Test Programs/HelloWorld.lltp-x25-32.bin

assemble: $(SOURCE_FILE_ASM)
	cd $(SRC_CODE_DIR)
	$(DOTNET) run --assemble $(SOURCE_FILE_ASM)

run: assemble
	$(DOTNET) run --run $(TARGET_FILE_BIN)