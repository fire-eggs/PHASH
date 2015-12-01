using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

// ReSharper disable SuggestUseVarKeywordEvident

// v8test algorithm
namespace pixel
{
    class Fabien
    {
        public static string BlockString(Bitmap image, int bX, int bY)
        {
            var res = CalcBlocks(image, bX, bY);
            string res2 = "";
            foreach (var d in res)
            {
                res2 += d.ToString("F5") + "&";
            }
            return res2;
        }

        private static BitmapData LockImage(Bitmap image)
        {
            return image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);
        }

        private static int GetPixelSize(BitmapData data)
        {
            if (data == null)
                return -1;
            switch (data.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 4;
                // 2011/12/17 this isn't working: resulting in 'black' pixels for many monochrome images
                case PixelFormat.Format8bppIndexed:
                    return 1;
                default:
                    return -1;
            }
        }

        private static unsafe IEnumerable<double> CalcBlocks(Bitmap image, int bX, int bY)
        {
            BitmapData imageData = LockImage(image);
            int imgPixelSize = GetPixelSize(imageData);
            image.UnlockBits(imageData);
            if (imgPixelSize == -1)
            {
                throw new Exception("unhandled image format");
                //return null;
            }

//            byte* basePtr = (byte*) imageData.Scan0;
//            int strideVal = imageData.Stride;

            double [] result = new double[bX * bY];

            int blockWide = (image.Width-1)  / bX;
            int blockHigh = (image.Height-1) / bY;

            int blockDex = 0;
            for (int blockY = 0; blockY < bY; blockY++)
            {
                for (int blockX = 0; blockX < bX; blockX++)
                {
                    result[blockDex] += CalcBlock(image, blockX, blockY, blockHigh, blockWide);
                    blockDex ++;
                } // for blockX
            } // for blockY
            return Normalize(ref result);
        }

        // Calculate a single block
        private static unsafe double CalcBlock(Bitmap bmp, int blockX, int blockY, int blockHigh, int blockWide)
        {
            int halfW = blockWide/2;
            int halfH = blockHigh/2;
            int llcx = blockX*blockWide;
            int llcy = blockY*blockHigh;

            double blockVal = 0.0;
            for (int x = 0; x < halfW; x++)
            {
                for (int y = 0; y < halfH; y++)
                {
                    // euclidean distance from center of block
                    double distance = Math.Sqrt(Math.Pow(halfW - x, 2.0) + Math.Pow(halfH - y, 2.0));
                    double sum = 0.0;

                    sum += CalcPixel(bmp, llcx + x, llcy + y);
                    sum += CalcPixel(bmp, llcx + blockWide - x, llcy + y);
                    sum += CalcPixel(bmp, llcx + x, llcy + blockHigh - y);
                    sum += CalcPixel(bmp, llcx + blockWide - x, llcy + blockHigh - y);

                    blockVal += sum/distance;
                }
            }
            return blockVal;
        }

        // Calculate a single pixel
        private static double CalcPixel(Bitmap bmp, int px, int py)
        {
            Color pxColor = bmp.GetPixel(px, py);
            return 0.2125*pxColor.R + 0.7154*pxColor.G + 0.0721*pxColor.B;
        }

        // ReSharper disable LoopCanBeConvertedToQuery
        private static IEnumerable<double> Normalize(ref double[] orig)
        {
            int count = orig.GetLength(0);
            double total = 0.0;
            foreach (var d in orig)
            {
                total += d;
            }

            double moyenne = total/count;
            if (Equals(moyenne, 0.0))
                return orig;

            var newArr = new double[count];
            for (int i = 0; i < count; i++)
                newArr[i] = orig[i]/moyenne;

            moyenne = 1.0;
            double sommeCarresDiff = 0.0;
            foreach (var d in newArr)
            {
                sommeCarresDiff += Math.Pow(d - moyenne, 2.0);
            }
            double ecartType = Math.Sqrt(sommeCarresDiff/count);
            if (!Equals(ecartType, 0.0))
            {
                for (int i = 0; i < count; i++)
                    newArr[i] = moyenne + (newArr[i] - moyenne)/ecartType;
            }
            return newArr;
        }
        // ReSharper restore LoopCanBeConvertedToQuery
    }
}
