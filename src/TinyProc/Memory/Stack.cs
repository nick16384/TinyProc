namespace TinyProc.Memory;

/// <summary>
/// Haha you fool expected some code here, but instead, you are greeted with a giant wall of text
/// explaining how the stack in the HLTP32 architecture works.<br></br>
/// The stack is similar to any other read/write memory, the only key difference being that
/// the stack can not be read from / written to in any random order, but instead in
/// a LIFO manner (last in, first out).
/// Since the stack is just a hypothetical construct in memory from external view,
/// the CPU must implement stack functionality itself, which is the reason no separate class exists
/// for the stack logic.
/// The CPU internally holds a register called the stack pointer (SP), which points to the uppermost
/// stack element (the element with the highest address, since the stack grows from lower to higher addresses).
/// If an element is added ("push"), the stack pointer is incremented and the element stored at the new SP address.
/// If an element is removed ("pop"), the data at the current SP is zeroed out and the stack pointer decremented,
/// returning the element that was removed.
/// </summary>
