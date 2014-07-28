using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class BDL4
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }


        private BDL4(VoidPtr data)
        {
            _Data = data;
        }

        private VoidPtr GetSection(string tag)
        {
            Header* header = (Header*)_Data;
            VoidPtr ptr = _Data + Marshal.SizeOf(typeof(Header));

            for (int i = 0; i < header->_SectionCount; i++)
            {
                Section* section = (Section*)ptr;

                if (section->Tag == tag)
                    return ptr;
                else
                    ptr += section->_Length;
            }

            return 0;
        }

        public INF1 WrapINF1()
        {
            VoidPtr section = GetSection("INF1");

            if (section != 0)
                return INF1.Wrap(section);
            else
                return null;
        }

        public VTX1 WrapVTX1()
        {
            VoidPtr section = GetSection("VTX1");

            if (section != 0)
                return VTX1.Wrap(section);
            else
                return null;
        }

        public EVP1 WrapEVP1()
        {
            VoidPtr section = GetSection("EVP1");

            if (section != 0)
                return EVP1.Wrap(section);
            else
                return null;
        }

        public DRW1 WrapDRW1()
        {
            VoidPtr section = GetSection("DRW1");

            if (section != 0)
                return DRW1.Wrap(section);
            else
                return null;
        }

        public JNT1 WrapJNT1()
        {
            VoidPtr section = GetSection("JNT1");

            if (section != 0)
                return JNT1.Wrap(section);
            else
                return null;
        }

        public SHP1 WrapSHP1()
        {
            VoidPtr section = GetSection("SHP1");

            if (section != 0)
                return SHP1.Wrap(section);
            else
                return null;
        }

        public MAT3 WrapMAT3()
        {
            VoidPtr section = GetSection("MAT3");

            if (section != 0)
                return MAT3.Wrap(section);
            else
                return null;
        }

        public MDL3 WrapMDL3()
        {
            VoidPtr section = GetSection("MDL3");

            if (section != 0)
                return MDL3.Wrap(section);
            else
                return null;
        }

        public TEX1 WrapTEX1()
        {
            VoidPtr section = GetSection("TEX1");

            if (section != 0)
                return TEX1.Wrap(section);
            else
                return null;
        }

        #region Static Members

        public static BDL4 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag2 != "bdl4")
                throw new Exception("Data cannot be wrapped with the BDL4 class.");

            return new BDL4(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag1;     // J3D2
            public buint _Tag2;     // Bdl4
            public buint _Length;
            public buint _SectionCount;
            public buint _Tag3;     // SVR3

            public buint _Pad1;     // 0xFFFFFFFF
            public buint _Pad2;     // 0xFFFFFFFF
            public buint _Pad3;     // 0xFFFFFFFF

            public string Tag1 { get { return TagExtension.ToTag(_Tag1); } }
            public string Tag2 { get { return TagExtension.ToTag(_Tag2); } }
            public string Tag3 { get { return TagExtension.ToTag(_Tag3); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Section
        {
            public buint _Tag;
            public buint _Length;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        #endregion
    }
}
