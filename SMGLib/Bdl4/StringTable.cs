using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class StringTable
    {
        public static readonly Encoding _TableEncoding = Encoding.GetEncoding("Shift-JIS");

        private VoidPtr _Data = 0;

        private StringTable(VoidPtr data)
        {
            _Data = data;
        }

        public uint GetEntryHashCode(int index)
        {
            StringEntry* entries = (StringEntry*)(_Data + 0x04);

            return entries[index]._Hash;
        }

        public string GetString(int index)
        {
            StringEntry* entries = (StringEntry*)(_Data + 0x04);
            VoidPtr str = _Data + entries[index]._Offset;

            return _TableEncoding.GetString(str);
        }

        #region Static Members

        public static StringTable Wrap(VoidPtr ptr)
        {
            return new StringTable(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            bushort _Count;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct StringEntry
        {
            public bushort _Hash;
            public bushort _Offset;
        }

        #endregion
    }
}
