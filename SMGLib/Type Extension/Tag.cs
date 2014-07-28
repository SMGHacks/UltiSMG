﻿using System;

namespace System
{
    public static class TagExtension
    {
        public static string ToTag(this uint val)
        {
            string str = "";
            for (int i = 3; i >= 0; i--)
                str += (char)((val >> (i * 0x8)) & 0xFF);

            return str;
        }

        public static uint ToTag(this string str)
        {
            if (str.Length >= 4)
                str = str.Substring(0, 4);
            else
                str = str.PadRight(4, ' ');

            uint tag = 0x00000000;
            for (int i = 3; i >= 0; i--)
                tag |= (uint)((sbyte)str[3 - i] << (i * 0x8));

            return tag;
        }
    }
}
