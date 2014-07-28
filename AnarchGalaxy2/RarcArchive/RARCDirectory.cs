using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using AnarchGalaxy2.Rarc;
using AnarchGalaxy2.Yaz0;

namespace AnarchGalaxy2.RarcArchive
{
    [DebuggerDisplay("Directory: { Name }")]
    public unsafe class RARCDirectory : RARCArchiveEntry
    {
        public List<RARCArchiveEntry> Children { get { return _Children; } set { _Children = value; } }
        private List<RARCArchiveEntry> _Children = null;

        public RARCDirectory() : this("") { }
        public RARCDirectory(string name)
            : base(name)
        {
            _Children = new List<RARCArchiveEntry>();
        }

        public RARCArchiveEntry[] GetAllChildren()
        {
            List<RARCArchiveEntry> result = new List<RARCArchiveEntry>();

            foreach (RARCArchiveEntry child in _Children)
            {
                result.Add(child);

                if (child is RARCDirectory)
                    result.AddRange((child as RARCDirectory).GetAllChildren());
            }

            return result.ToArray();
        }

        public RARCArchiveEntry NavigateFirst(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            
            string[] terms = path.Split('\\');

            IEnumerable<RARCArchiveEntry> children = _Children.Where(c => c.Name == terms[0]);

            if (terms.Length > 1)
                foreach (RARCDirectory subdir in children.OfType<RARCDirectory>())
                {
                    RARCArchiveEntry result = subdir.NavigateFirst(string.Join("\\", terms, 1, terms.Length - 1));

                    if (result != null)
                        return result;
                }

            if (terms.Length == 1)
                return children.FirstOrDefault();

            return null;
        }

        public RARCArchiveEntry[] NavigateAll(string path)
        {
            if (string.IsNullOrEmpty(path))
                return new RARCArchiveEntry[0];

            string[] terms = path.Split('\\');

            IEnumerable<RARCArchiveEntry> children = _Children.Where(c => c.Name == terms[0]);
            List<RARCArchiveEntry> result = new List<RARCArchiveEntry>();

            if (terms.Length > 1)
                foreach (RARCDirectory subdir in children.OfType<RARCDirectory>())
                    result.AddRange(subdir.NavigateAll(string.Join("\\", terms, 1, terms.Length - 1)));

            if (terms.Length == 1)
                result.AddRange(children);

            return result.ToArray();
        }

        #region Static Members

        public static RARCDirectory FromFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            return FromPtr(bytes);
        }

        public static RARCDirectory FromPtr(byte[] bytes)
        {
            VoidPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            RARCDirectory result = FromPtr(ptr);
            Marshal.FreeHGlobal(ptr);

            return result;
        }

        public static RARCDirectory FromPtr(VoidPtr ptr)
        {
            if (TagExtension.ToTag(*(buint*)ptr) == "Yaz0")
                return FromCompressed(ptr);
            else
                return FromUncompressed(ptr);
        }

        public static RARCDirectory FromCompressed(VoidPtr ptr)
        {
            YAZ0 yaz0 = YAZ0.Wrap(ptr);
            byte[] bytes = yaz0.GetData();

            VoidPtr uncompressed = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, uncompressed, bytes.Length);

            RARCDirectory result = FromUncompressed(uncompressed);
            Marshal.FreeHGlobal(uncompressed);

            return result;
        }

        public static RARCDirectory FromUncompressed(VoidPtr ptr)
        {
            RARC rarc = RARC.Wrap(ptr);

            return GetDirectory(rarc, 0);
        }

        private static RARCDirectory GetDirectory(RARC rarc, int index)
        {
            RARCDirectory result = new RARCDirectory();
            result.Name = rarc.GetDirectoryName(index);

            for (int i = 0; i < rarc.GetDirectoryEntryCount(index); i++)
                if (rarc.GetDirectoryEntryIsReal(index, i))
                {
                    int type = rarc.GetDirectoryEntryType(index, i);
                    RARCArchiveEntry child = null;

                    switch (type)
                    {
                        case 0x02: child = GetDirectory(rarc, rarc.GetSubdirectoryIndex(index, i)); break;
                        case 0x11: child = GetFile(rarc, index, i); break;
                        default: throw new Exception(string.Format("Unknown entry type. ({0})", type));
                    }

                    result.Children.Add(child);
                }

            return result;
        }

        private static RARCFile GetFile(RARC rarc, int index, int entry)
        {
            byte[] data = rarc.GetSubcontent(index, entry);
            string name = rarc.GetDirectoryEntryName(index, entry);
            RARCFile result = RARCFile.FromPtr(data, name);

            return result;
        }

        #endregion
    }
}
