using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using AnarchGalaxy2.Rarc;
using AnarchGalaxy2.Yaz0;

namespace AnarchGalaxy2.RarcArchive
{
    public unsafe static class RARCBuilder
    {
        public static byte[] BuildRARC(RARCDirectory root, bool yaz0Compression)
        {
            BuildManager bm = new BuildManager(root);
            byte[] result = bm.Build();

            if (yaz0Compression)
                result = YAZ0.Compress(result);

            return result;
        }

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

        private class BuildManager
        {
            private RARCDirectory _Root = null;

            private Encoding _Encoding = null;

            private int _DirectoryCount = 0;
            private int _EntryCount = 0;
           
            private int _ContentLength = 0;
            
            

            private int _StringKeyLength = 0;
            private Dictionary<RARCArchiveEntry, int> _StringKey = null;

            private int _DirectoryIndex = 0;
            private int _ContentIndex = 0;
            private int _LevelIndex = 0;

            private int _Length = 0;

            public BuildManager(RARCDirectory root)
            {
                _Root = root;
                _Encoding = Encoding.GetEncoding("Shift-JIS");
            }

            public byte[] Build()
            {
                BuildStringKey();

                Directory[] directories = BuildDirectories();
                DirectoryEntry[] entries = BuildEntries();
                ArchiveHeader archiveHeader = BuildArchiveHeader();
                Header header = BuildHeader();

                _Length = header._HeaderLength;
                _Length += header._ArchiveLength;
                _Length += header._ContentLength1;

                VoidPtr ptr = Marshal.AllocHGlobal(_Length);

                WriteZero(ptr);
                WriteHeader(ptr, header);
                WriteArchiveHeader(ptr, archiveHeader);
                WriteDirectories(ptr, directories);
                WriteEntries(ptr, entries);
                WriteStrings(ptr);
                WriteContent(ptr);

                byte[] result = new byte[_Length];
                Marshal.Copy(ptr, result, 0, result.Length);
                Marshal.FreeHGlobal(ptr);

                return result;
            }

            private Header BuildHeader()
            {
                Header result = new Header();

                int headerLength = Marshal.SizeOf(typeof(Header));
                int archiveLength = GetArchiveLength();
                int fileLength = headerLength + archiveLength + _ContentLength;

                result._Tag = TagExtension.ToTag("RARC");
                result._FileLength = fileLength;
                result._HeaderLength = headerLength;
                result._ArchiveLength = archiveLength;
                result._ContentLength1 = _ContentLength;
                result._ContentLength2 = _ContentLength;
                result._Padding1 = 0;
                result._Padding2 = 0;

                return result;
            }

            private ArchiveHeader BuildArchiveHeader()
            {
                uint headerLength = (uint)Marshal.SizeOf(typeof(ArchiveHeader));
                uint directories = headerLength;
                uint directoriesLength = (uint)((Marshal.SizeOf(typeof(Directory)) * _DirectoryCount + 0x1F) & ~0x1F);
                uint entries = directories + directoriesLength;
                uint entriesLength = (uint)((Marshal.SizeOf(typeof(DirectoryEntry)) * _EntryCount + 0x1F) & ~0x1F);
                uint names = entries + entriesLength;
                int namesLength = _StringKeyLength;

                ArchiveHeader result = new ArchiveHeader();
                result = new ArchiveHeader();
                result._DirectoryCount = _DirectoryCount;
                result._Directories = directories;
                result._EntryCount = _EntryCount;
                result._Entries = entries;
                result._Names = names;
                result._NamesLength = namesLength;
                result._Unk = 0;
                result._Padding = 0;

                return result;
            }

            private Directory[] BuildDirectories()
            {
                List<Directory> result = new List<Directory>();

                Directory dir = new Directory();
                dir._Tag = TagExtension.ToTag("ROOT");
                dir._NameOffset = _StringKey[_Root];
                dir._NameHashcode = RARCHashcode.MakeHashcode(_Root.Name);
                dir._EntryCount = (short)(_Root.Children.Count + 2);
                dir._IndexStart = 0;
                result.Add(dir);

                _DirectoryCount = 1;
                _EntryCount = _Root.Children.Count;
                _EntryCount++; // ".";
                _EntryCount++; // "..";

                foreach (RARCDirectory d in _Root.Children.OfType<RARCDirectory>())
                    result.AddRange(BuildDirectories(d));

                return result.ToArray();
            }

