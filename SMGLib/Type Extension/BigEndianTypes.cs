using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{_data.Reverse()}", Type="bint")]
    public struct bint
    {
        public int _data;
        public static implicit operator int(bint val) { return val._data.Reverse(); }
        public static implicit operator bint(int val) { return new bint { _data = val.Reverse() }; }
        public static explicit operator uint(bint val) { return (uint)val._data.Reverse(); }
        public static explicit operator bint(uint val) { return new bint { _data = (int)val.Reverse() }; }
    }

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{_data.Reverse()}", Type="buint")]
    public struct buint
    {
        public uint _data;
        public static implicit operator uint(buint val) { return val._data.Reverse(); }
        public static implicit operator buint(uint val) { return new buint { _data = val.Reverse() }; }
        public static explicit operator int(buint val) { return (int)val._data.Reverse(); }
        public static explicit operator buint(int val) { return new buint { _data = (uint)val.Reverse() }; }
    }

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{_data.Reverse()}", Type = "bfloat")]
    public struct bfloat
    {
        public float _data;
        public static implicit operator float(bfloat val) { return val._data.Reverse(); }
        public static implicit operator bfloat(float val) { return new bfloat { _data = val.Reverse() }; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [DebuggerDisplay("{_data.Reverse()}", Type = "bshort")]
    public struct bshort
    {
        public short _data;
        public static implicit operator short(bshort val) { return val._data.Reverse(); }
        public static implicit operator bshort(short val) { return new bshort { _data = val.Reverse() }; }
        public static explicit operator ushort(bshort val) { return (ushort)val._data.Reverse(); }
        public static explicit operator bshort(ushort val) { return new bshort { _data = (short)val.Reverse() }; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [DebuggerDisplay("{_data.Reverse()}", Type = "bushort")]
    public struct bushort
    {
        public ushort _data;
        public static implicit operator ushort(bushort val) { return val._data.Reverse(); }
        public static implicit operator bushort(ushort val) { return new bushort { _data = val.Reverse() }; }
        public static explicit operator short(bushort val) { return (short)val._data.Reverse(); }
        public static explicit operator bushort(short val) { return new bushort { _data = (ushort)val.Reverse() }; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BVec2
    {
        public bfloat _x;
        public bfloat _y;

        public BVec2(float x, float y) { _x = x; _y = y; }

        public override string ToString() { return String.Format("({0}, {1})", (float)_x, (float)_y); }

        public static implicit operator Vector2(BVec2 v) { return new Vector2(v._x, v._y); }
        public static implicit operator BVec2(Vector2 v) { return new BVec2(v._x, v._y); }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BVec3
    {
        public bfloat _x;
        public bfloat _y;
        public bfloat _z;

        public BVec3(float x, float y, float z) { _x = x; _y = y; _z = z; }

        public override string ToString() { return String.Format("({0}, {1}, {2})", (float)_x, (float)_y, (float)_z); }

        public static implicit operator Vector3(BVec3 v) { return new Vector3(v._x, v._y, v._z); }
        public static implicit operator BVec3(Vector3 v) { return new BVec3(v._x, v._y, v._z); }
    }
}
