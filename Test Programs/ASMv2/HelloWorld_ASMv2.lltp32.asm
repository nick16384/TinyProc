#VERSION 2.0
#MEMREGION RAM 0x00000000, 0x01000000 ; Main program memory
#MEMREGION CON 0x01000001, 0x01000020 ; Console / text output memory

; Spits out "Hello, World!" in the console memory and then continues in a infinite loop doing nothing.
; This program uses the v2 assembler, which is much more feature rich and therefore produces much cleaner code.

MOV   gp1, "Hell"
STORE gp1, CON:0

MOV   gp1, "o, W"
STORE gp1, CON:1

MOV   gp1, "orld"
STORE gp1, CON:2

MOV   gp1, "!"
STORE gp1, CON:3

_halt:
JMP   _halt