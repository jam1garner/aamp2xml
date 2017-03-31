using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AAMP2XML
{
    public abstract class FileBase
    {
        public abstract Endianness Endian { get; set; }

        public abstract void Read(string filename);
        public abstract byte[] Rebuild();

        public void Save(string filename)
        {
            var Data = Rebuild();
            if (Data.Length <= 0)
                throw new Exception("Warning: Data was empty!");

            File.WriteAllBytes(filename, Data);
        }
    }
}

namespace System.IO
{
    public enum Endianness
    {
        Big = 0,
        Little = 1
    }
}
