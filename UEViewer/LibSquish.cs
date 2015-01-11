using System;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using UELib.Engine;
using UELib.Core;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace UEViewer
{

    public static class LibSquish
    {
        [DllImport("squish.dll", EntryPoint = "DecompressImage")]
        internal static extern void DecompressImage([MarshalAs(UnmanagedType.LPArray)] byte[] rgba, uint width, uint height, [MarshalAs(UnmanagedType.LPArray)] byte[] blocks, int flags);
        [DllImport("squish.dll", EntryPoint = "GetStorageRequirements")]
        internal static extern uint GetStorageRequirements(uint width, uint height, int flags);

        [Flags]
        public enum DxtFormat : int
        {
            Dxt1 = 0x1, // Default
            Dxt3 = 0x2,
            Dxt5 = 0x4,
        }

        [Flags]
        public enum ColorCompression : int
        {
            ClusterFit = 0x008,
            RangeFit = 0x010,
            IterativeClusterFit = 0x100,
            WeightByAlpha = 0x080
        }

        [Flags]
        public enum ColorErrorMetric : int
        {
            Perceptual = 0x20,
            Uniform = 0x40
        }

        private static void RemapRGBA(ref byte[] decompImgData)
        {
            for (int i = 0; i < decompImgData.Length; i += 4)
            {
                decompImgData[i] ^= decompImgData[i + 2];
                decompImgData[i + 2] ^= decompImgData[i];
                decompImgData[i] ^= decompImgData[i + 2];
            }
        }
        public static byte[] GetRawImage(UTexture texture)
        {
            byte[] imgData = new byte[texture.MipMaps.Sum(a => a.Pixels.Length)];
            byte[] decompImgData = new byte[texture.USize * texture.VSize * 4];
            int offset = 0;
            for (int i = 0; i < texture.MipMaps.Count; i++)
            {
                System.Buffer.BlockCopy(texture.MipMaps[i].Pixels, 0, imgData, offset, texture.MipMaps[i].Pixels.Length);
                offset += texture.MipMaps[i].Pixels.Length;
            }
            switch (texture.Format)
            {
                case ETextureFormat.TEXF_G16:
                    int m_Index = 0;
                    offset = 0;
                    for (int y = 0; y < texture.USize; y++)
                    {
                        for (int x = 0; x < texture.VSize; x++)
                        {
                            int b = (int)((imgData[m_Index++] << 8) | imgData[m_Index++]) & 0xFFFF >> 8;
                            byte[] values = BitConverter.GetBytes(b | b << 8 | b << 16);
                            System.Buffer.BlockCopy(values, 0, decompImgData, offset, 3);
                            decompImgData[offset + 3] = 255;
                        }
                    }
                    break;
                case ETextureFormat.TEXF_DXT1:
                    DecompressImage(decompImgData, (uint)texture.USize, (uint)texture.VSize, imgData, (int)LibSquish.DxtFormat.Dxt1);
                    RemapRGBA(ref decompImgData);
                    break;
                case ETextureFormat.TEXF_DXT3:
                    DecompressImage(decompImgData, (uint)texture.USize, (uint)texture.VSize, imgData, (int)LibSquish.DxtFormat.Dxt3);
                    RemapRGBA(ref decompImgData);
                    break;
                case ETextureFormat.TEXF_DXT5:
                    DecompressImage(decompImgData, (uint)texture.USize, (uint)texture.VSize, imgData, (int)LibSquish.DxtFormat.Dxt5);
                    RemapRGBA(ref decompImgData);
                    break;
                default:
                    Console.WriteLine("Not implemented  format " + texture.Format);
                    break;
            }
            return decompImgData;
        }
        public static void Export(Stream fileStream, UTexture texture)
        {
            byte[] pixelsArr = GetRawImage(texture);
            byte[] header = new byte[] {
            0, // ID length
            0, // no color map
            2, // uncompressed, true color
            0, 0, 0, 0,
            0,
            0, 0, 0, 0, // x and y origin
            (byte)(texture.USize & 0x00FF),
            (byte)((texture.USize & 0xFF00) >> 8),
            (byte)(texture.VSize & 0x00FF),
            (byte)((texture.VSize & 0xFF00) >> 8),
            32, // 32 bit bitmap
            0 };

            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                writer.Write(header);
                writer.Write(pixelsArr);
            }
        }
        public static Bitmap GetImage(UTexture texture)
        {
            if (texture == null)
            {
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }
            byte[] imgData = new byte[texture.MipMaps.Sum(a => a.Pixels.Length)];
            byte[] decompImgData = new byte[texture.USize * texture.VSize * 4];
            Bitmap image = new Bitmap(texture.USize, texture.VSize, PixelFormat.Format32bppArgb);
            BitmapData bmpData;
            int offset = 0;
            for (int i = 0; i < texture.MipMaps.Count; i++)
            {
                System.Buffer.BlockCopy(texture.MipMaps[i].Pixels, 0, imgData, offset, texture.MipMaps[i].Pixels.Length);
                offset += texture.MipMaps[i].Pixels.Length;
            }
            switch (texture.Format)
            {
                case ETextureFormat.TEXF_G16:
                    image = new Bitmap(texture.USize, texture.VSize);
                    int m_Index = 0;
                    for (int y = 0; y < texture.USize; y++)
                    {
                        for (int x = 0; x < texture.VSize; x++)
                        {
                            int b = (int)((imgData[m_Index++] << 8) | imgData[m_Index++]) & 0xFFFF >> 8;
                            byte[] values = BitConverter.GetBytes(b | b << 8 | b << 16);
                            Color c = Color.FromArgb(255, (int)values[0], (int)values[1], (int)values[2]);
                            image.SetPixel(x, y, c);
                        }
                    }
                    break;
                case ETextureFormat.TEXF_DXT1:
                    DecompressImage(decompImgData, (uint)texture.USize, (uint)texture.VSize, imgData, (int)LibSquish.DxtFormat.Dxt1);
                    RemapRGBA(ref decompImgData);
                    bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
                    Marshal.Copy(decompImgData, 0, bmpData.Scan0, decompImgData.Length);
                    image.UnlockBits(bmpData);
                    break;
                case ETextureFormat.TEXF_DXT3:
                    DecompressImage(decompImgData, (uint)texture.USize, (uint)texture.VSize, imgData, (int)LibSquish.DxtFormat.Dxt3);
                    RemapRGBA(ref decompImgData);
                    bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
                    Marshal.Copy(decompImgData, 0, bmpData.Scan0, decompImgData.Length);
                    image.UnlockBits(bmpData);
                    break;
                case ETextureFormat.TEXF_DXT5:
                    DecompressImage(decompImgData, (uint)texture.USize, (uint)texture.VSize, imgData, (int)LibSquish.DxtFormat.Dxt5);
                    RemapRGBA(ref decompImgData);
                    bmpData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
                    Marshal.Copy(decompImgData, 0, bmpData.Scan0, decompImgData.Length);
                    image.UnlockBits(bmpData);
                    break;
                case ETextureFormat.TEXF_RGBA8:
                    m_Index = 0;
                    for (int y = 0; y < texture.USize; y++)
                    {
                        for (int x = 0; x < texture.VSize; x++)
                        {
                            int c = (int)(imgData[m_Index++] | (imgData[m_Index++] << 8) | (imgData[m_Index++] << 16) | (imgData[m_Index++] << 24));
                            Color col = Color.FromArgb(c);
                            image.SetPixel(x, y, col);
                        }
                    }
                    break;
                default:
                    Console.WriteLine("Not implemented  format " + texture.Format);
                    break;
            }
            return image;
        }

    }
}
