#VERSION 2.0
#MEMREGION RAM 0x00000000 0x00FFFFFF
#MEMREGION CON 0x01000000 0x01000FFF

; Calculates the factorial of a given number

mov   gp1, 10 ; Source number -> Calculate the factorial of this
mov   gp2, 1 ; gp2: Multiplier 1
mov   gp3, 1 ; gp3: Multiplier 2
mov   gp4, 1 ; gp4: gp2 * gp3

factorial:
    mov   gp2, gp1
    mov   gp3, gp4
    mov   gp4, 0
    ;jmp   multiply

; gp2 * gp3 = gp4 (Add gp3 to gp4 gp2 amount of times)
multiply:
    add   gp4, gp3
    dec   gp2
    bnz   multiply

    mov   gp5, gp4

    dec   gp1
    bnz   factorial

_halt:
    jmp   _halt