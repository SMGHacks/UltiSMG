using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class VTX1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }


        private VoidPtr[] _Streams = null;

        private VTX1(VoidPtr data)
        {
            _Data = data;
            _Streams = new VoidPtr[0x0C];

            Header* header = (Header*)_Data;
            Stream* streams = (Stream*)(_Data + header->_Streams);

            for (int i = 0; i < 0x0C && streams[i]._StreamType != 0xFF; i++)
                _Streams[streams[i]._StreamType - 0x09] = streams + i;
        }

        private VoidPtr GetStream(int streamType)
        {
            VoidPtr result = _Streams[streamType - 0x09];

            if (result == 0)
                throw new Exception("Stream not fount.");

            return result;
        }

        private VoidPtr GetStreamData(int streamType)
        {
            Header* header = (Header*)_Data;

            switch (streamType)
            {
                case 0x09: return _Data + header->_Position;
                case 0x0A: return _Data + header->_Normal;
                case 0x0B: return _Data + header->_Color1;
                case 0x0C: return _Data + header->_Color2;
                case 0x0D: return _Data + header->_Texture1;
                case 0x0E: return _Data + header->_Texture2;
                case 0x0F: return _Data + header->_Texture3;
                case 0x10: return _Data + header->_Texture4;
                case 0x11: return _Data + header->_Texture5;
                case 0x12: return _Data + header->_Texture6;
                case 0x13: return _Data + header->_Texture7;
                case 0x14: return _Data + header->_Texture8;
                default: throw new Exception("Invalid stream type.");
            }
        }

        public float[] ReadData(int streamType, int index)
        {
            Stream* stream = (Stream*)GetStream(streamType);
            VoidPtr streamData = GetStreamData(streamType);

            int elemCount = GetElementCount(stream->_StreamType);
            Type elemType = GetElementType(stream->_ElementType);
            int elemSize = Marshal.SizeOf(elemType);
            float precision = (float)Math.Pow(0.5f, stream->_Mantissa);

            float[] result = new float[4];

            // Handle extended vectors.
            elemCount += stream->_ElementExtension;
            
            VoidPtr ptr = streamData[index, elemCount * elemSize];
            for (int i = 0; i < elemCount; i++)
            {
                result[i] = Convert.ToSingle(Marshal.PtrToStructure(ptr[i, elemSize], elemType));
                result[i] *= precision;
            }

            return result;
        }

        #region Static Members

        public static VTX1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "VTX1")
                throw new Exception("Data cannot be wrapped with the VTX1 class.");

            return new VTX1(ptr);
        }

        public static int GetElementCount(int type)
        {
            switch (type)
            {
                case 0x09: return 2;    // Position (xy)
                case 0x0A: return 3;    // Normal   (xyz)
                case 0x0B:              // Color1   (rgb)
                case 0x0C: return 3;    // Color2   (rgb)
                case 0x0D:              // Texture1 (u)
                case 0x0E:              // Texture2 (u)
                case 0x0F:              // Texture3 (u)
                case 0x10:              // Texture4 (u)
                case 0x11:              // Texture5 (u)
                case 0x12:              // Texture6 (u)
                case 0x13:              // Texture7 (u)
                case 0x14: return 1;    // Texture8 (u)
                default: throw new Exception("Invalid stream type.");
            }
        }

        public static Type GetElementType(int elementType)
        {
            switch (elementType)
            {
                case 0x03: return typeof(bshort);
                case 0x04: return typeof(bfloat);
                case 0x05: return typeof(byte);
                default: throw new Exception("Unknown element type");
            }
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag;
            public buint _Length;
            public buint _Streams;

            // Stream Data
            public buint _Position;
            public buint _Normal;

            public buint _Blank;

            public buint _Color1;
            public buint _Color2;
            public buint _Texture1;
            public buint _Texture2;
            public buint _Texture3;
            public buint _Texture4;
            public buint _Texture5;
            public buint _Texture6;
            public buint _Texture7;
            public buint _Texture8;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Stream
        {
            public bint _StreamType;
            public bint _ElementExtension;
            public bint _ElementType;

            public byte _Mantissa;
            public sbyte _Pad1;   // 0xFF
            public bshort _Pad2;  // 0xFFFF
        }

        #endregion
    }
}
