using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class TEX1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private TEX1(VoidPtr data)
        {
            _Data = data;
        }



        public StringTable GetNameTable()
        {
            Header* header = (Header*)_Data;

            return StringTable.Wrap(_Data + header->_Strings);
        }

        #region Static Members

        public static TEX1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "TEX1")
                throw new Exception("Data cannot be wrapped with the TEX1 class.");

            return new TEX1(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag;
            public buint _Length;
            public bushort _TextureCount;
            public bushort _Pad;
            public buint _Textures;
            public buint _Strings;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        #endregion
    }
}
