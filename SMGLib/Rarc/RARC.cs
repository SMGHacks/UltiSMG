
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AnarchGalaxy2.Rarc
{
    public unsafe class RARC
    {
        private VoidPtr _Data = null;

        public RARC(VoidPtr data)
        {
            _Data = data;
        }

        public int GetDirectoryCount()
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);

            return archive.GetDirectoryCount();
        }

        public string GetDirectoryName(int index)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);

            return archive.GetDirectoryName(index);
        }

        public int GetDirectoryEntryCount(int index)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);

            return archive.GetDirectoryEntryCount(index);
        }

        public int GetDirectoryEntryType(int index, int entry)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);
            DirectoryEntry directoryEntry = archive.GetDirectoryEntry(index, entry);

            return directoryEntry._Type;
        }

        public bool GetDirectoryEntryIsReal(int index, int entry)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);
            string name = archive.GetDirectoryEntryName(index, entry);

            return name != "." && name != "..";
        }

        public string GetDirectoryEntryName(int index, int entry)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);

            return archive.GetDirectoryEntryName(index, entry);
        }

        public int GetSubdirectoryIndex(int index, int entry)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);
            DirectoryEntry directoryEntry = archive.GetDirectoryEntry(index, entry);
            int type = directoryEntry._Type;

            if (directoryEntry._Type != 0x02)
                throw new Exception(string.Format("Entry is not a Subdirectory(0x02) type. ({0})", type.ToString("X2")));

            return directoryEntry._ContentOffset;
        }

        public byte[] GetSubcontent(int index, int entry)
        {
            Header* header = (Header*)_Data;
            Archive archive = Archive.Wrap(_Data + header->_HeaderLength);
            DirectoryEntry directoryEntry = archive.GetDirectoryEntry(index, entry);
            byte* content = (byte*)(_Data + header->_HeaderLength + header->_ArchiveLength);
            int type = directoryEntry._Type;
            int contentOffset = directoryEntry._ContentOffset;

            if (type != 0x11)
                throw new Exception(string.Format("Entry is not a Content(0x11) type. ({0})", type.ToString("X2")));

            byte[] result = new byte[directoryEntry._Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = content[contentOffset + i];

            return result;
        }

        #region Static Members

        public static RARC Wrap(VoidPtr ptr)
        {
            if ((*(Header*)ptr).Tag == "Yaz0")
                throw new Exception("Data has been compressed into a Yaz0 archive.");

            if ((*(Header*)ptr).Tag != "RARC")
                throw new Exception("Data cannot be wrapped with the RARC class.");

            return new RARC(ptr);
        }

        #endregion

        #region Wrappers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Header
        {
            public buint _Tag;
            public bint _FileLength;
            public bint _HeaderLength;
            public bint _ArchiveLength;
            public bint _ContentLength1;
            public bint _ContentLength2;
            public buint _Padding1;
            public buint _Padding2;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ArchiveHeader
        {
            public bint _DirectoryCount;
            public buint _Directories;
            public bint _EntryCount;
            public buint _Entries;
            public bint _NamesLength;
            public buint _Names;
            public buint _Unk;
            public buint _Padding;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Directory
        {
            public buint _Tag;
            public bint _NameOffset;
            public bushort _NameHashcode;
            public bshort _EntryCount;
            public bint _IndexStart;

            public string Tag { get { return TagExtension.ToTag(_Tag); } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DirectoryEntry
        {
            public bushort _ContentIndex;
            public bushort _NameHashcode;
            public byte _Type;
            public byte _Unk;
            public bushort _NameOffset;

            // public bint _FolderIndex;
            public bint _ContentOffset;

            public bint _Length;
            public buint _ContentPtr; // Live only;
        }

        #endregion

        private class Archive
        {
            private VoidPtr _Data = null;

            public Archive(VoidPtr data)
            {
                _Data = data;
            }

            public int GetDirectoryCount()
            {
                ArchiveHeader* header = (ArchiveHeader*)_Data;

                return header->_DirectoryCount;
            }

            public string GetDirectoryName(int index)
            {
                ArchiveHeader* header = (ArchiveHeader*)_Data;
                Directory* directories = (Directory*)(_Data + header->_Directories);
                VoidPtr names = _Data + header->_Names;
                int nameOffset = directories[index]._NameOffset;

                string result = Encoding.GetEncoding("Shift-JIS").GetString(names + nameOffset);

                return result;
            }

            public int GetDirectoryEntryCount(int index)
            {
                ArchiveHeader* header = (ArchiveHeader*)_Data;
                Directory* directories = (Directory*)(_Data + header->_Directories);

                return directories[index]._EntryCount;
            }

            public DirectoryEntry GetDirectoryEntry(int index, int entry)
            {
                ArchiveHeader* header = (ArchiveHeader*)_Data;
                DirectoryEntry* entries = (DirectoryEntry*)(_Data + header->_Entries);
                Directory* directories = (Directory*)(_Data + header->_Directories);
                int startIndex = directories[index]._IndexStart;

                return entries[startIndex + entry];
            }

            public string GetDirectoryEntryName(int index, int entry)
            {
                ArchiveHeader* header = (ArchiveHeader*)_Data;
                DirectoryEntry* entries = (DirectoryEntry*)(_Data + header->_Entries);
                Directory* directories = (Directory*)(_Data + header->_Directories);
                VoidPtr names = _Data + header->_Names;
                int startIndex = directories[index]._IndexStart;

                int nameOffset = entries[startIndex + entry]._NameOffset;
                string result = Encoding.GetEncoding("Shift-JIS").GetString(names + nameOffset);

                return result;
            }



            #region Static Members

            public static Archive Wrap(VoidPtr ptr)
            {
                ArchiveHeader* header = (ArchiveHeader*)ptr;
                bool invalid = false;

                invalid |= header->_DirectoryCount < 0;
                invalid |= header->_Directories < Marshal.SizeOf(typeof(ArchiveHeader));
                invalid |= header->_EntryCount < 0;
                invalid |= header->_Entries < Marshal.SizeOf(typeof(ArchiveHeader));
                invalid |= header->_NamesLength < 0;
                invalid |= header->_Names < Marshal.SizeOf(typeof(ArchiveHeader));

                if (invalid)
                    throw new Exception("Data cannot be wrapped with the Archive class.");

                return new Archive(ptr);
            }

            #endregion
        }
    }
}
