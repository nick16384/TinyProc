; This is a test file for both the ASMv3.1 assembler, as well as the CPU in revision 0.3-indev
; Doesn't serve a distinct purpose except debugging.
#VERSION 3.1
#ORG 10000000h

#DEFINE PTR_TEST_DATA 0b1, 56h, 0x56, 56, "This is a test string", 0xA, "Another test string", 0
#DEFINE NULL 0
#DEFINE LD_DATA_ADDR 0x20202020
#DEFINE SP_BASE 0x00020000
#DEFINE VECTOR_RESET 0x00000000

#SECTION .data
; Single words
imm1: dw 0x39393939
dw 39393939h
dw 0b01010101
dw 39393939
equ e1 0x39393939
equ e2 39393939h
equ e3 0b01010101
equ e4 39393939
; Multi-words
ptr1: dw $PTR_TEST_DATA
dw $PTR_TEST_DATA
; Length
dw len: ptr1
equ ptr1_l2 len: ptr1
len_ptr1: dw len: ptr1
; Times
times 4 dw 0x0
ptrreptest: times 2 dw $PTR_TEST_DATA
times 4 dw 1

#SECTION (__entry__ = _start) (__adrmodeimplicit__ = absolute) .text
start_actual:
    ; Basic arithmetic
    ; Some mnemonics are commented out, since they haven't been implemented yet
    ; Register-Immediate
    mov gp1, imm1
    add gp1, 25
    sub gp1, 55
    ;xor gp1, 0b10101010_10101010_10101010_10101010
    or  gp1, 0x70
    or  gp1, 70h
    and gp1, 70
    ; Register-Register
    ;or gp1, gp1 ; Zero out gp1
    ;not gp1
    or  gp2, 125
    add gp1, gp2
    sub gp2, gp1
    ;xor gp1, gp2
    and gp1, 70
    inc gp1
    inc gp1
    dec gp1
    mov gp8, gp1
    mov gp1, gp8
    mov gp1, $NULL
    mov gp2, $NULL
    mov gp8, $NULL
    ; Compare & conditional test
    mov gp1, 0
    mov gp2, 0
    cmp gp1, 0
    cmp gp1, gp2
    ; Test without actually doing anything
    nopzr
    nopnz
    nopng
    nopnn
    nopof
    nopno
    jmp 0 ; Continue

    ; Memory operations
    mov gp1, 90
    ; Store
    st gp1, [e1 + 5]
    st gp1, [imm1]
    st gp1, [imm1 + 1] ; *imm2
    st gp1, [0x40000000 * 2 - 50]
    st gp1, [0x40000000 * 4 + 0x50000000] ; Check overflow handling
    st gp1, [0 - 0x80000000] ; Check overflow handling 2
    st gp1, [abs 40000000h]
    st gp1, [abs 40000000h + 1]
    st gp1, [rel +40000000h]
    st gp1, [rel -40000000h]
    st gp1, [rel +40000000h + 2]
    st gp1, [rel -40000000h + 2]
    st gp1, [$LD_DATA_ADDR]
    st gp1, [rel +$LD_DATA_ADDR]
    st gp1, [rel -$LD_DATA_ADDR]
    ; Load
    ld gp2, [e1 + 5]
    ld gp3, [imm1]
    ld gp4, [imm1 + 1]
    ld gp5, [0x40000000]
    ld gp6, [rel +40000000h]
    ld gp7, [rel -40000000h]
    ld gp8, [$LD_DATA_ADDR]
    ld gp1, [rel +$LD_DATA_ADDR]
    ld gp2, [rel -$LD_DATA_ADDR]
    ; Stack
    push gp1
    push gp2
    push gp3
    push gp4
    push gp5
    push gp6
    push gp7
    push gp8
    mov sp, $SP_BASE + 20
    pop gp1
    pop gp2
    pop gp3
    pop gp4
    pop gp5
    pop gp6
    pop gp7
    pop gp8
    mov sp, $SP_BASE + 50
    ; Test lowest and highest address
    ld gp1, [0] ; First reset instruction (should not be zero)
    st gp1, [ffffffffh] ; Highest address; Reserved, but unused

    ; Calling / stack / interrupts
    call func
    int $VECTOR_RESET

_start:
    nop
    jmp start_actual

func:
    ; Test stack inside function
    pop gp1
    mov gp1, gp2
    push gp2
    call func_nested1
    ret

func_nested1:
    st gp2, [40000000h]
    ret