namespace TinyProc.Processor;

// The control unit directs control of individual CPU elements depending on the instruction bits.
// It essentially decodes a received instruction and controls the components (e.g. ALU and registers)
// its connected to.
class ControlUnit(CPU cpu)
{
    // Program counter
    private Register PC;
    // Instruction register 1
    private Register IRA;
    // Instruction register 2
    // Two 32 bit instruction registers necessary, since an entire instruction
    // is double-word-aligned, meaning it occupies 64 bits.
    private Register IRB;

    // The parent CPU
    // TODO: Is this really beautiful?
    private CPU _cpu = cpu;
}