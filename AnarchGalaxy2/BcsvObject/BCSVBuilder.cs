using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using AnarchGalaxy2.Bcsv;

namespace AnarchGalaxy2.BcsvObject
{
    public unsafe static class BCSVBuilder
    {
        public static byte[] BuildBCSV(BCSVObject[] items, FieldDefinition[] fields)
        {
            BuildManager bm = new BuildManager(items, fields);

            return bm.Build();
        }

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

        private class BuildManager
        {
            private BCSVObject[] _Items = null;
            private FieldDefinition[] _Fields = null;

            private Encoding _Encoding = null;

            private int _ItemStride = 0;
            private int _Length = 0;
            private int _RoundedLength = 0;

            private Dictionary<string, int> _StringKey = null;
            

            public BuildManager(BCSVObject[] items, FieldDefinition[] fields)
            {
                _Items = items;
                _Fields = fields;
                _Encoding = Encoding.GetEncoding("Shift-JIS");
            }

            public byte[] Build()
            {
                Header header = BuildHeader();
                Field[] fields = BuildFields();
                int stringKeyLength = BuildStringKey();

                _Length = Marshal.SizeOf(typeof(Header));
                _Length += Marshal.SizeOf(typeof(Field)) * header._FieldCount;
                _Length += header._ItemStride * header._ItemCount;
                _Length += stringKeyLength;
                _RoundedLength = (_Length + 0x1F) & ~0x1F;

                VoidPtr ptr = Marshal.AllocHGlobal(_RoundedLength);
                
                WriteZero(ptr);
                WriteHeader(ptr, header);
                WriteFields(ptr, fields);
                WriteItems(ptr);
                WriteStrings(ptr);
                WritePadding(ptr);

                byte[] result = new byte[_RoundedLength];
                Marshal.Copy(ptr, result, 0, result.Length);
                Marshal.FreeHGlobal(ptr);

                return result;
            }

            private Header BuildHeader()
            {
                int headerSize = Marshal.SizeOf(typeof(Header));
                int fieldSize = Marshal.SizeOf(typeof(Field));
                _ItemStride = _Fields.Max(x => x.Offset + GetFieldStride(x.Type));

                Header result = new Header();
                result._ItemCount = _Items.Length;
                result._FieldCount = _Fields.Length;
                result._Items = headerSize + _Fields.Length * fieldSize;
                result._ItemStride = _ItemStride;

                return result;
            }

            private Field[] BuildFields()
            {
                Field[] result = new Field[_Fields.Length];

                for (int i = 0; i < _Fields.Length; i++)
                {
                    result[i] = new Field();
                    result[i]._Hashcode = _Fields[i].Hashcode;
                    result[i]._Mask = _Fields[i].Mask;
                    result[i]._Offset = _Fields[i].Offset;
                    result[i]._Shift = _Fields[i].Shift;
                    result[i]._Type = _Fields[i].Type;
                }

                return result;
            }

            private int BuildStringKey()
            {
                int length = 0;
                _StringKey = new Dictionary<string, int>();


                for (int i = 0; i < _Items.Length; i++)
                    for (int j = 0; j < _Fields.Length; j++)
                        if (_Fields[j].Type == 0x06)
                        {
                            string value = "";
                            
                            if (_Items[i].HasField(_Fields[j].Name)
                             && _Items[i].GetFieldType(_Fields[j].Name) == _Fields[j].Type)
                                value = _Items[i].GetField<string>(j);

                            if (!_StringKey.ContainsKey(value))
                            {
                                _StringKey.Add(value, length);
                                length += _Encoding.GetByteCount(value);
                                length += 1;
                            }
                        }

                return length;
            }

            private void WriteZero(VoidPtr data)
            {
                for (int i = 0; i < _Length; i++)
                    Marshal.WriteByte(data, i, 0);
            }

            private void WriteHeader(VoidPtr data, Header header)
            {
                Header* headerData = (Header*)data;
                *headerData = header;
            }

            private void WriteFields(VoidPtr data, Field[] fields)
            {
                Field* fieldData = (Field*)(data + Marshal.SizeOf(typeof(Header)));
                for (int i = 0; i < fields.Length; i++)
                    fieldData[i] = fields[i];
            }

