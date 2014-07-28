using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class EVP1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private EVP1(VoidPtr data)
        {
            _Data = data;
        }

        private int GetStartIndex(int envelope)
        {
            Header* header = (Header*)_Data;
            byte* counts = (byte*)(_Data + header->_JointCounts);
            int result = 0;

            for (int i = 0; i < envelope; i++)
                result += counts[i];

            return result;
        }

        public int GetEnvelopeCount()
        {
            Header* header = (Header*)_Data;

            return header->_EnvelopeCount;
        }

        public int GetJointCount(int envelope)
        {
            Header* header = (Header*)_Data;
            byte* counts = (byte*)(_Data + header->_JointCounts);

            return counts[envelope];
        }

        public int[] GetJointIndices(int envelope)
        {
            Header* header = (Header*)_Data;
            bushort* indices = (bushort*)(_Data + header->_JointIndices);
            int start = GetStartIndex(envelope);
            int count = GetJointCount(envelope);

            int[] result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = indices[start + i];

            return result;
        }

        public float[] GetJointWeights(int envelope)
        {
            Header* header = (Header*)_Data;
            bfloat* weights = (bfloat*)(_Data + header->_JointWeights);
            int start = GetStartIndex(envelope);
            int count = GetJointCount(envelope);

            float[] result = new float[count];
            for (int i = 0; i < count; i++)
                result[i] = weights[start + i];

            return result;
        }

        public float[,] GetMatrix(int matrix)
        {
            Header* header = (Header*)_Data;
            Matrix* matrices = (Matrix*)(_Data + header->_Matrices);
            float[,] result = new float[4, 3];

            for (int j = 0; j < 4; j++)
            {
                result[j, 0] = matrices[matrix][j, 0];
                result[j, 1] = matrices[matrix][j, 1];
                result[j, 2] = matrices[matrix][j, 2];
            }

            return result;
        }

        #region Static Members

        public static EVP1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "EVP1")
                throw new Exception("Data cannot be wrapped with the EVP1 class.");

            return new EVP1(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Header
        {
            public buint _Tag;
            public buint _Length;

            public bushort _EnvelopeCount;
            public bushort _Pad;

            public buint _JointCounts;
            public buint _JointIndices;
            public buint _JointWeights;
            public buint _Matrices;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x30)]
        private unsafe struct Matrix
        {
            //public fixed bfloat _Row1[4];
            //public fixed bfloat _Row2[4];
            //public fixed bfloat _Row3[4];

            public float this[int index] { get { return Get(index); } set { Set(index, value); } }
            public float this[int row, int column] { get { return Get(row, column); } set { Set(row, column, value); } }


            public float Get(int index)
            {
                fixed (void* ptr = &this)
                    return ((bfloat*)ptr)[index];
            }

            public float Get(int row, int column)
            {
                fixed (void* ptr = &this)
                    return ((bfloat*)ptr)[column * 4 + row];
            }

            public void Set(int index, float value)
            {
                fixed (void* ptr = &this)
                    ((bfloat*)ptr)[index] = value;
            }

            public void Set(int row, int column, float value)
            {
                fixed (void* ptr = &this)
                    ((bfloat*)ptr)[column * 4 + row] = value;
            }
        }

        #endregion
    }
}
