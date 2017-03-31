using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AAMP2XML
{
    class Program
    {
        static void Main(string[] args)
        {
            AAMP testAAMP = new AAMP(args[0]);
            //File.WriteAllText("dump.txt", aampDump("", testAAMP.RootNode, 0));
            testAAMP.ToXML().Save("dump.xml");
        }
    }
}
