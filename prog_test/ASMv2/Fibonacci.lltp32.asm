#VERSION 2.0
#MEMREGION RAM 0x00000000 0x000FFFFF
#MEMREGION CON 0x00100000 0x001000FF

mov gp1, 0
mov gp2, 1
mov gp4, 18

fib_loop:
    mov gp3, 0
    add gp3, gp1
    add gp3, gp2

    mov gp1, gp2
    mov gp2, gp3

    dec gp4
    mov gp4, gp4 ; effectively a comparison
    bnz fib_loop

_halt:
    jmp _halt