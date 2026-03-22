#VERSION 3.0
#ENTRY _start

#SECTION (__attribute__ inline = all) .data
; CPU predefined SHIT entries
; Reset and double fault are non-overridable
; 0x00 - 0x1f are non-maskable
imm SHIT_BASE_OFFSET = 0x00010000
imm VECTOR_RESET = 0x00
imm VECTOR_DOUBLE_FAULT = 0x01
imm VECTOR_UNKNOWN_INSTRUCTION = 0x02
imm VECTOR_INVALID_ADDRESS = 0x03
imm VECTOR_STACK_OVERFLOW = 0x04
imm VECTOR_ILLEGAL_SECURE_MEMORY_WRITE = 0x05
imm VECTOR_DIVISION_BY_ZERO = 0x06
imm VECTOR_MANUAL_USER_FAULT = 0x07 ; TODO: Implement
imm STACK_BASE = 0x00020000

#SECTION (__attribute__ loadaddress = 0x00000000) .text
    _start:
    ; Assuming the hardware has reset up until this point (all registers at 0x0, loader in memory)

    ; Initialize service handler interrupt table (SHIT, similar to the x86 IVT)
    ; An interrupt, which is ASTR d in the SHIT
    ; contains 1 absolute address for where the handler is ASTR d.
    ; One interrupt occupies 1 vector, of which there are 256 (starting at address 0x00010000).
    ; By default, every handler points to the address 0, where the reset code is located, causing a reset loop.
    MOV   gp1, 0x00000000
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_RESET)]
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_DOUBLE_FAULT)]
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_UNKNOWN_INSTRUCTION)]
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_INVALID_ADDRESS)]
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_STACK_OVERFLOW)]
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_ILLEGAL_SECURE_MEMORY_WRITE)]
    ST    gp1, [(SHIT_BASE_OFFSET + VECTOR_DIVISION_BY_ZERO)]

    ; Save the SHIT base offset at address 0xFF
    MOV   gp1, SHIT_BASE_OFFSET
    ST    gp1, [0x00010100]

    ; Set, then clear all flags
    TST
    CLA

    MOV   sp, STACK_BASE

    ; Jump to the loader
    JMP   [0x00000100]