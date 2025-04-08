; Simple program that counts memory address 0x78 up every 4 cycles.
LOAD  GP1, 0x78 ; 0
INC   GP1       ; 2
STORE GP1, 0x78 ; 4
JMP   0x0       ; 6