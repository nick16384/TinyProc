#VERSION 3.0 
#ENTRY _start 

#SECTION .data

immediate int_syscall = 1
immediate int_syscall_conwrite = 10

#SECTION .text
_start:

    MOV gp1, 0xffffffff
    ADD gp1, 1