            private Directory[] BuildDirectories(RARCDirectory directory)
            {
                List<Directory> result = new List<Directory>();

                Directory dir = new Directory();
                dir._Tag = TagExtension.ToTag(directory.Name.ToUpper());
                dir._NameOffset = _StringKey[directory];
                dir._NameHashcode = RARCHashcode.MakeHashcode(directory.Name);
                dir._EntryCount = (short)(directory.Children.Count + 2);
                dir._IndexStart = _EntryCount;
                result.Add(dir);

                _DirectoryCount++;
                _EntryCount += directory.Children.Count;
                _EntryCount++; // ".";
                _EntryCount++; // "..";

                foreach (RARCDirectory d in directory.Children.OfType<RARCDirectory>())
                    result.AddRange(BuildDirectories(d));

                return result.ToArray();
            }

            private DirectoryEntry[] BuildEntries()
            {
                List<DirectoryEntry> result = new List<DirectoryEntry>();

                _DirectoryIndex = 1;
                _ContentIndex = 0;
                _LevelIndex = 1;

                List<DirectoryEntry> tail = new List<DirectoryEntry>();
                foreach (RARCArchiveEntry entry in _Root.Children)
                {
                    result.Add(BuildEntry(entry));

                    if (entry is RARCDirectory)
                        tail.AddRange(BuildEntries(entry as RARCDirectory, 0));
                }

                result.Add(BuildCurrentLevelEntry(0));
                result.Add(BuildUpLevelEntry(-1));
                result.AddRange(tail);

                return result.ToArray();
            }

            private DirectoryEntry[] BuildEntries(RARCDirectory directory, int upLevelIndex)
            {
                List<DirectoryEntry> result = new List<DirectoryEntry>();
                int currentLevelIndex = _LevelIndex;

                _LevelIndex++;

                List<DirectoryEntry> tail = new List<DirectoryEntry>();
                foreach (RARCArchiveEntry entry in directory.Children)
                {
                    result.Add(BuildEntry(entry));

                    if (entry is RARCDirectory)
                        tail.AddRange(BuildEntries(entry as RARCDirectory, currentLevelIndex));
                }

                result.Add(BuildCurrentLevelEntry(currentLevelIndex));
                result.Add(BuildUpLevelEntry(upLevelIndex));

                result.AddRange(tail);

                return result.ToArray();
            }

            private DirectoryEntry BuildEntry(RARCArchiveEntry entry)
            {
                if (entry is RARCDirectory)
                    return BuildEntry(entry as RARCDirectory);
                else
                    return BuildEntry(entry as RARCFile);
            }

            private DirectoryEntry BuildEntry(RARCDirectory directory)
            {
                DirectoryEntry ent = new DirectoryEntry();
                ent._ContentIndex = 0xFFFF;
                ent._NameHashcode = RARCHashcode.MakeHashcode(directory.Name);
                ent._Type = 0x02;
                ent._Unk = 0x00;
                ent._NameOffset = (ushort)_StringKey[directory];
                ent._ContentOffset = _DirectoryIndex;
                ent._Length = 0x10;
                ent._ContentPtr = 0;

                _DirectoryIndex++;

                return ent;
            }

            private DirectoryEntry BuildEntry(RARCFile file)
            {
                DirectoryEntry ent = new DirectoryEntry();
                ent._ContentIndex = (ushort)_ContentIndex;
                ent._NameHashcode = RARCHashcode.MakeHashcode(file.Name);
                ent._Type = 0x11;
                ent._Unk = 0x00;
                ent._NameOffset = (ushort)_StringKey[file];
                ent._ContentOffset = _ContentLength;
                ent._Length = file.GetDataLength();
                ent._ContentPtr = 0;

                _ContentIndex++;
                _ContentLength += (file.GetDataLength() + 0x1F) & ~0x1F;
               
                return ent;
            }

            private DirectoryEntry BuildCurrentLevelEntry(int directoryIndex)
            {
                DirectoryEntry result = new DirectoryEntry();
                result._ContentIndex = 0xFFFF;
                result._NameHashcode = RARCHashcode.MakeHashcode(".");
                result._Type = 0x02;
                result._Unk = 0x00;
                result._NameOffset = 0x00;
                result._ContentOffset = directoryIndex;
                result._Length = 0x10;
                result._ContentPtr = 0;

                return result;
            }

            private DirectoryEntry BuildUpLevelEntry(int directoryIndex)
            {
                DirectoryEntry result = new DirectoryEntry();
                result._ContentIndex = 0xFFFF;
                result._NameHashcode = RARCHashcode.MakeHashcode("..");
                result._Type = 0x02;
                result._Unk = 0x00;
                result._NameOffset = 0x02;
                result._ContentOffset = directoryIndex;
                result._Length = 0x10;
                result._ContentPtr = 0;

                return result;
            }

