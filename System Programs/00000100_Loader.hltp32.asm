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
immediate int_vector_reset = 0x0

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
        ald  gp1, (unloaded_program_source + progheader_offset_data_addr)
        sub  gp1, 0
        bzr  dataLoadAddressIsZero
        jmp  dataLoadAddressIsNotZero
    
    ; The data section load address is zero. This means it is relocatable.
    ; For simplicity, copy it to the default address 0x10000000
    dataLoadAddressIsZero:
        mov  gp6, (unloaded_program_source + progheader_offset_data_addr)
        ald  gp7, (unloaded_program_source + progheader_offset_data_size)
        mov  gp8, data_fixed_load_address
        call copySection
        jmp  dataSectionCopyFinished

    ; The data section load address is NOT zero. This means it has to be loaded at a specified address.
    ; Since the load address is specified, load it it that address.
    dataLoadAddressIsNotZero:
        mov  gp6, (unloaded_program_source + progheader_offset_data_addr)
        ald  gp7, (unloaded_program_source + progheader_offset_data_size)
        ald  gp8, (unloaded_program_source + progheader_offset_data_addr)
        call copySection
        jmp  dataSectionCopyFinished
    
    dataSectionCopyFinished:
        ; Check whether .text section load address is 0 or not
        ald  gp1, (unloaded_program_source + progheader_offset_text_addr)
        sub  gp1, 0
        bzr  textLoadAddressIsZero
        jmp  textLoadAddressIsNotZero
        
    ; The text section load address is zero. This means it is relocatable.
    ; For simplicity, copy it to the default address 0x20000000
    textLoadAddressIsZero:
        ; No need for relocating memory addresses, since they have been set by the assembler,
        ; because they are fixed.
        mov   gp6, (unloaded_program_source + progheader_offset_text_addr)
        ald   gp7, (unloaded_program_source + progheader_offset_text_size)
        mov   gp8, text_fixed_load_address
        call  copySection
        call  clearRegisters
        acall text_fixed_load_address ; Start the actual program
        jmp   programReturned

    ; The text section load address is NOT zero. This means it has to be loaded at a specified address.
    ; Since the load address is specified, load it it that address.
    textLoadAddressIsNotZero:
        mov   gp6, (unloaded_program_source + progheader_offset_text_addr)
        ald   gp7, (unloaded_program_source + progheader_offset_text_size)
        ald   gp8, (unloaded_program_source + progheader_offset_text_addr)
        call  copySection
        call  clearRegisters
        acall (unloaded_program_source + progheader_offset_text_addr) ; Start the actual program
        jmp   programReturned

    ; Copies a section of memory to another section.
    ; Parameters:
    ; GP6: Source address
    ; GP7: Number of words to copy
    ; GP8: Destination address
    ; Registers modified:
    ; GP1, GP6, GP7, GP8
    copySection:
        aldr  gp1, gp6
        astrr gp1, gp8
        inc   gp6
        inc   gp8
        dec   gp7
        intng int_vector_reset ; If the program is empty, the decrement operation results in an underflow. This is not correct: Trigger reset interrupt
        mov   gp7, gp7
        bnz   copySection
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
        cla ; Effectively clears SR
        ret
    
    ; Call this when the main program has used the "ret" instruction and gave back control to the loader.
    programReturned:
        jmp   _halt
    
    _halt:
        jmp   _halt