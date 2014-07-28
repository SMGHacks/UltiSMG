using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AnarchGalaxy2.Bcsv
{
    public static class FieldHashcode
    {
        private static Dictionary<uint, string> _HashKey;

        static FieldHashcode()
        {
            _HashKey = new Dictionary<uint, string>();
        }

        public static uint MakeHashcode(string str)
        {
            uint hashcode = 0x0;
            foreach (char chr in str)
            {
                hashcode *= 0x1F;
                hashcode += (uint)chr;
            }

            return hashcode;
        }

        public static string LookupHashcode(uint hashCode)
        {
            string name = null;
            if (!_HashKey.TryGetValue(hashCode, out name))
                return string.Format("[{0}]", hashCode.ToString("X8"));

            return name;
        }

        public static void LoadHashKey(string path)
        {
            string[] fieldNames = File.ReadAllLines(path);

            _HashKey.Clear();
            foreach (string fieldName in fieldNames)
            {
                uint hashcode = MakeHashcode(fieldName);

                if (!_HashKey.ContainsKey(hashcode))
                    _HashKey.Add(hashcode, fieldName);
            }
        }
    }
}
