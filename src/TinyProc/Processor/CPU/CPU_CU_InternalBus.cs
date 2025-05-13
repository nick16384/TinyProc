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
        /*private abstract class MultiSrcMultiDstRegisterSelector
        {
            private protected readonly Dictionary<RegisterCode, Register> _sourceCodeRegisterMap;
            private protected readonly Dictionary<RegisterCode, Register> _targetCodeRegisterMap;
            private protected readonly Bus _transferBus;

            private protected RegisterCode _busSourceRegisterCode;
            private protected RegisterCode BusSourceRegisterCode
            {
                get => _busSourceRegisterCode;
                set => UpdateBusTransferRoute(value, BusTargetRegisterCode);
            }
            private protected Register BusSourceRegister
            {
                get => _sourceCodeRegisterMap[BusSourceRegisterCode];
            }

            private protected RegisterCode _busTargetRegisterCode;
            private protected RegisterCode BusTargetRegisterCode
            {
                get => _busTargetRegisterCode;
                set => UpdateBusTransferRoute(BusSourceRegisterCode, value);
            }
            private protected Register BusTargetRegister
            {
                get => _targetCodeRegisterMap[BusTargetRegisterCode];
            }

            private protected void UpdateBusTransferRoute(RegisterCode newSrcRegisterCode, RegisterCode newDstRegisterCode)
            {
                if (!_sourceCodeRegisterMap.ContainsKey(newSrcRegisterCode))
                    throw new ArgumentException($"Illegal bus source address {newSrcRegisterCode:x8}");

                if (!_targetCodeRegisterMap.ContainsKey(newDstRegisterCode))
                    throw new ArgumentException($"Illegal bus target address {newDstRegisterCode:x8}");

                // Disable old transfer route so no garbage data remains on the bus.
                BusSourceRegister.BusReadEnable = false;
                BusTargetRegister.BusWriteEnable = false;

                _busSourceRegisterCode = newSrcRegisterCode;
                _busTargetRegisterCode = newDstRegisterCode;

                BusSourceRegister.WriteTargetUBID = _transferBus._UBID;
                BusTargetRegister.ReadSourceUBID = _transferBus._UBID;
                BusTargetRegister.BusWriteEnable = true;
                BusSourceRegister.BusReadEnable = true;
                // Transfer from source to destination occurs via bus.
            }

            public MultiSrcMultiDstRegisterSelector(int busWidth, uint UBID,
                Dictionary<RegisterCode, Register> sourceCodeRegisterMap,
                Dictionary<RegisterCode, Register> targetCodeRegisterMap)
            {
                _sourceCodeRegisterMap = sourceCodeRegisterMap;
                foreach (Register reg in _sourceCodeRegisterMap.Values)
                {
                    reg.BusReadEnable = false;
                    reg.BusWriteEnable = false;
                }
                _targetCodeRegisterMap = targetCodeRegisterMap;
                foreach (Register reg in _targetCodeRegisterMap.Values)
                {
                    reg.BusReadEnable = false;
                    reg.BusWriteEnable = false;
                }
                _transferBus = new Bus(busWidth, UBID, [.. _sourceCodeRegisterMap.Values, .. _targetCodeRegisterMap.Values]);
            }
        }*/

        // TODO: Make MultiSrcSingleDstRegisterSelector and SingleSrcMultiDstRegisterSelector
        // inherit from one common class.

        private class MultiSrcSingleDstRegisterSelector
        {
            private readonly Dictionary<InternalRegisterCode, Register> _sourceCodeRegisterMap;
            private readonly Register _fixedTargetRegister;
            private readonly Bus _transferBus;

            private InternalRegisterCode _busSourceRegisterCode;
            public InternalRegisterCode BusSourceRegisterCode
            {
                get => _busSourceRegisterCode;
                set => SetSourceRegisterCode(value);
            }
            private Register BusSourceRegister
            {
                get => _sourceCodeRegisterMap[BusSourceRegisterCode];
            }

            private void SetSourceRegisterCode(InternalRegisterCode newSrcRegisterCode)
            {
                if (!_sourceCodeRegisterMap.ContainsKey(newSrcRegisterCode))
                    throw new ArgumentException($"Illegal bus source address {newSrcRegisterCode:x8}");

                // Disable old transfer route so no garbage data remains on the bus.
                BusSourceRegister.BusReadEnable = false;
                _fixedTargetRegister.BusWriteEnable = false;

                _busSourceRegisterCode = newSrcRegisterCode;

                BusSourceRegister.WriteTargetUBID = _transferBus._UBID;
                _fixedTargetRegister.ReadSourceUBID = _transferBus._UBID;
                _fixedTargetRegister.BusWriteEnable = true;
                BusSourceRegister.BusReadEnable = true;
                // Transfer from source to destination occurs via bus.
            }

            public MultiSrcSingleDstRegisterSelector(int busWidth, uint UBID,
                Dictionary<InternalRegisterCode, Register> sourceCodeRegisterMap, Register fixedTargetRegister)
            {
                _sourceCodeRegisterMap = sourceCodeRegisterMap;
                _busSourceRegisterCode = _sourceCodeRegisterMap.Keys.First();
                _fixedTargetRegister = fixedTargetRegister;
                foreach (Register reg in _sourceCodeRegisterMap.Values)
                {
                    reg.BusReadEnable = false;
                    reg.BusWriteEnable = false;
                }
                _fixedTargetRegister.BusReadEnable = false;
                _fixedTargetRegister.BusWriteEnable = false;
                _transferBus = new Bus(busWidth, UBID, [.. _sourceCodeRegisterMap.Values, _fixedTargetRegister]);
            }
        }

        private class SingleSrcMultiDstRegisterSelector
        {
            private readonly Register _fixedSourceRegister;
            private readonly Dictionary<InternalRegisterCode, Register> _targetCodeRegisterMap;
            private readonly Bus _transferBus;

            private InternalRegisterCode _busTargetRegisterCode;
            public InternalRegisterCode BusTargetRegisterCode
            {
                get => _busTargetRegisterCode;
                set => SetTargetRegisterCode(value);
            }
            private Register BusTargetRegister
            {
                get => _targetCodeRegisterMap[BusTargetRegisterCode];
            }

            private void SetTargetRegisterCode(InternalRegisterCode newDstRegisterCode)
            {
                if (!_targetCodeRegisterMap.ContainsKey(newDstRegisterCode))
                    throw new ArgumentException($"Illegal bus target address {newDstRegisterCode:x8}");

                // Disable old transfer route so no garbage data remains on the bus.
                _fixedSourceRegister.BusReadEnable = false;
                BusTargetRegister.BusWriteEnable = false;

                _busTargetRegisterCode = newDstRegisterCode;

                _fixedSourceRegister.WriteTargetUBID = _transferBus._UBID;
                BusTargetRegister.ReadSourceUBID = _transferBus._UBID;
                BusTargetRegister.BusWriteEnable = true;
                _fixedSourceRegister.BusReadEnable = true;
                // Transfer from source to destination occurs via bus.
            }

            public SingleSrcMultiDstRegisterSelector(int busWidth, uint UBID,
                Register fixedSourceRegister, Dictionary<InternalRegisterCode, Register> targetCodeRegisterMap)
            {
                _fixedSourceRegister = fixedSourceRegister;
                _fixedSourceRegister.BusReadEnable = false;
                _fixedSourceRegister.BusWriteEnable = false;

                _targetCodeRegisterMap = targetCodeRegisterMap;
                _busTargetRegisterCode = _targetCodeRegisterMap.Keys.First();
                foreach (Register reg in _targetCodeRegisterMap.Values)
                {
                    reg.BusReadEnable = false;
                    reg.BusWriteEnable = false;
                }
                _transferBus = new Bus(busWidth, UBID, [_fixedSourceRegister, .. _targetCodeRegisterMap.Values]);
            }
        }

        public const uint INTBUS_B1_UBID = 0x00000001u;
        public const uint INTBUS_B2_UBID = 0x00000002u;
        public const uint INTBUS_B3_UBID = 0x00000003u;

        private readonly Dictionary<InternalRegisterCode, Register> B1_REGISTERS;
        private readonly Dictionary<InternalRegisterCode, Register> B2_REGISTERS;
        private readonly Dictionary<InternalRegisterCode, Register> B3_REGISTERS;

        private readonly MultiSrcSingleDstRegisterSelector _IntBus1;
        private readonly MultiSrcSingleDstRegisterSelector _IntBus2;
        private readonly SingleSrcMultiDstRegisterSelector _IntBus3;
    }
}