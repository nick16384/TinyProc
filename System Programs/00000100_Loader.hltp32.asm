#VERSION 3.0
#ENTRY _start

#SECTION (__attribute__ inline = all) .data
immediate unloaded_program_source = 0x00030000
immediate progheader_offset_version = 0 ; The version attribute is technically unnecessary, but is kept for simplicity.
immediate progheader_offset_entrypoint = 1
immediate progheader_offset_data_addr = 2
immediate progheader_offset_data_size = 3
immediate progheader_offset_text_addr = 4
immediate progheader_offset_text_size = 5
immediate data_fixed_load_address = 0x10000000
immediate text_fixed_load_address = 0x20000000
; The operands of these opcodes need to be modified for their new memory location:
immediate opcode_JMP = 0x01
immediate opcode_B = 0x02
immediate opcode_LOAD = 0x30
immediate opcode_LOADR = 0x31
immediate opcode_STORE = 0x32
immediate opcode_STORR = 0x33

#SECTION (__attribute__ loadaddress = 0x00000100) .text
    ; The program loader assumes that up until this point, the Reset vector has been called and
    ; it has started to execute this loader.
    ; It is also assumed that a valid program (including header) has been loaded at address 0x00030000

    ; After _start, these things will happen in order:
    ; 1. The .data section is loaded from memory address 0x00030000 into a location it specifies or 0x10000000
    ; 2. The .text section is loaded from memory address 0x00030000 into a location it specifies or 0x20000000
    ; 3. The program's entry point will be calculated and called (NOT jumped to!)
    ; (4. Once the program returns (via the "ret" instruction), this forces a hardware reset)

    _start:
        ; Check whether .data section load address is 0 or not
        load gp1, (unloaded_program_source + progheader_offset_data_addr)
        sub  gp1, 0
        bzr  dataLoadAddressIsZero
        jmp  dataLoadAddressIsNotZero
    
    ; The data section load address is zero. This means it is relocatable.
    ; For simplicity, copy it to the default address 0x10000000
    dataLoadAddressIsZero:
        mov  gp6, (unloaded_program_source + progheader_offset_data_addr)
        load gp7, (unloaded_program_source + progheader_offset_data_size)
        mov  gp8, data_fixed_load_address
        call copySection
        jmp  dataSectionCopyFinished

    ; The data section load address is NOT zero. This means it has to be loaded at a specified address.
    ; Since the load address is specified, load it it that address.
    dataLoadAddressIsNotZero:
        mov  gp6, (unloaded_program_source + progheader_offset_data_addr)
        load gp7, (unloaded_program_source + progheader_offset_data_size)
        load gp8, (unloaded_program_source + progheader_offset_data_addr)
        call copySection
        jmp  dataSectionCopyFinished
    
    dataSectionCopyFinished:
        ; Check whether .text section load address is 0 or not
        load gp1, (unloaded_program_source + progheader_offset_text_addr)
        sub  gp1, 0
        bzr  textLoadAddressIsZero
        jmp  textLoadAddressIsNotZero
        
    ; The text section load address is zero. This means it is relocatable.
    ; For simplicity, copy it to the default address 0x20000000
    textLoadAddressIsZero:
        ; No need for relocating memory addresses, since they have been set by the assembler,
        ; because they are fixed.
        mov  gp6, (unloaded_program_source + progheader_offset_text_addr)
        load gp7, (unloaded_program_source + progheader_offset_text_size)
        mov  gp8, text_fixed_load_address
        call copySection
        call clearRegisters
        call text_fixed_load_address ; Start the actual program
        jmp  programReturned

    ; The text section load address is NOT zero. This means it has to be loaded at a specified address.
    ; Since the load address is specified, load it it that address.
    textLoadAddressIsNotZero:
        mov  gp6, (unloaded_program_source + progheader_offset_text_addr)
        load gp7, (unloaded_program_source + progheader_offset_text_size)
        load gp8, (unloaded_program_source + progheader_offset_text_addr)
        call adjustRelocatableAddresses
        mov  gp6, (unloaded_program_source + progheader_offset_text_addr)
        load gp7, (unloaded_program_source + progheader_offset_text_size)
        load gp8, (unloaded_program_source + progheader_offset_text_addr)
        call copySection
        call clearRegisters
        call (unloaded_program_source + progheader_offset_text_addr) ; Start the actual program
        jmp  programReturned

    ; Copies a section of memory to another section.
    ; Parameters:
    ; GP6: Source address
    ; GP7: Number of words to copy
    ; GP8: Destination address
    ; Registers modified:
    ; GP1, GP6, GP7, GP8
    copySection:
        loadr gp1, gp6
        storr gp1, gp8
        inc   gp6
        inc   gp8
        dec   gp7
        intng 0xfffffeff ; If the program is empty, the decrement operation results in an underflow. This is not correct: Trigger reset interrupt
        mov   gp7, gp7
        bnz   copySection
        ret
    
    ; Before copying the .text section to its load address, this function takes
    ; care of changing all references to memory in the .text section to the new load address.
    ; Parameters:
    ; GP6: .text section start (before copy)
    ; GP7: .text section size
    ; GP8: .text section new load address
    ; Registers modified:
    ; GP1, GP6, GP7
    adjustRelocatableAddresses:
        loadr gp1, gp6 ; Load the first word of the instruction into GP1

        ; If opcode is a memory instruction, modify memory at (gp6 + 1) accordingly (2nd word of instruction / memory address)
        ; FIXME: Instruction "RS" not implemented yet
        ; rs    gp1, (32 - 6) ; gp1 = gp1 >> 28 (Extract opcode from instruction)
        ; Every load / store instruction needs an offset to the .data section
        ; Every jump instruction needs an offset to the .text section
        sub   gp1, opcode_JMP ; Is gp1 a jump instruction?
        bzr   adjustRelocatableAddresses_opcodeIsJump
        sub   gp1, opcode_B ; Is gp1 a branch instruction?
        bzr   adjustRelocatableAddresses_opcodeIsJump
        sub   gp1, opcode_LOAD ; Is gp1 a LOAD instruction?
        bzr   adjustRelocatableAddresses_opcodeIsLoadOrStore
        sub   gp1, opcode_LOADR ; Is gp1 a LOADR instruction?
        bzr   adjustRelocatableAddresses_opcodeIsLoadOrStore
        sub   gp1, opcode_STORE ; Is gp1 a STORE instruction?
        bzr   adjustRelocatableAddresses_opcodeIsLoadOrStore
        sub   gp1, opcode_STORR ; Is gp1 a STORR instruction?
        bzr   adjustRelocatableAddresses_opcodeIsLoadOrStore

        ; All of the above cases do not apply (The instruction is not a memory affecting instruction)
        jmp   adjustRelocatableAddresses_opcodeDoesNotAffectMemory

        adjustRelocatableAddresses_opcodeIsLoadOrStore:
            inc   gp6
            loadr gp1, gp6 ; gp1 = [gp6 + 1]
            add   gp1, data_fixed_load_address ; Add the .data section load address offset and store back the instruction
            storr gp1, gp6
            jmp   adjustRelocatableAddresses_end
        
        adjustRelocatableAddresses_opcodeIsJump:
            inc   gp6
            loadr gp1, gp6 ; gp1 = [gp6 + 1]
            add   gp1, text_fixed_load_address ; Add the .text section load address offset and store back the instruction
            storr gp1, gp6
            jmp   adjustRelocatableAddresses_end

        adjustRelocatableAddresses_opcodeDoesNotAffectMemory:
            inc   gp6 ; Only increment to not mess up with decrement later
            jmp   adjustRelocatableAddresses_end

        adjustRelocatableAddresses_end:
            dec   gp6 ; Decrement from previous increment to 2nd instruction word
            ; Increment to next instruction:
            add   gp6, 2
            sub   gp7, 2
            ; If zero or negative, return
            bzr   adjustRelocatableAddresses_ret
            bnn   adjustRelocatableAddresses
    
        adjustRelocatableAddresses_ret:
            ret
    
    ; Self-explanatory function
    clearRegisters:
        mov   gp1, 0
        mov   gp2, 0
        mov   gp3, 0
        mov   gp4, 0
        mov   gp5, 0
        mov   gp6, 0
        mov   gp7, 0
        mov   gp8, 0
        ret
    
    ; Call this when the main program has used the "ret" instruction and gave back control to the loader.
    programReturned:
        int   0 ; Reset