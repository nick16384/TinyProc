; Simple program that counts memory address 0x78 up every 4 cycles.
LOAD  GP1, 0x78
ADD   GP1, 0x1
STORE GP1, 0x78
JMP   0x0