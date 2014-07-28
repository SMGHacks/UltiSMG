using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Lz77
{
    public unsafe static class LZ77
    {
        private const int WINDOWSIZE = 0x08;
        private const int MINBUFFERCOPY = 0x02;
        private const int MAXBUFFERCOPY = 0x111;
        private const int MINCOPYDIST = 0x01;
        private const int MAXCOPYDIST = 0x1000;

        public static byte[] Decompress(VoidPtr data, int resultLength)
        {
            DecompressionManager dm = new DecompressionManager();

            return dm.Decompress(data, resultLength);
        }

        public static byte[] Compress(byte[] data)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, ptr, data.Length);

            byte[] result = Compress(ptr, data.Length);

            Marshal.FreeHGlobal(ptr);

            return result;
        }

        public static byte[] Compress(VoidPtr data, int length)
        {
            CompressionManager cm = new CompressionManager();

            return cm.Compress(data, length);
        }

        private class DecompressionManager
        {
            private byte* _InPtr = (byte*)0;
            private byte* _OutPtr = (byte*)0;
            private byte* _EndPtr = (byte*)0;

            public byte[] Decompress(VoidPtr data, int resultLength)
            {
                VoidPtr outData = Marshal.AllocHGlobal(resultLength);
                _InPtr = (byte*)data;
                _OutPtr = (byte*)outData;
                _EndPtr = _OutPtr + resultLength;

                while (!IsDone())
                {
                    int copyFlags = *_InPtr++;

                    for (int i = 0; i < WINDOWSIZE && !IsDone(); i++)
                        if ((copyFlags & (0x80 >> i)) != 0x00)
                            LiteralCopy();
                        else
                            BufferCopy();
                }

                byte[] result = new byte[resultLength];
                Marshal.Copy(outData, result, 0, result.Length);
                Marshal.FreeHGlobal(outData);

                return result;
            }

            private void LiteralCopy()
            {
                *_OutPtr++ = *_InPtr++;
            }

            private void BufferCopy()
            {
                ushort half = *(bushort*)_InPtr;
                int length = (half & 0xF000) >> 0x0C;
                int distance = (half & 0x0FFF);
                _InPtr += 0x02;

                if (length == 0)
                    length = 0x10 + *_InPtr++;

                length += MINBUFFERCOPY;
                distance += MINCOPYDIST;

                byte* copyPtr = _OutPtr - distance;
                while (length-- != 0 && !IsDone())
                    *_OutPtr++ = *copyPtr++;
            }

            private bool IsDone()
            {
                return _OutPtr >= _EndPtr;
            }
        }

        private class CompressionManager
        {
            private byte* _InPtr = (byte*)0;
            private byte* _OutPtr = (byte*)0;
            private byte* _StartPtr = (byte*)0;
            private byte* _EndPtr = (byte*)0;

            public byte[] Compress(VoidPtr data, int length)
            {
                VoidPtr outData = Marshal.AllocHGlobal(length);
                _StartPtr = (byte*)data;
                _EndPtr = (byte*)data + length;
                _InPtr = (byte*)data;
                _OutPtr = (byte*)outData;

                while (!IsDone())
                {
                    byte* flagPtr = _OutPtr++;
                    *flagPtr = 0x00;

                    for (int i = 0; i < WINDOWSIZE & !IsDone(); i++)
                    {
                        byte* bestCopy = (byte*)0;
                        int bestLength = 0;

                        if (FindCopyString(_InPtr, out bestCopy, out bestLength))
                        {
                            int distance = (int)(_InPtr - bestCopy - MINCOPYDIST);
                            int count = bestLength - MINBUFFERCOPY;

                            if (count < 0x10)
                                WriteShortBufferCopy(distance, count);
                            else
                                WriteLongBufferCopy(distance, count);

                            _InPtr += bestLength;
                        }
                        else
                        {
                            *_OutPtr++ = *_InPtr++;

                            *flagPtr |= (byte)(0x80 >> i);
                        }
                    }
                }


                uint size = (uint)(_OutPtr - outData);
                byte[] result = new byte[(int)size];
                Marshal.Copy(outData, result, 0, result.Length);
                Marshal.FreeHGlobal(outData);

                return result;
            }

            private bool FindCopyString(byte* copyPtr, out byte* bestCopy, out int bestLength)
            {
                byte* start = (copyPtr - MAXCOPYDIST < _StartPtr ? _StartPtr : copyPtr - MAXCOPYDIST);
                byte* candidate = start;
                bestLength = MINBUFFERCOPY;
                bestCopy = (byte*)0;

                while (candidate < copyPtr)
                {
                    int length = 0;
                    
                    while (copyPtr + length < _EndPtr
                        && candidate[length] == copyPtr[length]
                        && length < MAXBUFFERCOPY)
                        {
                            length++;
                        }

                    if (length > bestLength)
                    {
                        bestLength = length;
                        bestCopy = candidate;
                    }

                    candidate++;
                }

                return bestCopy != (byte*)0;
            }

            private void WriteShortBufferCopy(int distance, int count)
            {
                *(bushort*)_OutPtr = (ushort)(distance | (count << 0x0C));
                _OutPtr += 0x02;
            }

            private void WriteLongBufferCopy(int distance, int count)
            {
                *(bushort*)_OutPtr = (ushort)distance;
                *(byte*)(_OutPtr + 0x02) = (byte)(count - 0x10);
                _OutPtr += 0x03;
            }

            private bool IsDone()
            {
                return _InPtr >= _EndPtr;
            }
        }
    }
}
