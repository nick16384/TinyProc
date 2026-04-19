#VERSION 3.1
#ORG 0x00000100

#SECTION .data
equ unloaded_program_source 0x00030000
equ asmheader_offset_version 0   ; Not used by loader
equ asmheader_offset_loadaddress 1
equ asmheader_offset_entrypoint 2
equ asmheader_offset_data_size 3 ; Not used by loader
equ asmheader_offset_text_size 4 ; Not used by loader
equ asmheader_size 8
equ int_vector_reset 0x0
equ stack_base 0x00020000
equ instruction_size 2

#SECTION (__entry__ = _start) (__adrmodeimplicit__ = absolute) .text
; The program loader assumes that up until this point, the Reset vector has been called and
; it has started to execute this loader.
; It is also assumed that a valid program (including header) has been loaded at address 0x00030000

; After _start, these things will happen in order:
; 1. The global load address for the target program is determined.
; 2. The entire target program is loaded at (i.e. copied to) its load address.
; 3. The entry point is calculated, the stack pointer reset and all register cleared.
; 4. The loaded jumps to the entry point.

_start:
    jmp  loadProgram

loadProgram:
    mov  gp6, unloaded_program_source + asmheader_size ; Load source address
    ld   gp7, [unloaded_program_source + asmheader_offset_data_size]
    ld   gp2, [unloaded_program_source + asmheader_offset_text_size]
    add  gp7, gp2 ; .data size + .text size = total load size
    ld   gp8, [unloaded_program_source + asmheader_offset_loadaddress] ; Load destination address
    call copySection
    jmp startLoadedProgram

startLoadedProgram:
    ; Calculate entry point address:
    ; entry_point_address = program_load_address + data_size + entry_point_text_offset
    ld   gp1, [unloaded_program_source + asmheader_offset_loadaddress]
    ld   gp2, [unloaded_program_source + asmheader_offset_data_size]
    add  gp1, gp2
    ld   gp2, [unloaded_program_source + asmheader_offset_entrypoint]
    add  gp1, gp2
    ; GP1 now contains the program's entry point address

    ; Clear all registers
    mov  gp2, 0
    mov  gp3, 0
    mov  gp4, 0
    mov  gp5, 0
    mov  gp6, 0
    mov  gp7, 0
    mov  gp8, 0
    cla ; Effectively clears SR
    ; Reset stack
    mov  sp, stack_base

    ; Jump to the loaded program
    jmp  gp1

; Copies a section of memory to another section.
; Parameters:
; GP6: Source address
; GP7: Number of words to copy
; GP8: Destination address
; Registers modified:
; GP1, GP6, GP7, GP8, SR
copySection:
    ld    gp1, [gp6]
    st    gp1, [gp8]
    inc   gp6
    inc   gp8
    dec   gp7
    intng int_vector_reset ; If the program is empty, the decrement operation results in an underflow. This is not correct: Trigger reset interrupt
    cmp   gp7, 0
    bnz   copySection
    ret