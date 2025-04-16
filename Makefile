DOTNET = dotnet

SRC_CODE_DIR = src/TinyProc

SOURCE_FILE_ASM = Test\ Programs/HelloWorld.lltp-x25-32.asm
TARGET_FILE_BIN = Test\ Programs/HelloWorld.lltp-x25-32.bin

assemble:
	$(DOTNET) run --project $(SRC_CODE_DIR) --assemble $(SOURCE_FILE_ASM)

run: assemble
	$(DOTNET) run --project $(SRC_CODE_DIR) --run $(TARGET_FILE_BIN)