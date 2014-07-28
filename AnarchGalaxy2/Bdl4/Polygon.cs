using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class Polygon
    {
        private VoidPtr _Data = 0;
        private VoidPtr _FormatData = 0;

        private Polygon(VoidPtr data, VoidPtr formatData)
        {
            _Data = data;
            _FormatData = formatData;
        }

        public int GetPrimitiveType()
        {
            Header* header = (Header*)_Data;

            return header->_PrimitiveType;
        }

        public int GetVertexCount()
        {
            Header* header = (Header*)_Data;

            return header->_VertexCount;
        }

        public int GetSkinIndex(int vertex)
        {
            return GetIndex(0, vertex);
        }

        public int GetIndex(int streamType, int vertex)
        {
            Format* format = (Format*)_FormatData;
            VoidPtr indices = (VoidPtr)(_Data + 0x03);
            int vertexStride = 0;
            Type elemType = null;
            int elemOffset = -1;

            for (int i = 0; i < 0x0D && format[i]._DataType != 0xFF; i++)
            {
                if (format[i]._DataType == streamType)
                {
                    elemType = GetElementType(format[i]._ElementType);
                    elemOffset = vertexStride;
                }

                vertexStride += Marshal.SizeOf(GetElementType(format[i]._ElementType));
            }

            if (elemType == null)
                throw new Exception("Index type not found.");

            return Convert.ToInt32(Marshal.PtrToStructure(indices[vertex, vertexStride] + elemOffset, elemType));
        }

        #region Static Members

        public static Polygon Wrap(VoidPtr ptr, VoidPtr formatPtr)
        {
            return new Polygon(ptr, formatPtr);
        }

        public static Type GetElementType(int elementType)
        {
            switch (elementType)
            {
                case 0x01: return typeof(byte);
                case 0x03: return typeof(bushort);
                default: throw new Exception("Unknown element type");
            }
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public byte _PrimitiveType;
            public bushort _VertexCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Format
        {
            public bint _DataType;
            public bint _ElementType;
        }

        #endregion
    }
}

