using TinyProc.Application;
using TinyProc.Memory;

namespace TinyProc.Processor.CPU;

public partial class CPU
{
    // Memory management unit (fulfils the role of a component usually referred to as the memory controller)
    // Much simpler than in modern architectures (like x86_64), but essentially controls
    // memory flow in / out of the CPU
    private class MMU : IBusAttachable
    {
        // TODO: Make part (esp. read/write of addresses) publicly available (via CPU debug port?)
        // TODO: Clean up code for that
        public class MemoryAddressRegister(MMU mmu) : Register(0, true)
        {
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get => _storedValue;
                set
                {
                    _storedValue = value;
                    // Update MDR --> Trigger bus update
                    //uint? unassigned = _mmu?.MDR?.ValueDirect;
                }
            }
        }
        public class MemoryDataRegister(MMU mmu) : Register(0, true)
        {
            private readonly Lock readWriteLock = new();
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get
                {
                    // Synchronization is required, because read / write operations are not atomic, meaning
                    // that any external thread could input wrong address data while the CPU might still want
                    // to read from another address it wrote to the MAR earlier.
                    lock (readWriteLock)
                    {
                        _mmu.MemoryAddressBus.Data = Bus.UIntToBoolArray(_mmu.GetRelativeAddress(_mmu.MAR.ValueDirect, _mmu.RAM));
                        _mmu.RAM.ReadEnable = true;
                        _storedValue = Bus.BoolArrayToUInt(_mmu.MemoryDataBus.Data, 0);
                        _mmu.RAM.ReadEnable = false;
                        return _storedValue;
                    }
                }
                set
                {
                    lock (readWriteLock)
                    {
                        _mmu.MemoryAddressBus.Data = Bus.UIntToBoolArray(_mmu.GetRelativeAddress(_mmu.MAR.ValueDirect, _mmu.RAM));
                        _mmu.MemoryDataBus.Data = Bus.UIntToBoolArray(value);
                        if (_mmu.RAM is not IReadWriteMemoryDevice)
                            throw new Exception("Cannot write on read-only device.");
                        IReadWriteMemoryDevice rwDevice = _mmu.RAM as IReadWriteMemoryDevice;
                        rwDevice.WriteEnable = true;
                        rwDevice.WriteEnable = false;
                        _storedValue = value;
                    }
                }
            }
        }
        public class StackPointer(MMU mmu) : Register(SP_BASE_ADDRESS, true)
        {
            private readonly MMU _mmu = mmu;
            private protected override uint Value
            {
                get => base.Value;
                set
                {
                    if (value > SP_MAX_ADDRESS)
                        _mmu._cpu.TriggerHardwareFault(Fault.STACK_OVERFLOW);
                    if (value < SP_BASE_ADDRESS)
                        throw new Exception("The stack pointer points to memory below the stack. This is a logic error.");
                    base.Value = value;
                }
            }
        }

        private readonly Dictionary<IMemoryDevice, (uint, uint)> _MemorySpaces;
        // Memory address register: Sets an address to read from / write to in memory logic.
        public readonly MemoryAddressRegister MAR;
        // Memory data register:
        // When reading, contains the value at address set in MAR.
        // When writing, contains the value to be written to address in MAR.
        public readonly MemoryDataRegister MDR;
        // Stack pointer
        private const uint SP_BASE_ADDRESS = 0x00020000;
        private const uint SP_MAX_ADDRESS = 0x0002FFFF;
        public readonly StackPointer SP;

        // Memory bus UBIDs
        public const uint UBID_MEMADDRESSBUS = 0x11;
        public const uint UBID_MEMDATABUS = 0x12;

        // Multiple devices are all attached to the same bus.
        // If the MMU wants to address a specific device, it should use the
        // respective read/write lines of that device.
        private readonly Bus MemoryAddressBus;
        private readonly Bus MemoryDataBus;

        private readonly CPU _cpu;

        // Facilitates and encapsulates mechanisms to read from and write to arbitrary memory.
        // Combines attached memory objects' address spaces into one continuous address space.
        public MMU(CPU cpu, Dictionary<uint, IMemoryDevice> rams)
        {
            _cpu = cpu;
            _MemorySpaces = [];
            foreach ((uint memStart, IMemoryDevice memDevice) in rams)
            {
                Logging.LogDebug($"Mem HW start:{memStart:x8}");
                _MemorySpaces.Add(memDevice, (memStart, memDevice.Size - 1));
            }
            MAR = new MemoryAddressRegister(this);
            MDR = new MemoryDataRegister(this);
            SP = new StackPointer(this);

            MemoryAddressBus = new Bus(Register.SYSTEM_WORD_SIZE, UBID_MEMADDRESSBUS, [.. rams.Values]);
            MemoryDataBus = new Bus(Register.SYSTEM_WORD_SIZE, UBID_MEMDATABUS, [.. rams.Values]);
        }

        private IMemoryDevice GetMemAtVirtualAddress(uint addr)
        {
            foreach ((IMemoryDevice ram, (uint, uint) ramSpace) in _MemorySpaces)
            {
                // Not the correct RAM, if the address is above the the max. address of the current RAM
                if (addr >= ramSpace.Item2)
                    continue;
                return ram;
            }
            // If the foreach loop runs through before the return statement was called, then the address is out of range.
            throw new ArgumentOutOfRangeException(
                $"Cannot determine memory from absolute address {addr:x8}. Max. address is {_MemorySpaces.Values.Last().Item2:x8}");
        }

        private IMemoryDevice RAM
        {
            get => GetMemAtVirtualAddress(MAR.ValueDirect);
        }

        private uint GetRelativeAddress(uint absoluteAddr, IMemoryDevice ram)
        {
            return absoluteAddr - _MemorySpaces[ram].Item1;
        }

        public void AttachToBus(uint ubid, Bus bus) { /* Do nothing. MMU already is the bus master. */ }

        #region Debug methods

        /// <summary>
        /// <i>Internal debug method, not for use as actual hardware.</i><br></br>
        /// Reads any arbitrary address from virtual memory space.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal uint Debug_ReadVirtualDirect(uint address)
        {
            IMemoryDevice dev = GetMemAtVirtualAddress(address);
            return dev.ReadDirect(GetRelativeAddress(address, dev));
        }
        /// <summary>
        /// <i>Internal debug method, not for use as actual hardware.</i><br></br>
        /// Writes to any arbitrary address from virtual memory space.
        /// Writing to a ROM will still throw an error.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        internal void Debug_WriteVirtualDirect(uint address, uint value)
        {
            IReadWriteMemoryDevice rwDev = GetMemAtVirtualAddress(address) as IReadWriteMemoryDevice;
            rwDev.WriteDirect(GetRelativeAddress(address, rwDev), value);
        }

        #endregion Debug methods
    }
}