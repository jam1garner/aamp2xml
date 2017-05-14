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
                Vector2 = 0x3,
                Vector3 = 0x4,

                Vector4 = 0x6,
                String = 0x7,
                String2 = 0x14,
                Actor = 0x8
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
                        case type.Actor:
                        case type.String:
                        case type.String2:
                            Value = f.readString();
                            break;
                        case type.Boolean:
                            Value = (f.readByte() != 0);
                            break;
                        case type.Vector2:
                            Value = new float[2] { f.readFloat(), f.readFloat() };
                            break;
                        case type.Vector3:
                            Value = new float[3] { f.readFloat(), f.readFloat(), f.readFloat() };
                            break;
                        case type.Vector4:
                            Value = new float[4] { f.readFloat(), f.readFloat(), f.readFloat(), f.readFloat() };
                            break;
                    }
                }
                f.seek(currentPostion);
            }

            public XmlElement ToXmlElement(XmlDocument doc)
            {
                string nodeName = name;
                if (name == null)
                    nodeName = "UnknownName";
                XmlElement node = doc.CreateElement(nodeName);
                if (name == null)
                    node.SetAttribute("hash", "0x" + nameHash.ToString("X"));
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
                        case type.Actor:
                        case type.String:
                        case type.String2:
                            value = Value.ToString();
                            break;
                        case type.Boolean:
                            value = "0";
                            if ((bool)Value)
                                value = "1";
                            break;
                        case type.Vector2:
                            value = $"{((float[])Value)[0]} {((float[])Value)[1]}";
                            break;
                        case type.Vector3:
                            value = $"{((float[])Value)[0]} {((float[])Value)[1]} {((float[])Value)[2]}";
                            break;
                        case type.Vector4:
                            value = $"{((float[])Value)[0]} {((float[])Value)[1]} {((float[])Value)[2]} {((float[])Value)[3]}";
                            break;
                        default:
                            value = "Offset 0x"+ValueOffset.ToString("X");
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

            public void fromXmlNode(XmlElement node)
            {
                if (node.Attributes == null || node.Attributes["hash"] == null)
                    nameHash = Crc32.Compute(node.Name);
                else
                    nameHash = uint.Parse(node.Attributes["hash"].Value, System.Globalization.NumberStyles.HexNumber);
                
                if(node.ChildNodes.Count > 0 && node.FirstChild.NodeType == XmlNodeType.Element)
                {
                    foreach(XmlNode child in node.ChildNodes)
                    {
                        Node newChild = new Node();
                        newChild.fromXmlNode((XmlElement)child);
                        Children.Add(newChild);
                    }
                }
                else
                {
                    nodeType = (type)Enum.Parse(typeof(type), node.GetAttribute("type"));
                    string value = node.InnerText;
                    if (nodeType != type.String && nodeType != type.String2 && nodeType != type.Actor)
                        value = value.Trim(" ".ToCharArray());
                    /*nintendo*/switch (nodeType)
                    {
                        case type.Boolean:
                            Value = (int.Parse(value)!=0);
                            break;
                        case type.Float:
                            Value = float.Parse(value);
                            break;
                        case type.Int:
                            Value = uint.Parse(value);
                            break;
                        case type.Actor:
                        case type.String:
                        case type.String2:
                            Value = value;
                            break;
                        case type.Vector2:
                            string[] vector2 = value.Split(' ');
                            Value = new float[2] { float.Parse(vector2[0]), float.Parse(vector2[1]) };
                            break;
                        case type.Vector3:
                            string[] vector3 = value.Split(' ');
                            Value = new float[3] { float.Parse(vector3[0]), float.Parse(vector3[1]), float.Parse(vector3[2]) };
                            break;
                        case type.Vector4:
                            string[] vector4 = value.Split(' ');
                            Value = new float[4] { float.Parse(vector4[0]), float.Parse(vector4[1]), float.Parse(vector4[2]), float.Parse(vector4[3]) };
                            break;
                    }
                }
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

        private int getDataSize(Node n)
        {
            if(n.Children.Count > 0)
            {
                int size = 0;
                foreach (Node child in n.Children)
                    size += getDataSize(child);
                return size;
            }
            else
            {
                /*nintendo*/switch(n.nodeType)
                {
                    case Node.type.Boolean:
                    case Node.type.Float:
                    case Node.type.Int:
                        return 4;
                    case Node.type.Vector2:
                        return 8;
                    case Node.type.Vector3:
                        return 0xC;
                    case Node.type.Vector4:
                        return 0x10;
                    case Node.type.Actor:
                    case Node.type.String:
                    case Node.type.String2:
                    default:
                        return 0;
                }
            }
        }

        private int getStringSize(Node n)
        {
            if (n.Children.Count > 0)
            {
                int size = 0;
                foreach (Node child in n.Children)
                    size += getStringSize(child);
                return size;
            }
            else
            {
                /*nintendo*/switch (n.nodeType)
                {
                    case Node.type.Actor:
                    case Node.type.String:
                    case Node.type.String2:
                        int size = ((string)n.Value).Length;
                        do
                            size++;
                        while (size % 4 != 0);
                        return size;
                    case Node.type.Boolean:
                    case Node.type.Float:
                    case Node.type.Int:
                    case Node.type.Vector2:
                    case Node.type.Vector3:
                    case Node.type.Vector4:
                    default:
                        return 0;
                }
            }
        }

        private int getChildNodeCount(Node n)
        {
            int childCount = n.Children.Count;
            foreach (Node child in n.Children)
                childCount += getChildNodeCount(child);
            return childCount;
        }

        private int getGrandChildNodeCount(Node n)
        {
            return getChildNodeCount(n) - n.Children.Count;
        }

        private void WriteChildren(Node node, FileOutput f, FileOutput dataBuffer, FileOutput stringBuffer, ref int dataBufferOffset, ref int stringBufferOffset)
        {
            //Write this node's children
            int childNodeOffset = f.pos() + (node.Children.Count*8);
            foreach (Node child in node.Children)
            {
                int offset;
                if(child.Children.Count > 0)
                {
                    offset = childNodeOffset;
                    childNodeOffset += child.Children.Count * 8;
                }
                else
                {
                    /*nintendo*/switch (child.nodeType)
                    {
                        case Node.type.Actor:
                        case Node.type.String:
                        case Node.type.String2:
                            offset = stringBufferOffset;
                            break;
                        case Node.type.Boolean:
                        case Node.type.Float:
                        case Node.type.Int:
                        case Node.type.Vector2:
                        case Node.type.Vector3:
                        case Node.type.Vector4:
                        default:
                            offset = dataBufferOffset;
                            break;
                    }
                }
                offset = (offset - f.pos()) / 4;
                f.writeInt(child.nameHash);
                f.writeShort(offset);
                f.writeByte(child.Children.Count);
                f.writeByte((byte)child.nodeType);

                if(child.Children.Count == 0)
                {
                    //write either the data or strings
                    /*nintendo*/switch (child.nodeType)
                    {
                        case Node.type.Boolean:
                            if ((bool)child.Value)
                                dataBuffer.writeInt(1);
                            else
                                dataBuffer.writeInt(0);
                            break;
                        case Node.type.Float:
                            dataBuffer.writeFloat((float)child.Value);
                            break;
                        case Node.type.Int:
                            dataBuffer.writeInt((uint)child.Value);
                            break;
                        case Node.type.Vector2:
                            dataBuffer.writeFloat(((float[])child.Value)[0]);
                            dataBuffer.writeFloat(((float[])child.Value)[1]);
                            break;
                        case Node.type.Vector3:
                            dataBuffer.writeFloat(((float[])child.Value)[0]);
                            dataBuffer.writeFloat(((float[])child.Value)[1]);
                            dataBuffer.writeFloat(((float[])child.Value)[2]);
                            break;
                        case Node.type.Vector4:
                            dataBuffer.writeFloat(((float[])child.Value)[0]);
                            dataBuffer.writeFloat(((float[])child.Value)[1]);
                            dataBuffer.writeFloat(((float[])child.Value)[2]);
                            dataBuffer.writeFloat(((float[])child.Value)[3]);
                            break;
                        case Node.type.Actor:
                        case Node.type.String:
                        case Node.type.String2:
                            stringBuffer.writeString((string)child.Value);
                            do
                                stringBuffer.writeByte(0);
                            while (stringBuffer.pos() % 4 != 0);
                            break;
                    }
                    dataBufferOffset += getDataSize(child);
                    stringBufferOffset += getStringSize(child);
                }
            }
            //Write the grandchildren (assuming they exist)
            foreach (Node child in node.Children)
                WriteChildren(child, f, dataBuffer, stringBuffer, ref dataBufferOffset, ref stringBufferOffset);
        }

        public byte[] Rebuild()
        {
            FileOutput f = new FileOutput(), dataBuffer = new FileOutput(), stringBuffer = new FileOutput();
            f.Endian = Endianness.Little;
            dataBuffer.Endian = Endianness.Little;
            f.writeString("AAMP");
            f.writeInt(2);
            f.writeInt(3);
            int dataSize = getDataSize(RootNode);
            int stringSize = getStringSize(RootNode);
            int nodeCount = getChildNodeCount(RootNode);
            int nonDirectChildCount = getGrandChildNodeCount(RootNode);
            f.writeInt(0x40 + (nodeCount * 8) + dataSize + stringSize);//Filesize, will overwrite later
            f.writeInt(0);
            f.writeInt(4);
            f.writeInt(1);
            f.writeInt(nodeCount - nonDirectChildCount);
            f.writeInt(nonDirectChildCount);
            f.writeInt(dataSize);//Data buffer size. will overwrite later
            f.writeInt(stringSize);//String buffer size. will overwrite later
            f.writeInt(0);
            f.writeString("xml");
            f.writeByte(0);
            f.writeInt(RootNode.nameHash);
            f.writeInt(3);
            f.writeShort(3);
            f.writeShort(RootNode.Children.Count);
            int dataBufferPos = 0x40 + (nodeCount * 8);
            int stringBufferPos = dataBufferPos + dataSize;
            WriteChildren(RootNode, f, dataBuffer, stringBuffer, ref dataBufferPos, ref stringBufferPos);
            f.writeBytes(dataBuffer.getBytes());
            f.writeBytes(stringBuffer.getBytes());
            return f.getBytes();
        }

        public XmlDocument ToXML()
        {
            XmlDocument xml = new XmlDocument();
            xml.AppendChild(RootNode.ToXmlElement(xml));
            return xml;
        }

        public static AAMP fromXML(XmlDocument xml)
        {
            AAMP newAAMP = new AAMP();
            Node root = new Node();
            root.fromXmlNode((XmlElement)xml.LastChild);
            newAAMP.RootNode = root;
            return newAAMP;
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
            return name; 
        }
    }
}