            private void BuildStringKey()
            {
                _StringKey = new Dictionary<RARCArchiveEntry, int>();
                _StringKeyLength = 0;

                _StringKeyLength += _Encoding.GetByteCount(".") + 1;
                _StringKeyLength += _Encoding.GetByteCount("..") + 1;

                BuildStringKey(_Root);

                _StringKeyLength = (_StringKeyLength + 0x1F) & ~0x1F;
            }

            private void BuildStringKey(RARCArchiveEntry entry)
            {
                _StringKey.Add(entry, _StringKeyLength);
                _StringKeyLength += _Encoding.GetByteCount(entry.Name) + 1;

                if (entry is RARCDirectory)
                    foreach (RARCArchiveEntry child in (entry as RARCDirectory).Children)
                        BuildStringKey(child);
            }

            private int GetArchiveLength()
            {
                int headerLength = Marshal.SizeOf(typeof(ArchiveHeader));
                int directoriesLength = ((Marshal.SizeOf(typeof(Directory)) * _DirectoryCount + 0x1F) & ~0x1F);
                int entriesLength = ((Marshal.SizeOf(typeof(DirectoryEntry)) * _EntryCount + 0x1F) & ~0x1F);
                int namesLength = (_StringKeyLength + 0x1F) & ~0x1F;

                return headerLength + directoriesLength + entriesLength + namesLength;
            }

            private byte[][] GetContent(RARCDirectory directory)
            {
                List<byte[]> result = new List<byte[]>();

                foreach (RARCArchiveEntry entry in directory.Children)
                    if (entry is RARCDirectory)
                        result.AddRange(GetContent(entry as RARCDirectory));
                    else
                        result.Add((entry as RARCFile).Data);

                return result.ToArray();
            }

            private void WriteZero(VoidPtr data)
            {
                for (int i = 0; i < _Length; i++)
                    Marshal.WriteByte(data, i, 0);
            }

            private void WriteHeader(VoidPtr data, Header header)
            {
                Marshal.StructureToPtr(header, data, false);
            }

            private void WriteArchiveHeader(VoidPtr data, ArchiveHeader archiveHeader)
            {
                Header* header = (Header*)data;
                VoidPtr ptr = data + header->_HeaderLength;

                Marshal.StructureToPtr(archiveHeader, ptr, false);
            }

            private void WriteDirectories(VoidPtr data, Directory[] directories)
            {
                Header* header = (Header*)data;
                ArchiveHeader* archiveHeader = (ArchiveHeader*)(data + header->_HeaderLength);
                Directory* directoryData = (Directory*)(data + header->_HeaderLength + archiveHeader->_Directories);

                for (int i = 0; i < directories.Length; i++)
                    directoryData[i] = directories[i];
            }

            private void WriteEntries(VoidPtr data, DirectoryEntry[] entries)
            {
                Header* header = (Header*)data;
                ArchiveHeader* archiveHeader = (ArchiveHeader*)(data + header->_HeaderLength);
                DirectoryEntry* entryData = (DirectoryEntry*)(data + header->_HeaderLength + archiveHeader->_Entries);

                for (int i = 0; i < entries.Length; i++)
                    entryData[i] = entries[i];
            }

            private void WriteStrings(VoidPtr data)
            {
                Header* header = (Header*)data;
                ArchiveHeader* archiveHeader = (ArchiveHeader*)(data + header->_HeaderLength);
                byte* stringData = (byte*)(data + header->_HeaderLength + archiveHeader->_Names);

                foreach (byte b in _Encoding.GetBytes("."))
                    *stringData++ = b;
                *stringData++ = 0x00;

                foreach (byte b in _Encoding.GetBytes(".."))
                    *stringData++ = b;
                *stringData++ = 0x00;

                foreach (RARCArchiveEntry key in _StringKey.Keys)
                {
                    foreach (byte b in _Encoding.GetBytes(key.Name))
                        *stringData++ = b;

                    *stringData++ = 0x00;
                }
            }

            private void WriteContent(VoidPtr data)
            {
                Header* header = (Header*)data;
                VoidPtr contentData = data + header->_HeaderLength + header->_ArchiveLength;

                byte[][] content = GetContent(_Root);
                for (int i = 0; i < content.Length; i++)
                {
                    Marshal.Copy(content[i], 0, contentData, content[i].Length);

                    contentData += (content[i].Length + 0x1F) & ~0x1F;
                }
            }
        }
    }
}
