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

# SOURCE_FILE_ASM must be user provided
TARGET_FILE_BIN = $(SOURCE_FILE_ASM:.asm=.bin)

.PHONY: assemble emu-run emu-build emu-run-aot gui-run gui-build gui-run-aot

all: emu-run

assemble:
ifndef SOURCE_FILE_ASM
	$(error SOURCE_FILE_ASM not provided. Please re-run using 'make <target> SOURCE_FILE_ASM=<sourceFilePath>' to provide a source assembly file.)
endif
	$(DOTNET) run --project $(SRC_CODE_DIR) --assemble $(SOURCE_FILE_ASM)

run: emu-run
emu-run: assemble
	$(DOTNET) run --project $(SRC_CODE_DIR) --run $(TARGET_FILE_BIN)

emu-build:
	cd $(SRC_CODE_DIR) && $(DOTNET) build
	cd $(SRC_CODE_DIR) && $(DOTNET) publish

run-aot: emu-run-aot
emu-run-aot: emu-build
	$(AOT_COMPILED_EXECUTABLE) --run $(TARGET_FILE_BIN)

gui: gui-run
gui-run:
	$(DOTNET) run --project $(SRC_CODE_DIR_GUI)

gui-build:
	cd $(SRC_CODE_DIR_GUI) && $(DOTNET) build
	cd $(SRC_CODE_DIR_GUI) && $(DOTNET) publish

gui-aot: gui-run-aot
gui-run-aot: gui-build
	$(AOT_COMPILED_GUI)