using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class INF1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private INF1(VoidPtr data)
        {
            _Data = data;
        }

        public int GetEntryType(int index)
        {
            Header* header = (Header*)_Data;
            Entry* entries = (Entry*)(_Data + header->_Entries);

            return entries[index]._Type;
        }

        public int GetEntryIndex(int index)
        {
            Header* header = (Header*)_Data;
            Entry* entries = (Entry*)(_Data + header->_Entries);

            return entries[index]._Index;
        }

        #region Static Members

        public static INF1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "INF1")
                throw new Exception("Data cannot be wrapped with the INF1 class.");

            return new INF1(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Header
        {
            public buint _Tag;
            public buint _Length;
            public bushort _Unk1;
            public bushort _Pad; //  0xFFFF
            public buint _Unk2;
            public buint _VertexCount;
            public buint _Entries;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Entry
        {
            public bushort _Type;
            public bushort _Index;
        }

        #endregion
    }
}
