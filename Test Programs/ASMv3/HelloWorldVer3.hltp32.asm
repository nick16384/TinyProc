#VERSION 3.0
#ENTRY _start

#SECTION (__attribute__ relocatable = false) (__attribute__ loadaddress = 0x00000100) .data
; immediate is basically the same as #define in C
immediate int_syscall = 1
immediate int_syscall_conwrite = 10
; Hello world message must be stored as a pointer to the message, since the
; message itself is too large to be stored in a 32 bit value
pointer hello_world_msg, "Hello, World!" + 0xA ; Store string + newline
; Block is guaranteed to be continuous in memory -> Can be addressed
; Block internal data cannot be addressed after the block is created without knowing an offset
block params_helloworld_call
{
    ; Data in a block does not have individual names
    pointer hello_world_msg
    ; immediate length_bytes: hello_world_msg
    immediate length_words: hello_world_msg
}

#SECTION .text
_start:
    load  *(hello_world_msg + 0), r0
    store r0, CON:0
    load  *(hello_world_msg + 1), r0
    store r0, CON:1
    ; Syntactic sugar: var[x] = *var + x
    load  *(hello_world_msg + 2), r0
    store r0, CON:2
    load  *(hello_world_msg + 3), r0
    store r0, CON:3

    ; Alternative variant using software interrupts:

    ; r6: function
    ; r7: parameter 1 (pointer or 32 bit value)
    ; r8: parameter 2 (pointer or 32 bit value)
    ; If more parameters needed, use parameter blocks and pointers to them

    load  int_syscall_conwrite, r6 ; Load the function to be called by the syscall (console write)
    load  hello_world_msg, r7          ; Load pointer to the data to be printed
    load  hello_world_msg_words, r8    ; Load the number of words to be printed
    int   int_syscall              ; Trigger the interrupt -> syscall