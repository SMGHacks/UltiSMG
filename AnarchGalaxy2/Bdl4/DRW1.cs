using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class DRW1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private DRW1(VoidPtr data)
        {
            _Data = data;
        }

        public int GetTransformRigCount()
        {
            Header* header = (Header*)_Data;
            return header->_RigCount;
        }

        public int GetTransformRigType(int transform)
        {
            Header* header = (Header*)_Data;
            byte* rigTypes = (byte*)(_Data + header->_RigTypes);

            return rigTypes[transform];
        }

        public int GetTransformRigIndex(int transform)
        {
            Header* header = (Header*)_Data;
            bushort* rigIndices = (bushort*)(_Data + header->_RigIndices);

            return rigIndices[transform];
        }

        #region Static Members

        public static DRW1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "DRW1")
                throw new Exception("Data cannot be wrapped with the DRW1 class.");

            return new DRW1(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag;
            public buint _Length;
            public bshort _RigCount;
            public bushort _Pad;

            public buint _RigTypes;
            public buint _RigIndices;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        #endregion
    }
}
