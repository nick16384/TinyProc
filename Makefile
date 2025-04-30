DOTNET = dotnet

SRC_CODE_DIR = src/TinyProc
ifeq ($(OS), Windows_NT)
	AOT_COMPILED_EXECUTABLE = $(SRC_CODE_DIR)/bin/Release/net9.0/TinyProc.exe
else
	AOT_COMPILED_EXECUTABLE = $(SRC_CODE_DIR)/bin/Release/net9.0/TinyProc
endif

SOURCE_FILE_ASM = "Test Programs/ASMv2/Alphabet.lltp32.asm"
TARGET_FILE_BIN = "Test Programs/ASMv2/Alphabet.lltp32.bin"

assemble:
	$(DOTNET) run --project $(SRC_CODE_DIR) --assemble $(SOURCE_FILE_ASM)

run: assemble
	$(DOTNET) run --project $(SRC_CODE_DIR) --run $(TARGET_FILE_BIN)

build:
	cd $(SRC_CODE_DIR) && $(DOTNET) build
	cd $(SRC_CODE_DIR) && $(DOTNET) publish

assemble-aot: build
	$(AOT_COMPILED_EXECUTABLE) --assemble $(SOURCE_FILE_ASM)

run-aot: build
	$(AOT_COMPILED_EXECUTABLE) --run $(TARGET_FILE_BIN)