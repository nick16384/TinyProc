#VERSION 2.0
#MEMREGION RAM 0x00000000, 0x00FFFFFF
#MEMREGION CON 0x01000000, 0x01000020

; Prints out the alphabet in capital letters

MOV   gp1, "---A"
STORE gp1, RAM:78
MOV   gp2, CON:0

MOV   gp3, 26

_alphabet:
STORE gp1, RAM:78
STORR gp1, gp2
LOAD  gp1, RAM:78
INC   gp1
INC   gp2
SUB   gp3, 1
BNZ   _alphabet

_halt:
JMP   _halt