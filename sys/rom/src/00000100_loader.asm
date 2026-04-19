#VERSION 3.1
#ORG 0x00000100

#SECTION .data
equ unloaded_program_source 0x00030000
equ asmheader_offset_version 0   ; Not used by loader
equ asmheader_offset_loadaddress 1
equ asmheader_offset_entrypoint 2
equ asmheader_offset_data_size 3 ; Not used by loader
equ asmheader_offset_text_size 4 ; Not used by loader
equ int_vector_reset 0x0
equ stack_base 0x00020000
equ instruction_size 2

#SECTION (__entry__ = _start) (__adrmodeimplicit__ = absolute) .text
; The program loader assumes that up until this point, the Reset vector has been called and
; it has started to execute this loader.
; It is also assumed that a valid program (including header) has been loaded at address 0x00030000

; After _start, these things will happen in order:
; 1. The .data and .text sections of the unloaded program (starting from 0x00030000) will be copied directly
;    after another to 0x10000000.
; 2. The program's entry point will be calculated and called (NOT jumped to!)
; (3. Once the program returns (via the "ret" instruction), this enters an indefinite halt state / endless jmp loop)

_start:
    jmp  loadProgram

loadProgram:
    mov  gp6, unloaded_program_source ; Load source address
    ld   gp6, [unloaded_program_source + asmheader_offset_data_size]
    ld   gp2, [unloaded_program_source + asmheader_offset_text_size]
    add  gp6, gp2 ; .data size + .text size = total load size
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
    ; Continuing, there is a little issue:
    ; Ideally, all registers should be cleared and the stack reset before the main program runs,
    ; such that it has a clean state to work with.
    ; Clearing the stack is trivial: Just set SP to stack_base and done.
    ; However, ensuring all GPRs (general purpose registers) are cleared is a little more challenging:
    ; GP2 through GP8 may be cleared without issue, but since GP1 contains the target address of the program,
    ; it cannot be cleared, since otherwise the loader doesn't know anymore where to start the program.
    ; The solution is a bit dirty, but works and ensures all registers are indeed zeroed out.
    ; First, inject the value of GP1 into the operand of the JMP instruction responsible for starting the program.
    ; This stores the entry point address in the instruction in memory itself, so GP1 may be cleared now.
    ; Then, GP1 is cleared and the jump executed. Since its operand has been overridden, it now jumps to the program.

    ; Inject entry point address into JMP operand
    st   gp1, [rel +instruction_size * 2 + 1] ; +CALL, +MOV, +1 for operand (we don't want to change the opcode)

    ; Clear all registers
    call clearRegisters
    ; Reset stack
    mov  sp, stack_base

    ; Jump to the loaded program
    jmp  0 ; Will be overridden. If not -> Reset

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

; Self-explanatory function
; Parameters:
; <None>
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