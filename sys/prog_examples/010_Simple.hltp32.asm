; Demo program:
; Simplest possible HLTP32 assembly program
; Does nothing for one cycle, undefined behavior afterwards

#VERSION 3.1
#ORG 0x10000000

#SECTION .data
; Empty

#SECTION (__entry__ = _start) .text
_start:
    nop