using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace pixel
{
    class Pixelate
    {
        public static Bitmap PixelateImg(Bitmap originalImage, int pixelSizeX, int pixelSizeY)
        {
            Bitmap newBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            BitmapData newData = LockImage(newBitmap);
            BitmapData oldData = LockImage(originalImage);
            int newPixelSize = GetPixelSize(newData);
            int oldPixelSize = GetPixelSize(oldData);

            int PX = pixelSizeX / 2;
            int PY = pixelSizeY / 2;
            int W = originalImage.Width;
            int H = originalImage.Height;

            for (int x = PX; x <= W - PX; x += pixelSizeX)
            {
                int MinX = Clamp(x - PX, W, 0);
                int MaxX = Clamp(x + PX, W, 0);

                for (int y = PY; y <= H - PY; y += pixelSizeY)
                {
                    int RValue = 0;
                    int GValue = 0;
                    int BValue = 0;

                    int MinY = Clamp(y - PY, H, 0);
                    int MaxY = Clamp(y + PY, H, 0);
                    for (int x2 = MinX; x2 < MaxX; ++x2)
                    {
                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            Color Pixel = Image.GetPixel(oldData, x2, y2, oldPixelSize);
                            RValue += Pixel.R;
                            GValue += Pixel.G;
                            BValue += Pixel.B;
                        }
                    }
                    RValue = RValue / (pixelSizeX * pixelSizeY);
                    GValue = GValue / (pixelSizeX * pixelSizeY);
                    BValue = BValue / (pixelSizeX * pixelSizeY);

                    Color TempPixel = Color.FromArgb(RValue, GValue, BValue);
                    for (int x2 = MinX; x2 < MaxX; ++x2)
                    {
                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            Image.SetPixel(newData, x2, y2, TempPixel, newPixelSize);
                        }
                    }
                }
            }
            newBitmap.UnlockBits(newData);
            originalImage.UnlockBits(oldData);
            return newBitmap;
        }

        private static int Clamp(int value, int max, int min)
        {
            value = value > max ? max : value;
            return value < min ? min : value;
        }

        private static BitmapData LockImage(Bitmap image)
        {
            return image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);
        }

        private static int GetPixelSize(BitmapData data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            switch (data.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 4;
                // 2011/12/17 this isn't working: resulting in 'black' pixels for many monochrome images
                //case PixelFormat.Format8bppIndexed:
                //    return 1;
                default:
                    throw new ArgumentException("unhandled image format");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="szX">size of a block wide</param>
        /// <param name="szY">size of a block high</param>
        /// <param name="blockCount"></param>
        /// <returns></returns>
        private static unsafe int[] CalcBlockAverage(Bitmap image, int szX, int szY, int blockCount)
        {
            // Given an image, returns a set of "block values".
            BitmapData imageData = LockImage(image);
            int imgPixelSize = GetPixelSize(imageData);

            byte* basePtr = (byte*)imageData.Scan0;
            int strideVal = imageData.Stride;

            int PX = szX / 2;
            int PY = szY / 2;
            int W = image.Width;
            int H = image.Height;

            int [] res = new int[blockCount];
            int resDex = 0;

            if (imgPixelSize == 1)
            {
                Debug.Assert(false);
                //if (OldPixelSize == 1)
                //{
                //    // Assuming greyscale
                //    RValue += ScanPointer[0];
                //    GValue += ScanPointer[0];
                //    BValue += ScanPointer[0];
                //}
            }
            else
            {
                for (int y = PY; y <= H - PY; y += szY)      // Blocks high... starting from half-block, to height-half-block
                {
                    for (int x = PX; x <= W - PX; x += szX)  // Blocks wide ... starting from half-block to width-half-block
                    {
                        int RValue = 0;
                        int GValue = 0;
                        int BValue = 0;

                        int MinY = Clamp(y - PY, H, 0);
                        int MaxY = Clamp(y + PY, H, 0);
                        int MinX = Clamp(x - PX, W, 0);
                        int MaxX = Clamp(x + PX, W, 0);

                        byte* DataPointer = basePtr + MinY * strideVal;

                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            //DataPointer = DataPointer + (y2 * OldData.Stride) + (x2 * OldPixelSize);

                            byte* ScanPointer = DataPointer + (MinX * imgPixelSize);
                            for (int x2 = MinX; x2 < MaxX; ++x2) // all pixels in this scan line, for this block
                            {
                                RValue += ScanPointer[2];
                                GValue += ScanPointer[1];
                                BValue += ScanPointer[0];

                                ScanPointer += imgPixelSize;
                            }

                            DataPointer += strideVal; // to next scan line
                        }

                        // average (R,G,B) value for this block
                        RValue = RValue / (szX * szY);
                        GValue = GValue / (szX * szY);
                        BValue = BValue / (szX * szY);

                        int Lum = (int)(RValue * 0.3 + GValue * 0.59 + BValue * 0.11);
                        res[resDex++] = Lum;
                    }
                }
            }

            image.UnlockBits(imageData);
            return res;
        }

        private static int[] Normalise(int[] vals, int min, int max)
        {
            var ratio = (double) max/vals.Max();
            var nList = vals.Select(i => (int)(i*ratio)).ToList();
            return nList.ToArray();
        }

        public static string BlockAvgString(Bitmap image, int szX, int szY, int blockCount)
        {
            int[] blockAvgInt = CalcBlockAverage(image, szX, szY, blockCount);
            int[] normList = Normalise(blockAvgInt, 0, 255);
            return OutString(normList);
        }

        private static string OutString(int[] vals)
        {
            // TODO stringbuilder
            string res = "";
            foreach (var i in vals)
            {
                res += i.ToString("D3") + "&";
            }
            return res;
        }

        public static string BlockWeightString(Bitmap image, int szX, int szY, int blockCount)
        {
            int[] blockWeightInt = CalcBlockWeighted(image, szX, szY, blockCount);
            int[] normList = Normalise(blockWeightInt, 0, 255);
            return OutString(normList);
        }

        private static unsafe int[] CalcBlockWeighted(Bitmap image, int szX, int szY, int blockCount)
        {
            BitmapData imageData = LockImage(image);
            int imgPixelSize = GetPixelSize(imageData);

            byte* basePtr = (byte*)imageData.Scan0;
            int strideVal = imageData.Stride;
            int imgW = image.Width;
            int imgH = image.Height;

            int[] res = new int[blockCount];
            int resDex = 0;

            int PX = szX/2;
            int PY = szY/2;

            if (imgPixelSize == 1)
            {
                Debug.Assert(false);
                //if (OldPixelSize == 1)
                //{
                //    // Assuming greyscale
                //    RValue += ScanPointer[0];
                //    GValue += ScanPointer[0];
                //    BValue += ScanPointer[0];
                //}
            }
            else
            {
                for (int y = PY; y <= imgH - PY; y += szY)      // Blocks high... starting from half-block, to height-half-block
                {
                    for (int x = PX; x <= imgW - PX; x += szX)  // Blocks wide ... starting from half-block to width-half-block
                    {
                        double RValue = 0.0;
                        double GValue = 0.0;
                        double BValue = 0.0;

                        int MinY = Clamp(y - PY, imgH, 0);
                        int MaxY = Clamp(y + PY, imgH, 0);
                        int MinX = Clamp(x - PX, imgW, 0);
                        int MaxX = Clamp(x + PX, imgW, 0);

                        byte* DataPointer = basePtr + MinY * strideVal;

                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            //DataPointer = DataPointer + (y2 * OldData.Stride) + (x2 * OldPixelSize);

                            byte* ScanPointer = DataPointer + (MinX * imgPixelSize);
                            for (int x2 = MinX; x2 < MaxX; ++x2) // all pixels in this scan line, for this block
                            {
                                double dx = x2 - x;
                                double dy = y2 - y;
                                double distance = Math.Sqrt((dx*dx) + (dy*dy));
                                if (distance < 0.0001)
                                    distance = 0.9;

                                RValue += ScanPointer[2] / distance;
                                GValue += ScanPointer[1] / distance;
                                BValue += ScanPointer[0] / distance;

                                ScanPointer += imgPixelSize;
                            }

                            DataPointer += strideVal; // to next scan line
                        }

                        //RValue = RValue / (szX * szY);
                        //GValue = GValue / (szX * szY);
                        //BValue = BValue / (szX * szY);

                        int Lum = (int)(RValue * 0.3 + GValue * 0.59 + BValue * 0.11);
                        res[resDex++] = Lum;
                    }
                }
            }

            image.UnlockBits(imageData);
            return res;
        }

        //public static unsafe string BlockAverage(Bitmap OriginalImage, int PixelSizeX, int PixelSizeY)
        //{
        //    // Given an image, returns a set of "block values".

        //    string res = "";
        //    BitmapData OldData = Image.LockImage(OriginalImage);
        //    int OldPixelSize = Image.GetPixelSize(OldData);

        //    byte* basePtr = (byte*)OldData.Scan0;
        //    int strideVal = OldData.Stride;

        //    int PX = PixelSizeX / 2;
        //    int PY = PixelSizeY / 2;
        //    int W = OriginalImage.Width;
        //    int H = OriginalImage.Height;

        //    for (int y = PY; y <= H - PY; y += PixelSizeY)
        //    {
        //        for (int x = PX; x <= W - PX; x += PixelSizeX)
        //        {
        //            int RValue = 0;
        //            int GValue = 0;
        //            int BValue = 0;

        //            int MinY = Clamp(y - PY, H, 0);
        //            int MaxY = Clamp(y + PY, H, 0);
        //            int MinX = Clamp(x - PX, W, 0);
        //            int MaxX = Clamp(x + PX, W, 0);

        //            byte* DataPointer = basePtr + MinY * strideVal;

        //            for (int y2 = MinY; y2 < MaxY; ++y2)
        //            {
        //                //DataPointer = DataPointer + (y2 * OldData.Stride) + (x2 * OldPixelSize);

        //                byte* ScanPointer = DataPointer + (MinX * OldPixelSize);
        //                for (int x2 = MinX; x2 < MaxX; ++x2)
        //                {
        //                    //Color Pixel = Image.GetPixel(OldData, x2, y2, OldPixelSize);
        //                    //RValue += Pixel.R;
        //                    //GValue += Pixel.G;
        //                    //BValue += Pixel.B;

        //                    if (OldPixelSize == 1)
        //                    {
        //                        // Assuming greyscale
        //                        RValue += ScanPointer[0];
        //                        GValue += ScanPointer[0];
        //                        BValue += ScanPointer[0];
        //                    }
        //                    else
        //                    {
        //                        RValue += ScanPointer[2];
        //                        GValue += ScanPointer[1];
        //                        BValue += ScanPointer[0];
        //                    }

        //                    ScanPointer += OldPixelSize;
        //                }

        //                DataPointer += strideVal;
        //            }
        //            RValue = RValue / (PixelSizeX * PixelSizeY);
        //            GValue = GValue / (PixelSizeX * PixelSizeY);
        //            BValue = BValue / (PixelSizeX * PixelSizeY);

        //            int Lum = (int)(RValue * 0.3 + GValue * 0.59 + BValue * 0.11);
        //            res += Lum.ToString("D3") + "&";
        //            //			        Color TempPixel = Color.FromArgb(RValue, GValue, BValue);
        //            //			        Console.Write((TempPixel.ToArgb() & 0x00FFFFFF).ToString("X6") + "&");
        //        }
        //    }
        //    Image.UnlockImage(OriginalImage, OldData);
        //    return res;
        //}


        public static string BlockAvgWghtString(Bitmap image, int szX, int szY, int blockCount)
        {
            int[] blockAvgInt = CalcBlockAvgWeight(image, szX, szY, blockCount);
            int[] normList = Normalise(blockAvgInt, 0, 255);
            return OutString(normList);
        }

        private static unsafe int[] CalcBlockAvgWeight(Bitmap image, int szX, int szY, int blockCount)
        {
            // Given an image, returns a set of "block values".
            BitmapData imageData = LockImage(image);
            int imgPixelSize = GetPixelSize(imageData);

            byte* basePtr = (byte*)imageData.Scan0;
            int strideVal = imageData.Stride;

            int PX = szX / 2;
            int PY = szY / 2;
            int W = image.Width;
            int H = image.Height;

            int[] res = new int[blockCount];
            int resDex = 0;

            if (imgPixelSize == 1)
            {
                Debug.Assert(false);
                //if (OldPixelSize == 1)
                //{
                //    // Assuming greyscale
                //    RValue += ScanPointer[0];
                //    GValue += ScanPointer[0];
                //    BValue += ScanPointer[0];
                //}
            }
            else
            {
                int cntY = 0;
                for (int y = PY; y <= H - PY; y += szY)      // Blocks high... starting from half-block, to height-half-block
                {
                    int cntX = 0;
                    for (int x = PX; x <= W - PX; x += szX)  // Blocks wide ... starting from half-block to width-half-block
                    {
                        int RValue = 0;
                        int GValue = 0;
                        int BValue = 0;

                        int MinY = Clamp(y - PY, H, 0);
                        int MaxY = Clamp(y + PY, H, 0);
                        int MinX = Clamp(x - PX, W, 0);
                        int MaxX = Clamp(x + PX, W, 0);

                        byte* DataPointer = basePtr + MinY * strideVal;

                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            //DataPointer = DataPointer + (y2 * OldData.Stride) + (x2 * OldPixelSize);

                            byte* ScanPointer = DataPointer + (MinX * imgPixelSize);
                            for (int x2 = MinX; x2 < MaxX; ++x2) // all pixels in this scan line, for this block
                            {
                                RValue += ScanPointer[2];
                                GValue += ScanPointer[1];
                                BValue += ScanPointer[0];

                                ScanPointer += imgPixelSize;
                            }

                            DataPointer += strideVal; // to next scan line
                        }

                        // average (R,G,B) value for this block
                        RValue = RValue / (szX * szY);
                        GValue = GValue / (szX * szY);
                        BValue = BValue / (szX * szY);

                        int Lum = (int)(RValue * 0.3 + GValue * 0.59 + BValue * 0.11);

                        // discount border blocks
                        if (cntY == 0 || cntX == 0 || cntY == 9 || cntX == 9)
                            Lum /= 2;
                        //int dY = cntY - 5;
                        //int dX = cntX - 5;
                        //double dist = Math.Sqrt((dX*dX) + (dY*dY));
                        //if (dist < 0.0001)
                        //    dist = 0.9;

                        //res[resDex++] = (int)(Lum / dist);
                        res[resDex++] = Lum;
                        cntX++;
                    }

                    cntY++;
                }
            }

            image.UnlockBits(imageData);
            return res;
        }

    }
}
