SRC_CODE_DIR = src/TinyProc
SRC_CODE_DIR_GUI = src/Avalonia/TinyProcVisualizer

ifeq ($(OS), Windows_NT)
	DOTNET = dotnet.exe
	AOT_COMPILED_EXECUTABLE = $(SRC_CODE_DIR)/bin/Release/net9.0/TinyProc.exe
	AOT_COMPILED_GUI = $(SRC_CODE_DIR_GUI)/bin/Release/net9.0/TinyProcVisualizer.exe
else
	DOTNET = dotnet
	AOT_COMPILED_EXECUTABLE = $(SRC_CODE_DIR)/bin/Release/net9.0/TinyProc
	AOT_COMPILED_GUI = $(SRC_CODE_DIR_GUI)/bin/Release/net9.0/TinyProcVisualizer
endif

SOURCE_FILE_ASM = "Test Programs/ASMv3/ASM3Test.hltp32.asm"
TARGET_FILE_BIN = "Test Programs/ASMv3/ASM3Test.hltp32.bin"

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

gui:
	$(DOTNET) run --project $(SRC_CODE_DIR_GUI)

build-gui:
	cd $(SRC_CODE_DIR_GUI) && $(DOTNET) build
	cd $(SRC_CODE_DIR_GUI) && $(DOTNET) publish

gui-aot: build-gui
	$(AOT_COMPILED_GUI)