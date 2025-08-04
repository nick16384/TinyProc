#VERSION 3.0
#ENTRY _start

#SECTION (__attribute__ inline = all) .data
; CPU predefined SHIT entries
; Reset and double fault are non-overridable
; 0x00 - 0x1f are non-maskable
immediate SHIT_BASE_OFFSET = 0x00010000
immediate VECTOR_RESET = 0x00
immediate VECTOR_DOUBLE_FAULT = 0x01
immediate VECTOR_UNKNOWN_INSTRUCTION = 0x02
immediate VECTOR_INVALID_ADDRESS = 0x03
immediate VECTOR_STACK_OVERFLOW = 0x04
immediate VECTOR_ILLEGAL_SECURE_MEMORY_WRITE = 0x05
immediate VECTOR_DIVISION_BY_ZERO = 0x06
immediate VECTOR_MANUAL_USER_FAULT = 0x07 ; TODO: Implement

#SECTION (__attribute__ loadaddress = 0x00000000) .text
    _start:
    ; Assuming the hardware has reset up until this point (all registers at 0x0, loader in memory)

    ; Initialize service handler interrupt table (SHIT, similar to the x86 IVT)
    ; An interrupt, which is stored in the SHIT
    ; contains 1 absolute address for where the handler is stored.
    ; One interrupt occupies 1 vector, of which there are 256 (starting at address 0x00010000).
    ; By default, every handler points to the address 0, where the reset code is located, causing a reset loop.
    mov   gp1, 0x00000000
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_RESET)
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_DOUBLE_FAULT)
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_UNKNOWN_INSTRUCTION)
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_INVALID_ADDRESS)
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_STACK_OVERFLOW)
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_ILLEGAL_SECURE_MEMORY_WRITE)
    STORE gp1, (SHIT_BASE_OFFSET + VECTOR_DIVISION_BY_ZERO)

    ; Jump to the loader
    JMP   0x00000100