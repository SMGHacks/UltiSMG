﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class MAT3
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private MAT3(VoidPtr data)
        {
            _Data = data;
        }

        #region Static Members

        public static MAT3 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "MAT3")
                throw new Exception("Data cannot be wrapped with the MAT3 class.");

            return new MAT3(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag;
            public buint _Length;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        #endregion
    }
}
