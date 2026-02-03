#VERSION 3.0
#ENTRY _start

#SECTION .data
; immediate is basically the same as #define in C
immediate int_syscall = 1
immediate int_syscall_conwrite = 10
; Hello world message must be stored as a pointer to the message, since the
; message itself is too large to be stored in a 32 bit value
pointer hello_world_msg, "Hello, World!" + 0xA ; Store string + newline
; Block is guaranteed to be continuous in memory -> Can be addressed
; Block internal data cannot be addressed after the block is created without knowing an offset
immediate hello_world_msg_words len: hello_world_msg
; TODO: Do we really need this block structure? If yes, implement it properly!
;block params_helloworld_call
;{
;    ; Data in a block does not have individual names
;    pointer hello_world_msg
;    ; immediate length_bytes: hello_world_msg
;    immediate len: hello_world_msg
;}

#SECTION .text
_start:
    ; Note: Constant values that are evaluated by the assembler must be put in parenthesis.
    load  gp1, (hello_world_msg + 0)
    store gp1, 70
    load  gp1, (hello_world_msg + 1)
    store gp1, 71
    load  gp1, (hello_world_msg + 2)
    store gp1, 72
    load  gp1, (hello_world_msg + 3)
    store gp1, 73

    ; Alternative variant using software interrupts:

    ; r6: function
    ; r7: parameter 1 (pointer or 32 bit value)
    ; r8: parameter 2 (pointer or 32 bit value)
    ; If more parameters needed, use parameter blocks and pointers to them

    ;load  gp6, int_syscall_conwrite  ; Load the function to be called by the syscall (console write)
    ;load  gp7, hello_world_msg       ; Load pointer to the data to be printed
    ;load  gp8, hello_world_msg_words ; Load the number of words to be printed
    ;int   int_syscall                ; Trigger the interrupt -> syscall