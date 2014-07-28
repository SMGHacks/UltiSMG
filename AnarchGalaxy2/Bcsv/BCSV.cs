using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bcsv
{
    public unsafe class BCSV
    {
        private VoidPtr _Data = null;

        private BCSV(VoidPtr data)
        {
            _Data = data;
        }

        public int GetItemCount()
        {
            Header* header = (Header*)_Data;

            return header->_ItemCount;
        }

        public int GetFieldCount()
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));

            return header->_FieldCount;
        }

        public FieldDefinition[] GetFieldDefinitions()
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            int count = header->_FieldCount;
            FieldDefinition[] result = new FieldDefinition[count];

            for (int i = 0; i < count; i++)
            {
                string name = FieldHashcode.LookupHashcode(fields[i]._Hashcode);
                uint mask = fields[i]._Mask;
                ushort offset = fields[i]._Offset;
                byte shift = fields[i]._Shift;
                byte type = fields[i]._Type;

                result[i] = new FieldDefinition(name, fields[i]._Hashcode, offset, type, mask, shift);
            }

            return result;
        }

        public object GetFieldValue(int item, int field)
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            int type = fields[field]._Type;

            switch (type)
            {
                case 0x00: return GetFieldAsWord(item, field);
                case 0x02: return GetFieldAsFloat(item, field);
                case 0x03: return GetFieldAsWord(item, field);
                case 0x04: return GetFieldAsHalf(item, field);
                case 0x05: return GetFieldAsByte(item, field);
                case 0x06: return GetFieldAsString(item, field);
                default: throw new Exception(string.Format("Unknown field type: {0}", type.ToString("X2")));
            }
        }

        public float GetFieldAsFloat(int item, int field)
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            VoidPtr items = _Data + header->_Items;
            int offset = fields[field]._Offset;
            int stride = header->_ItemStride;
            int type = fields[field]._Type;

            if (type != 0x02)
                throw new Exception(string.Format("Field is not of Float(0x02) Type: {0}", type.ToString("X2")));

            float result = (bfloat)Marshal.PtrToStructure(items[item, stride] + offset, typeof(bfloat));

            return result;
        }

        public uint GetFieldAsWord(int item, int field)
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            VoidPtr items = _Data + header->_Items;
            int offset = fields[field]._Offset;
            int stride = header->_ItemStride;
            int type = fields[field]._Type;

            if (type != 0x00 && type != 0x03)
                throw new Exception(string.Format("Field is not of Word(0x00, 0x03) Type: {0}", field.ToString("X2")));

            uint result = (buint)Marshal.PtrToStructure(items[item, stride] + offset, typeof(buint));

            result = (uint)((result & fields[field]._Mask) >> fields[field]._Shift);

            return result;
        }

        public ushort GetFieldAsHalf(int item, int field)
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            VoidPtr items = _Data + header->_Items;
            int offset = fields[field]._Offset;
            int stride = header->_ItemStride;
            int type = fields[field]._Type;

            if (type != 0x04)
                throw new Exception(string.Format("Field is not of Half(0x04) Type: {0}", field.ToString("X2")));

            ushort result = (bushort)Marshal.PtrToStructure(items[item, stride] + offset, typeof(bushort));

            result = (ushort)((result & fields[field]._Mask) >> fields[field]._Shift);

            return result;
        }

        public byte GetFieldAsByte(int item, int field)
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            VoidPtr items = _Data + header->_Items;
            int offset = fields[field]._Offset;
            int stride = header->_ItemStride;
            int type = fields[field]._Type;

            if (type != 0x05)
                throw new Exception(string.Format("Field is not of Byte(0x05) Type: {0}", field.ToString("X2")));

            byte result = Marshal.ReadByte(items[item, stride], offset);

            result = (byte)((result & fields[field]._Mask) >> fields[field]._Shift);

            return result;
        }

        public string GetFieldAsString(int item, int field)
        {
            Header* header = (Header*)_Data;
            Field* fields = (Field*)(_Data + Marshal.SizeOf(typeof(Header)));
            VoidPtr items = _Data + header->_Items;
            VoidPtr strings = _Data + header->_Items + (header->_ItemCount * header->_ItemStride);
            int offset = fields[field]._Offset;
            int stride = header->_ItemStride;
            int type = fields[field]._Type;

            if (type != 0x06)
                throw new Exception(string.Format("Field is not of String(0x06) Type: {0}", type.ToString("X2")));

            uint stringOffset = (buint)Marshal.PtrToStructure(items[item, stride] + offset, typeof(buint));
            string result = Encoding.GetEncoding("Shift-JIS").GetString(strings + stringOffset);

            return result;
        }


        #region Static Members

        public static BCSV Wrap(VoidPtr ptr)
        {
            Header* header = (Header*)ptr;
            bool invalid = false;

            invalid |= header->_ItemCount < 0;
            invalid |= header->_FieldCount < 0;
            invalid |= header->_Items < Marshal.SizeOf(typeof(Header));
            invalid |= header->_ItemStride < 0;

            if (invalid)
                throw new Exception("Data cannot be wrapped with the BCSV class.");

            return new BCSV(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public bint _ItemCount;
            public bint _FieldCount;
            public bint _Items;
            public bint _ItemStride;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Field
        {
            public buint _Hashcode;
            public buint _Mask;
            public bushort _Offset;
            public byte _Shift;
            public byte _Type;
        }

        #endregion
    }
}
