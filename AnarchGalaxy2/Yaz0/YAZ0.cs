using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using AnarchGalaxy2.Lz77;

namespace AnarchGalaxy2.Yaz0
{
    public unsafe class YAZ0
    {
        private VoidPtr _Data = null;

        private YAZ0(VoidPtr data)
        {
            _Data = data;
        }

        public int GetDataLength()
        {
            Header* header = (Header*)_Data;

            return (int)header->_UncompressedSize;
        }

        public byte[] GetData()
        {
            Header* header = (Header*)_Data;
            VoidPtr data = _Data + Marshal.SizeOf(typeof(Header));

            return LZ77.Decompress(data, (int)header->_UncompressedSize);
        }

        #region Static Members

        public static YAZ0 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "Yaz0")
                throw new Exception("Data cannot be wrapped with the Yaz0 class.");

            return new YAZ0(ptr);
        }

        public static byte[] Decompress(byte[] data)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);

            byte[] result = Decompress(ptr);

            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public static byte[] Decompress(VoidPtr ptr)
        {
            YAZ0 yaz0 = YAZ0.Wrap(ptr);

            return yaz0.GetData();
        }

        public static byte[] Compress(byte[] data)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);

            byte[] result = Compress(ptr, data.Length);

            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public static byte[] Compress(VoidPtr ptr, int length)
        {
            Header header = new Header();
            header._Tag = TagExtension.ToTag("Yaz0");
            header._UncompressedSize = (uint)length;
            header._Pad1 = 0x00000000;
            header._Pad2 = 0x00000000;

            byte[] compressed = LZ77.Compress(ptr, length);

            int headerLength = Marshal.SizeOf(typeof(Header));
            int resultLength = headerLength + compressed.Length;
            byte[] result = new byte[resultLength];
            Marshal.Copy(new IntPtr(&header), result, 0, headerLength);
            Array.Copy(compressed, 0, result, headerLength, compressed.Length);

            return result;
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag;
            public buint _UncompressedSize;
            public buint _Pad1;
            public buint _Pad2;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        #endregion
    }
}
