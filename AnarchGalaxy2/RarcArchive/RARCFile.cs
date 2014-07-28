using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.RarcArchive
{
    [DebuggerDisplay("File: { Name }")]
    public class RARCFile : RARCArchiveEntry
    {
        public byte[] Data { get { return _Data; } set { _Data = value; } }
        private byte[] _Data = null;

        public RARCFile(byte[] data) : this("", data) { }
        public RARCFile(string name, byte[] data)
            : base(name)
        {
            _Data = data;
        }

        public int GetDataLength()
        {
            return _Data.Length;
        }

        #region Static Members

        public static RARCFile FromFile(string path)
        {
            return FromFile(path, Path.GetFileName(path));
        }

        public static RARCFile FromFile(string path, string name)
        {
            byte[] bytes = File.ReadAllBytes(path);

            return new RARCFile(name, bytes);
        }

        public static RARCFile FromPtr(byte[] bytes)
        {
            return new RARCFile(bytes);
        }

        public static RARCFile FromPtr(byte[] bytes, string name)
        {
            return new RARCFile(name, bytes);
        }

        public static RARCFile FromPtr(VoidPtr ptr, int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);

            return new RARCFile(bytes);
        }

        public static RARCFile FromPtr(VoidPtr ptr, int byteCount, string name)
        {
            byte[] bytes = new byte[byteCount];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);

            return new RARCFile(name, bytes);
        }

        #endregion
    }
}
