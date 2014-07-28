using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class SHP1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private SHP1(VoidPtr data)
        {
            _Data = data;
        }

        private int GetSubsetVertexStride(int subset)
        {
            Header* header = (Header*)_Data;
            Subset* subsets = (Subset*)(_Data + header->_Subsets);
            Format* format = (Format*)(_Data + header->_Formats + subsets[subset]._FormatOffset);

            int result = 0;
            for (int i = 0; i < 0x0D && format[i]._DataType != 0xFF; i++)
                result += Marshal.SizeOf(GetElementType(format[i]._ElementType));

            return result;
        }

        private int[] GetTransformSet(int index)
        {
            Header* header = (Header*)_Data;
            bshort* transforms = (bshort*)(_Data + header->_Transforms);
            TransformSet* transformSets = (TransformSet*)(_Data + header->_TransformSets);
            int start = (int)transformSets[index]._TransformStartIndex;
            int count = transformSets[index]._TransformCount;

            int[] result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = transforms[start + i];
            
            return result;
        }

        public int[] GetSubsetVertexFormat(int subset)
        {
            Header* header = (Header*)_Data;
            Subset* subsets = (Subset*)(_Data + header->_Subsets);
            Format* format = (Format*)(_Data + header->_Formats + subsets[subset]._FormatOffset);
            
            List<int> result = new List<int>();
            for (int i = 0; i < 0x0D && format[i]._DataType != 0xFF; i++)
                if (format[i]._DataType != 0x00) 
                    result.Add(format[i]._DataType);

            return result.ToArray();
        }

        public bool GetSubsetHasSkinning(int subset)
        {
            Header* header = (Header*)_Data;
            Subset* subsets = (Subset*)(_Data + header->_Subsets);
            Format* format = (Format*)(_Data + header->_Formats + subsets[subset]._FormatOffset);
            bool hasSkinning = false;

            for (int i = 0; i < 0x0D && format[i]._DataType != 0xFF; i++)
                hasSkinning |= format[i]._DataType == 0;

            return hasSkinning;
        }

        public int[][] GetSubsetTransformSets(int subset)
        {
            Header* header = (Header*)_Data;
            Subset* subsets = (Subset*)(_Data + header->_Subsets);
            int start = subsets[subset]._PacketStartIndex;
            int count = subsets[subset]._PacketCount;

            int[][] result = new int[count][];
            for (int i = 0; i < count; i++)
                result[i] = GetTransformSet(start + i);

            return result;
        }

        public Polygon[][] GetSubsetPackets(int subset)
        {
            Header* header = (Header*)_Data;
            Subset* subsets = (Subset*)(_Data + header->_Subsets);
            Packet* packets = (Packet*)(_Data + header->_Packets);
            Format* format = (Format*)(_Data + header->_Formats + subsets[subset]._FormatOffset);
            VoidPtr polygonData = _Data + header->_PolygonData;
            int vertexStride = GetSubsetVertexStride(subset);
            int start = subsets[subset]._PacketStartIndex;
            int count = subsets[subset]._PacketCount;

            Polygon[][] result = new Polygon[count][];
            for (int i = 0; i < count; i++)
            {
                List<Polygon> polygons = new List<Polygon>();

                VoidPtr ptr = polygonData + packets[start + i]._PolygonDataOffset;
                uint length = packets[start + i]._PolygonDataLength;
                int offset = 0;
                while (offset < length && *(byte*)(ptr + offset) != 0)
                {
                    Polygon polygon = Polygon.Wrap(ptr + offset, format);
                    polygons.Add(polygon);

                    offset += Marshal.SizeOf(typeof(PolygonHeader));
                    offset += polygon.GetVertexCount() * vertexStride;
                }

                result[i] = polygons.ToArray();
            }

            return result;
        }

        #region Static Memebers

        public static SHP1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "SHP1")
                throw new Exception("Data cannot be wrapped with the SHP1 class.");

            return new SHP1(ptr);
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
        internal struct Header
        {
            public buint _Tag;  // SHP1
            public buint _Length;
            public bushort _SubsetCount;
            public bushort _Pad1;// 0xFFFF
            public buint _Subsets;
            public buint _Unks;
            public buint _Pad2; // 0x00000000

            public buint _Formats;
            public buint _Transforms;

            public buint _PolygonData;
            public buint _TransformSets;
            public buint _Packets;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Subset
        {
            public byte _MatrixType;
            public byte _Pad1;      // 0xFF

            public bushort _PacketCount;
            public bushort _FormatOffset;

            // Attributes Pairs
            public bushort _TransformSetStartIndex;
            public bushort _PacketStartIndex;

            public bushort _Pad2;    // 0xFFFF

            public bfloat _Unk;

            public BVec3 _BBMin;
            public BVec3 _BBMax;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Format
        {
            public bint _DataType;
            public bint _ElementType;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TransformSet
        {
            public bushort _Unk;
            public bushort _TransformCount;
            public buint _TransformStartIndex;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Packet
        {
            public buint _PolygonDataLength;
            public buint _PolygonDataOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PolygonHeader
        {
            public byte _PrimitiveType;
            public bushort _VertexCount;
        }

        #endregion
    }
}