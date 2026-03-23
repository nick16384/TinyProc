#VERSION 3.0
#ENTRY _start

#SECTION (__attribute__ inline = all) .data
imm unloaded_program_source = 0x00030000
imm asmheader_offset_version = 0 ; The version attribute is technically unnecessary, but is kept for clarity.
imm asmheader_offset_entrypoint = 1
imm asmheader_offset_data_addr = 2
imm asmheader_offset_data_size = 3
imm asmheader_offset_text_addr = 4
imm asmheader_offset_text_size = 5
imm asmheader_size = 8
imm reloc_default_load_address = 0x10000000
imm int_vector_reset = 0x0
imm sp_base_address = 0x00020000

#SECTION (__attribute__ loadaddress = 0x00000100) .text
    ; The program loader assumes that up until this point, the Reset vector has been called and
    ; it has started to execute this loader.
    ; It is also assumed that a valid program (including header) has been loaded at address 0x00030000

    ; After _start, these things will happen in order:
    ; 1. The .data and .text sections of the unloaded program (starting from 0x00030000) will be copied directly
    ;    after another to 0x10000000.
    ; 2. The program's entry point will be calculated and called (NOT jumped to!)
    ; (3. Once the program returns (via the "ret" instruction), this enters an indefinite halt state / endless jmp loop)

    _start:
        ; Check whether .data section load address is 0 or not
        ld   gp1, [(unloaded_program_source + asmheader_offset_data_addr)]
        bzr  [dataIsRelocatable]
        jmp  [dataIsFixed]
    
    ; The data section load address is zero. This means it is relocatable.
    dataIsRelocatable:
        mov  gp6, (unloaded_program_source + asmheader_size)
        ld   gp7, [(unloaded_program_source + asmheader_offset_data_size)]
        mov  gp8, reloc_default_load_address
        call [copySection]
        jmp  [dataSectionCopyFinished]

    ; The data section load address is NOT zero. This means it has to be loaded at a specified address.
    ; Since the load address is specified, load it it that address.
    dataIsFixed:
        mov  gp6, (unloaded_program_source + asmheader_size)
        ld   gp7, [(unloaded_program_source + asmheader_offset_data_size)]
        ld   gp8, [(unloaded_program_source + asmheader_offset_data_addr)]
        call [copySection]
        jmp  [dataSectionCopyFinished]
    
    dataSectionCopyFinished:
        ; Check whether .text section load address is 0 or not
        ld   gp1, [(unloaded_program_source + asmheader_offset_text_addr)]
        bzr  [textIsRelocatable]
        jmp  [textIsFixed]
        
    ; The text section load address is zero. This means it is relocatable.
    textIsRelocatable:
        ; No need for relocating memory addresses, since they have been set by the assembler,
        ; because they are fixed.
        ; Determine destination for .text section (relocbase + [.data size])
        mov   gp1, reloc_default_load_address
        ld    gp2, [(unloaded_program_source + asmheader_offset_data_size)]
        add   gp1, gp2
        mov   gp8, gp1
        ; Determine source of .text section (unloadedbase + header size + [.data size])
        mov   gp1, (unloaded_program_source + asmheader_size)
        ld    gp2, [(unloaded_program_source + asmheader_offset_data_size)]
        add   gp1, gp2
        mov   gp6, gp1
        ; Call copy for the .text section
        ; Source and destination address (GP6 / GP8 set already)
        ld    gp7, [(unloaded_program_source + asmheader_offset_text_size)]
        call  [copySection]
        ; Calculate entry point (base + .data size + asm entry point)
        mov   gp1, reloc_default_load_address
        ld    gp2, [(unloaded_program_source + asmheader_offset_data_size)]
        ld    gp3, [(unloaded_program_source + asmheader_offset_entrypoint)]
        add   gp1, gp2
        add   gp1, gp3
        call  [gp1] ; Start the actual program
        jmp   [programReturned]

    ; The text section load address is NOT zero. This means it has to be loaded at a specified address.
    ; Since the load address is specified, load it it that address.
    textIsFixed:
        mov   gp6, (unloaded_program_source + asmheader_offset_text_addr)
        ld    gp7, [(unloaded_program_source + asmheader_offset_text_size)]
        ld    gp8, [(unloaded_program_source + asmheader_offset_text_addr)]
        call  [copySection]
        call  [clearRegisters]
        call  [(unloaded_program_source + asmheader_offset_text_addr)] ; Start the actual program
        jmp   [programReturned]

    ; Copies a section of memory to another section.
    ; Parameters:
    ; GP6: Source address
    ; GP7: Number of words to copy
    ; GP8: Destination address
    ; Registers modified:
    ; GP1, GP6, GP7, GP8
    copySection:
        ld    gp1, [gp6]
        st    gp1, [gp8]
        inc   gp6
        inc   gp8
        dec   gp7
        intng int_vector_reset ; If the program is empty, the decrement operation results in an underflow. This is not correct: Trigger reset interrupt
        cmp   gp7, 0
        bnz   [copySection]
        ret
    
    ; Self-explanatory function
    ; Parameters: None
    ; Registers modified:
    ; GP1 - GP8, SR
    clearRegisters:
        mov   gp1, 0
        mov   gp2, 0
        mov   gp3, 0
        mov   gp4, 0
        mov   gp5, 0
        mov   gp6, 0
        mov   gp7, 0
        mov   gp8, 0
        cla ; Effectively clears SR
        ret
    
    ; Call this when the main program has used the "ret" instruction and gave back control to the loader.
    ; Clears the stack, all registers, and resumes indefinitely in a halt loop.
    programReturned:
        ; Clear the stack
        pop   gp2
        cmp   sp, sp_base_address
        bnz   [programReturned]
        ; Clear registers
        call  [clearRegisters]
        jmp   [_halt]
    
    _halt:
        jmp   [_halt]