            private void WriteItems(VoidPtr data)
            {
                VoidPtr itemData = (data + Marshal.SizeOf(typeof(Header)) + Marshal.SizeOf(typeof(Field)) * _Fields.Length);
                for (int i = 0; i < _Items.Length; i++)
                    for (int j = 0; j < _Fields.Length; j++)
                    {
                        VoidPtr fieldData = itemData[i, _ItemStride] + (int)_Fields[j].Offset;
                        object fieldValue = FieldDefinition.GetDefaultValue(_Fields[j].Type);

                        if (_Items[i].HasField(_Fields[j].Name)
                         && _Items[i].GetFieldType(_Fields[j].Name) == _Fields[j].Type)
                            fieldValue = _Items[i].GetField<object>(_Fields[j].Name);

                        switch (_Fields[j].Type)
                        {
                            case 0x00: WriteFieldAsWord(fieldData, j, (uint)fieldValue); break;
                            case 0x02: WriteFieldAsFloat(fieldData, j, (float)fieldValue); break;
                            case 0x03: WriteFieldAsWord(fieldData, j, (uint)fieldValue); break;
                            case 0x04: WriteFieldAsHalf(fieldData, j, (ushort)fieldValue); break;
                            case 0x05: WriteFieldAsByte(fieldData, j, (byte)fieldValue); break;
                            case 0x06: WriteFieldAsString(fieldData, j, (string)fieldValue); break;
                            default: throw new Exception(string.Format("Unknown field type: {0}", _Fields[j].Type.ToString("X2")));
                        }
                    }
            }

            private void WriteFieldAsFloat(VoidPtr fieldData, int field, float value)
            {
                Marshal.StructureToPtr((bfloat)value, fieldData, false);
            }

            private void WriteFieldAsWord(VoidPtr fieldData, int field, uint value)
            {
                uint data = (buint)Marshal.PtrToStructure(fieldData, typeof(buint));

                data |= ((value << _Fields[field].Shift) & _Fields[field].Mask);

                Marshal.StructureToPtr((buint)data, fieldData, false);
            }

            private void WriteFieldAsHalf(VoidPtr fieldData, int field, ushort value)
            {
                ushort data = (bushort)Marshal.PtrToStructure(fieldData, typeof(bushort));

                data |= (ushort)((value << _Fields[field].Shift) & _Fields[field].Mask);

                Marshal.StructureToPtr((bushort)data, fieldData, false);
            }

            private void WriteFieldAsByte(VoidPtr fieldData, int field, byte value)
            {
                byte data = (byte)Marshal.PtrToStructure(fieldData, typeof(byte));

                data |= (byte)((value << _Fields[field].Shift) & _Fields[field].Mask);

                Marshal.StructureToPtr(data, fieldData, false);
            }

            private void WriteFieldAsString(VoidPtr fieldData, int field, string value)
            {
                Marshal.StructureToPtr((buint)_StringKey[value], fieldData, false);
            }

            private void WriteStrings(VoidPtr data)
            {
                byte* stringData = (byte*)(data + Marshal.SizeOf(typeof(Header)) + Marshal.SizeOf(typeof(Field)) * _Fields.Length + _ItemStride * _Items.Length);

                foreach (string key in _StringKey.Keys)
                {
                    foreach (byte b in _Encoding.GetBytes(key))
                        *stringData++ = b;

                    *stringData++ = 0x00;
                }
            }

            private void WritePadding(VoidPtr data)
            {
                byte* padData = (byte*)(data + _Length);
                int padLength = _RoundedLength - _Length;

                for (int i = 0; i < padLength; i++)
                    padData[i] = 0x40;
            }

            #region Static Members

            public static int GetFieldStride(int type)
            {
                switch (type)
                {
                    case 0x00: return Marshal.SizeOf(typeof(buint));
                    case 0x02: return Marshal.SizeOf(typeof(bfloat));
                    case 0x03: return Marshal.SizeOf(typeof(buint));
                    case 0x04: return Marshal.SizeOf(typeof(bushort));
                    case 0x05: return Marshal.SizeOf(typeof(byte));
                    case 0x06: return Marshal.SizeOf(typeof(buint));
                    default: throw new Exception(string.Format("Unknown field type: {0}", type.ToString("X2")));
                }
            }

            #endregion
        }
    }
}
