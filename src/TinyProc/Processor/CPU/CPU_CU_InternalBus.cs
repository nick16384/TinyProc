namespace TinyProc.Processor.CPU;

public partial class CPU
{
    private partial class ControlUnit
    {
        // Special purpose circuit only accessible by the CU.
        // The register inside specifies, which register the internal busses B1, B2, and B3 read from / write to.
        // Acts as the bus master of all internal busses separately.
        // TODO: Maybe move this class to Register.cs file; Seems useful in a lot of places.
        // TODO: Make class's function bidirectional so target and source can switch roles.
        private class MultiSrcMultiDstRegisterSelector
        {
            private protected readonly uint _UBID;
            private protected readonly Dictionary<uint, Register> _sourceAddressRegisterMap;
            private protected readonly Dictionary<uint, Register> _targetAddressRegisterMap;
            private protected readonly Bus _transferBus;

            private protected uint _busSourceRegisterAddress;
            public uint BusSourceRegisterAddress
            {
                get => _busSourceRegisterAddress;
                set => UpdateBusTransferRoute(value, BusTargetRegisterAddress);
            }
            private protected Register BusSourceRegister
            {
                get => _sourceAddressRegisterMap[BusSourceRegisterAddress];
            }

            private protected uint _busTargetRegisterAddress;
            public uint BusTargetRegisterAddress
            {
                get => _busTargetRegisterAddress;
                set => UpdateBusTransferRoute(BusSourceRegisterAddress, value);
            }
            private protected Register BusTargetRegister
            {
                get => _targetAddressRegisterMap[BusTargetRegisterAddress];
            }

            private protected void UpdateBusTransferRoute(uint newSrcRegisterAddress, uint newDstRegisterAddress)
            {
                if (!_sourceAddressRegisterMap.ContainsKey(newSrcRegisterAddress))
                    throw new ArgumentException($"Illegal bus source address {newSrcRegisterAddress:X8}");

                if (!_targetAddressRegisterMap.ContainsKey(newDstRegisterAddress))
                    throw new ArgumentException($"Illegal bus target address {newDstRegisterAddress:X8}");

                // Disable old transfer route so no garbage data remains on the bus.
                BusSourceRegister.BusReadEnable = false;
                BusTargetRegister.BusWriteEnable = false;

                _busSourceRegisterAddress = newSrcRegisterAddress;
                _busTargetRegisterAddress = newDstRegisterAddress;

                BusSourceRegister.WriteTargetUBID = _UBID;
                BusTargetRegister.ReadSourceUBID = _UBID;
                BusTargetRegister.BusWriteEnable = true;
                BusSourceRegister.BusReadEnable = true;
                // Transfer from source to destination occurs via bus.
            }

            public MultiSrcMultiDstRegisterSelector(int busWidth, uint UBID,
                Dictionary<uint, Register> sourceAddressRegisterMap, uint selectedSrcRegister,
                Dictionary<uint, Register> targetAddressRegisterMap, uint selectedDstRegister)
            {
                _UBID = UBID;
                _sourceAddressRegisterMap = sourceAddressRegisterMap;
                foreach (Register reg in _sourceAddressRegisterMap.Values)
                {
                    reg.BusReadEnable = false;
                    reg.BusWriteEnable = false;
                }
                _targetAddressRegisterMap = targetAddressRegisterMap;
                foreach (Register reg in _targetAddressRegisterMap.Values)
                {
                    reg.BusReadEnable = false;
                    reg.BusWriteEnable = false;
                }
                _busSourceRegisterAddress = selectedSrcRegister;
                _busTargetRegisterAddress = selectedDstRegister;
                UpdateBusTransferRoute(_busSourceRegisterAddress, _busTargetRegisterAddress);
                _transferBus = new Bus(busWidth, _UBID, [.. _sourceAddressRegisterMap.Values, .. _targetAddressRegisterMap.Values]);
            }
        }
        private class MultiSrcSingleDstRegisterSelector(int busWidth, uint UBID,
                Dictionary<uint, Register> sourceAddressRegisterMap, uint selectedSrcRegister,
                Register fixedDstRegister)
                : MultiSrcMultiDstRegisterSelector(busWidth, UBID,
                sourceAddressRegisterMap, selectedSrcRegister,
                new Dictionary<uint, Register>{{uint.MaxValue, fixedDstRegister}}, uint.MaxValue)
        {}
        private class SingleSrcMultiDstRegisterSelector(int busWidth, uint UBID,
                Register fixedSrcRegister,
                Dictionary<uint, Register> targetAddressRegisterMap, uint selectedDstRegister)
                : MultiSrcMultiDstRegisterSelector(busWidth, UBID,
                new Dictionary<uint, Register>{{uint.MaxValue, fixedSrcRegister}}, uint.MaxValue,
                targetAddressRegisterMap, selectedDstRegister)
        {}

        public const uint INTBUS_B1_UBID = 0x00000001u;
        public const uint INTBUS_B2_UBID = 0x00000002u;
        public const uint INTBUS_B3_UBID = 0x00000003u;

        private readonly Dictionary<uint, Register> B1_REGISTERS;
        private readonly Dictionary<uint, Register> B2_REGISTERS;
        private readonly Dictionary<uint, Register> B3_REGISTERS;

        private MultiSrcSingleDstRegisterSelector _IntBus1Src;
        private MultiSrcSingleDstRegisterSelector _IntBus2Src;
        private SingleSrcMultiDstRegisterSelector _IntBus3Dst;
    }
}