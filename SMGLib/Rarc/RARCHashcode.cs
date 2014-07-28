using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnarchGalaxy2.Rarc
{
    public static class RARCHashcode
    {
        public static ushort MakeHashcode(string str)
        {
            ushort hashcode = 0x0;
            foreach (char chr in str)
            {
                hashcode *= 0x3;
                hashcode += (ushort)chr;
            }

            return hashcode;
        }
    }
}
