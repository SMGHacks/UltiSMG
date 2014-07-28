using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Bdl4
{
    public unsafe class JNT1
    {
        public VoidPtr Data { get { return _Data; } }
        private VoidPtr _Data = 0;

        public uint Length { get { return ((Header*)_Data)->_Length; } }

        private JNT1(VoidPtr data)
        {
            _Data = data;
        }

        private VoidPtr GetJoint(int index)
        {
            Header* header = (Header*)_Data;
            Joint* joints = (Joint*)(_Data + header->_Joints);

            return joints + index;
        }

        public int GetJointCount()
        {
            Header* header = (Header*)_Data;

            return header->_JointCount;
        }

        public Vector3 GetJointScale(int index)
        {
            Joint* joint = (Joint*)GetJoint(index);
            
            return joint->_Scale;
        }

        public Vector3 GetJointRotation(int index)
        {
            Joint* joint = (Joint*)GetJoint(index);

            return new Vector3(joint->_Pitch, joint->_Yaw, joint->_Roll) / 0x8000 * (float)Math.PI;
        }

        public Vector3 GetJointTranslation(int index)
        {
            Joint* joint = (Joint*)GetJoint(index);

            return joint->_Translate;
        }

        public string GetJointName(int index)
        {
            Header* Header = (Header*)_Data;
            StringTable names = StringTable.Wrap(_Data + Header->_Strings);

            return names.GetString(index);
        }

        #region Static Members

        public static JNT1 Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag != "JNT1")
                throw new Exception("Data cannot be wrapped with the JNT1 class.");

            return new JNT1(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Header
        {
            public buint _Tag;
            public buint _Length;
            public bushort _JointCount;
            public bushort _Pad;

            public buint _Joints;
            public buint _Unks;
            public buint _Strings;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Joint
        {
            public bushort _Unk;
            public byte _Unk2;
            public byte _Pad;

            public BVec3 _Scale;
            
            public bshort _Pitch;
            public bshort _Yaw;
            public bshort _Roll;
            public bushort _Pad2;

            public BVec3 _Translate;

            public bfloat _Unk3;

            public BVec3 _BBMin;
            public BVec3 _BBMax;

        }

        #endregion
    }
}
