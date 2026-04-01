; Test for all (or most) ASMv3 features
; Doesn't serve a distinct purpose except debugging
#VERSION 3.1
#ENTRY _start

#DEFINE REPEAT_TEST_TIMES 50
#DEFINE REPEAT_TEST_DATA "word. "
#DEFINE LD_DATA_ADDR 0x20202020
#DEFINE SP_BASE 0x00020000
#DEFINE VECTOR_RESET 0x00000000

#SECTION .data
dw imm1 0x39393939
dw imm2 39393939h
dw imm3 0b01010101
dw imm4 39393939
dw ptr1 0x55, "This is a test string", 0xA, "Another test string"
dw "Empty data here lol"
equ imm5 len: ptr1
dw imm6 len: ptr1
times 50 db 0x0
times $REPEAT_TEST_TIMES db $REPEAT_TEST_DATA

#SECTION (__entry = _start) .text
_start:
    ; Basic arithmetic
    mov gp1, imm1

    ; Load / store
    ld gp1, [imm1]
    st gp1, [abs imm1]
    st gp1, [rel imm1]
    st gp1, [+imm1]
    st gp1, [-imm1]
    st gp1, [$LD_DATA_ADDR]
    st gp1, [+$LD_DATA_ADDR]

    ; Calling / stack / interrupts
    call func
    int $VECTOR_RESET

func:
    pop gp1
    mov gp2, gp1
    push gp1
    call func_nested1
    ret

func_nested1:
    st gp2, [40000000h]
    ret