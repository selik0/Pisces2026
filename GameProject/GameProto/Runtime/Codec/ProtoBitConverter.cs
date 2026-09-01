using System.Runtime.InteropServices;

namespace GameProto
{
    internal static class ProtoBitConverter
    {
        public static float UInt32ToSingle(uint value)
        {
            UInt32SingleUnion union = new UInt32SingleUnion
            {
                UInt32 = value
            };
            return union.Single;
        }

        public static uint SingleToUInt32(float value)
        {
            UInt32SingleUnion union = new UInt32SingleUnion
            {
                Single = value
            };
            return union.UInt32;
        }

        public static double UInt64ToDouble(ulong value)
        {
            UInt64DoubleUnion union = new UInt64DoubleUnion
            {
                UInt64 = value
            };
            return union.Double;
        }

        public static ulong DoubleToUInt64(double value)
        {
            UInt64DoubleUnion union = new UInt64DoubleUnion
            {
                Double = value
            };
            return union.UInt64;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UInt32SingleUnion
        {
            [FieldOffset(0)]
            public uint UInt32;

            [FieldOffset(0)]
            public float Single;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UInt64DoubleUnion
        {
            [FieldOffset(0)]
            public ulong UInt64;

            [FieldOffset(0)]
            public double Double;
        }
    }
}
