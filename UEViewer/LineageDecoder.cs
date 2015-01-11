using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib.Decoding;
using UELib;
using System.IO;
namespace UELib.Decoding
{
    public class LineageDecoder : IBufferDecoder
    {
        byte m_LineageKey = 0;
        private IUnrealStream Stream;
        public void PreDecode(IUnrealStream stream)
        {
            Stream = stream;
            byte[] m_Data = new byte[22];
            m_Data = stream.UR.ReadBytes(22);
            //stream.Read(m_Data, 0, 22);
            string result = Encoding.Unicode.GetString(m_Data);
            if (result == "Lineage2Ver")
            {
                m_Data = new byte[6];
                stream.Read(m_Data, 0, 6);
                string archive_version = Encoding.Unicode.GetString(m_Data);
                switch (archive_version)
                {
                    case "111": m_LineageKey = 0xAC; break;
                    case "121":

                        string filename = Path.GetFileName(stream.Name).ToLower();
                        int ind = 0;
                        for (int i = 0; i < filename.Length; i++)
                        {
                            ind += filename[i];
                        }
                        int xb = ind & 0xFF;

                        this.m_LineageKey = (byte)(xb | xb << 8 | xb << 16 | xb << 24);

                        break;
                    default:
                        throw new System.IO.IOException(String.Format("Unsupported version {0}", archive_version));
                }
            }
        }
        public int PositionOffset
        {
            get
            {

                return 28;
            }
        }
        public byte DecodeByte(byte b)
        {
            if (m_LineageKey > 0)
            {
                return (byte)(b ^ m_LineageKey);
            }
            return b;
        }
        public void DecodeBuild(IUnrealStream stream, UnrealPackage.GameBuild build)
        {
        }
        public int DecodeRead(byte[] array, int offset, int count)
        {

            if (m_LineageKey > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = (byte)(array[i] ^ m_LineageKey);
                }
            }
            return count;
        }
    }
}
