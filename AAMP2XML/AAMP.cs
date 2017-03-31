using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

namespace AAMP2XML
{
    class AAMP
    {
        public AAMP(){ }

        public AAMP(string filename)
        {
            Read(new FileData(filename));
        }

        public AAMP(FileData f)
        {
            Read(f);
        }

        public class Node
        {
            public Node() { }

            public enum type
            {
                Boolean = 0x0,
                Float = 0x1,
                Int = 0x2,
                FloatPair = 0x3,
                FloatTriple = 0x4,

                FloatQuad = 0x6,
                String = 0x7
            }

            public type nodeType;
            public string name = null;
            public uint nameHash = 0;
            public List<Node> Children = new List<Node>();
            public object Value = null;
            public int ValueOffset;

            public void Read(FileData f)
            {
                nameHash = f.readUInt32();
                name = GetName(nameHash);
                int valueOffset = (f.readShort() * 4);
                byte childCount = (byte)f.readByte();
                nodeType = (type)f.readByte();
                int currentPostion = f.pos();
                f.seek(valueOffset + currentPostion - 8);
                ValueOffset = valueOffset + currentPostion - 8;
                if (childCount > 0)
                {
                    for(int i = 0; i < childCount; i++)
                    {
                        Node child = new Node();
                        child.Read(f);
                        Children.Add(child);
                    }
                }
                else
                {
                    /*nintendo*/switch (nodeType)
                    {
                        case type.Int:
                            Value = f.readUInt32();
                            break;
                        case type.Float:
                            Value = f.readFloat();
                            break;
                        case type.String:
                            Value = f.readString();
                            break;
                        case type.Boolean:
                            Value = (f.readByte() != 0);
                            break;
                        case type.FloatPair:
                            Value = new float[2] { f.readFloat(), f.readFloat() };
                            break;
                        case type.FloatTriple:
                            Value = new float[3] { f.readFloat(), f.readFloat(), f.readFloat() };
                            break;
                        case type.FloatQuad:
                            Value = new float[4] { f.readFloat(), f.readFloat(), f.readFloat(), f.readFloat() };
                            break;
                    }
                }
                f.seek(currentPostion);
            }

            public XmlElement ToXmlElement(XmlDocument doc)
            {
                XmlElement node = doc.CreateElement(name);
                if (Children.Count == 0)
                {
                    node.SetAttribute("type", nodeType.ToString());
                    string value = "";
                    /*nintendo*/switch (nodeType)
                    {
                        case type.Int:
                            value = Value.ToString();
                            break;
                        case type.Float:
                            value = Value.ToString();
                            break;
                        case type.String:
                            value = Value.ToString();
                            break;
                        case type.Boolean:
                            value = "0";
                            if ((bool)Value)
                                value = "1";
                            break;
                        case type.FloatPair:
                            value = $"{((float[])Value)[0]} {((float[])Value)[1]}";
                            break;
                        case type.FloatTriple:
                            value = $"{((float[])Value)[0]} {((float[])Value)[1]} {((float[])Value)[2]}";
                            break;
                        case type.FloatQuad:
                            value = $"{((float[])Value)[0]} {((float[])Value)[1]} {((float[])Value)[2]} {((float[])Value)[3]}";
                            break;
                    }
                    node.InnerText = value;
                }
                else
                {
                    foreach(Node child in Children)
                    {
                        node.AppendChild(child.ToXmlElement(doc));
                    }
                }
                return node;
            }
        }

        public Node RootNode;

        public void Read(string filename)
        {
            Read(new FileData(filename));
        }

        public void Read(FileData f)
        {
            f.Endian = Endianness.Little;
            f.skip(4); //AAMP (Magic)
            uint version = f.readUInt32();
            uint unknown0x8 = f.readUInt32();
            if (version != 2)
                throw new NotImplementedException("Not a supported version of AAMP");
            uint fileSize = f.readUInt32();
            f.skip(4); //Padding 00 00 00 00
            uint xmlStringLength = f.readUInt32();
            uint unk0x18 = f.readUInt32();
            uint unk0x1C = f.readUInt32();
            uint unk0x20 = f.readUInt32();
            uint dataBufferSize = f.readUInt32();
            uint stringBufferSize = f.readUInt32();
            uint unk0x2C = f.readUInt32();
            f.skip((int)xmlStringLength);//"xml" null terminated string
            //Root Node
            RootNode = new Node();
            RootNode.nameHash = f.readUInt32();
            RootNode.name = GetName(RootNode.nameHash);
            f.skip(4);//Unknown - always 0x3?
            int dataOffset = (f.pos() - 8) + (f.readShort() * 4);
            ushort childCount = (ushort)f.readShort();
            f.seek(dataOffset);
            for(int i = 0; i < childCount; i++)
            {
                Node child = new Node();
                child.Read(f);
                RootNode.Children.Add(child);
            }
        }

        public XmlDocument ToXML()
        {
            XmlDocument xml = new XmlDocument();
            xml.AppendChild(RootNode.ToXmlElement(xml));
            return xml;
        }

        public static Dictionary<uint, string> hashName = new Dictionary<uint, string>();

        private static void GenerateHashes()
        {
            foreach (string hashStr in Resources.U_King.Split('\n')) {
                uint hash = Crc32.Compute(hashStr);
                if (!hashName.ContainsKey(hash))
                    hashName.Add(hash, hashStr);
            }
        }

        public static string GetName(uint hash)
        {
            if (hashName.Count == 0)
                GenerateHashes();
            string name = null;
            hashName.TryGetValue(hash, out name);
            if (name == null)
                return "0x" + hash.ToString("X");
            else
                return name; 
        }
    }
}
