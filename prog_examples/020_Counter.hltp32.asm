; Demo program:
; Creates a variable in memory called "x" and increments it indefinitely.

#VERSION 3.1 ; Assembly version 3.1
#ORG 0x10000000 ; Load this code at memory address 0x10000000

#SECTION .data ; Start of the .data section; No executable code here, just raw data
x: dw 0 ; Reserve 32 bits for the variable x and initialize its value to zero.
; Implicit end of the .data section

#SECTION (__entry__ = _start) .text ; Start of the .text section; Actual executable code resides here.
_start:
    jmp main_loop

main_loop:
    ld gp1, [x] ; Get value at address of x
    add gp1, 1  ; Increment by 1
    st gp1, [x] ; Store value back to address of x

; Implicit end of the .text section