using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using AnarchGalaxy2.Rarc;

namespace AnarchGalaxy2.RarcArchive
{
    [DebuggerDisplay("Entry: { Name }")]
    public abstract class RARCArchiveEntry
    {
        public string Name { get { return _Name; } set { _Name = value; } }
        private string _Name = "";

        public RARCArchiveEntry()
        {

        }

        public RARCArchiveEntry(string name)
        {
            _Name = name;
        }
    }
}
