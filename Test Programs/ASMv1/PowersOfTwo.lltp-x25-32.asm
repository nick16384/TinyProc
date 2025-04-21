; Program that calculates powers of two indefinitely.
; The program is stuck on zero after an overflow occurs.

; Init
MOV   gp3, 0
MOV   gp4, 1

; Main
ADD   gp3, gp4
MOV   gp4, gp3
STORE gp3, 0x78
JMP   4 ; Main