; Spits out "Hello, World!" in the console memory and then continues in a infinite loop doing nothing.

; "Hell"
MOV   gp1, 0x48656c6c
STORE gp1, 20

; "o, W"
MOV   gp1, 0x6f2c2057
STORE gp1, 21

; "orld"
MOV   gp1, 0x6f726c64
STORE gp1, 22

; "!"
MOV   gp1, 0x21000000
STORE gp1, 23

NOP
JMP   16

; Total size: 18 words