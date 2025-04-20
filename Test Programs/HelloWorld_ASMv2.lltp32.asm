#VERSION 2.0
#MEMREGION RAM 0x00000000, 0x10000000 ; Main program memory
#MEMREGION CON 0x10000001, 0x20000000 ; Console / text output memory

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

STORE "Hell", CON:4
STORE "o, W", CON:5
STORE "orld", CON:6
STORE "!  2", CON:7

_halt:
JMP   _halt