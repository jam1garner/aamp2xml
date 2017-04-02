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
            if (args[0].EndsWith(".xml"))
            {
                saveName = Path.ChangeExtension(args[0], ".aamp");
                XmlDocument xml = new XmlDocument();
                xml.Load(args[0]);
                AAMP aamp = AAMP.fromXML(xml);
                //aamp.Rebuild(); todo - write aamp rebuilding
            }
            else
            {
                AAMP testAAMP = new AAMP(args[0]);
                saveName = Path.ChangeExtension(args[0], ".xml");
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                XmlWriter writer = XmlWriter.Create(saveName, settings);
                testAAMP.ToXML().Save(writer);
            }
        }
    }
}
