using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bcsv
{
    public class FieldDefinition
    {
        public string Name { get { return _Name; } }
        private string _Name = "";

        public uint Hashcode { get { return _Hashcode; } }
        private uint _Hashcode = 0xFFFFFFFF;

        public uint Mask { get { return _Mask; } }
        private uint _Mask = 0xFFFFFFFF;

        public ushort Offset { get { return _Offset; } }
        private ushort _Offset = 0;

        public byte Shift { get { return _Shift; } }
        private byte _Shift = 0;

        public byte Type { get { return _Type; } }
        private byte _Type = 0;

        public FieldDefinition(string name, uint hashcode, ushort offset, byte type)
            : this(name, hashcode, offset, type, GetDefaultMask(type), 0)
        {

        }

        public FieldDefinition(string name, uint hashcode, ushort offset, byte type, uint mask, byte shift)
        {
            _Name = name;
            _Hashcode = hashcode;
            _Mask = mask;
            _Offset = offset;
            _Shift = shift;
            _Type = type;
        }

        public Type GetSystemType()
        {
            return GetSystemType(_Type);
        }

        #region Static Members

        public static Type GetSystemType(int type)
        {
            switch (type)
            {
                case 0x00: return typeof(uint);
                case 0x02: return typeof(float);
                case 0x03: return typeof(uint);
                case 0x04: return typeof(ushort);
                case 0x05: return typeof(byte);
                case 0x06: return typeof(string);
                default: throw new Exception(string.Format("Unknown field type: {0}", type.ToString("X2")));
            }
        }

        public static object GetDefaultValue(int type)
        {
            switch (type)
            {
                case 0x00: return (uint)0x00000000;
                case 0x02: return (float)0.0;
                case 0x03: return (uint)0x00000000;
                case 0x04: return (ushort)0x0000;
                case 0x05: return (byte)0x00;
                case 0x06: return "";
                default: throw new Exception(string.Format("Unknown field type: {0}", type.ToString("X2")));
            }
        }

        public static uint GetDefaultMask(byte type)
        {
            switch (type)
            {
                case 0x00: return 0xFFFFFFFF;
                case 0x02: return 0xFFFFFFFF;
                case 0x03: return 0xFFFFFFFF;
                case 0x04: return 0xFFFF;
                case 0x05: return 0xFF;
                case 0x06: return 0xFFFFFFFF;
                default: throw new Exception(string.Format("Unknown field type: {0}", type.ToString("X2")));
            }
        }

        public static FieldDefinition[] FromFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            return FromPtr(bytes);
        }

        public static FieldDefinition[] FromPtr(byte[] bytes)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            FieldDefinition[] result = FromPtr(ptr);
            Marshal.FreeHGlobal(ptr);

            return result;
        }

        public static FieldDefinition[] FromPtr(VoidPtr ptr)
        {
            BCSV bcsv = BCSV.Wrap(ptr);

            return bcsv.GetFieldDefinitions();
        }

        #endregion
    }
}
