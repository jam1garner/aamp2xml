using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace AAMP2XML
{
    class Program
    {
        static void Main(string[] args)
        {
            string saveName;
            string arg = args[0];
            if (arg.EndsWith(".xml"))
            {
                saveName = Path.ChangeExtension(arg, ".aamp");
                XmlDocument xml = new XmlDocument();
                xml.Load(arg);
                AAMP aamp = AAMP.fromXML(xml);
                File.WriteAllBytes(saveName, aamp.Rebuild());
            }
            else
            {
                AAMP testAAMP = new AAMP(arg);
                saveName = Path.ChangeExtension(arg, ".xml");
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                XmlWriter writer = XmlWriter.Create(saveName, settings);
                testAAMP.ToXML().Save(writer);
            }
        }
    }
}
