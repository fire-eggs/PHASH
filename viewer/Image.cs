/*
Copyright (c) 2011 <a href="http://www.gutgames.com">James Craig</a>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

#endregion

// ReSharper disable SuggestUseVarKeywordEvident

namespace pixel
{
    /// <summary>
    /// Utility class used for image manipulation
    /// </summary>
    public static class Image
    {
        #region Static Functions

        #region AdjustContrast

        /// <summary>
        /// Adjusts the Contrast
        /// </summary>
        /// <param name="fileName">File to change</param>
        /// <param name="newFileName">Location to save the image to</param>
        /// <param name="value">Used to set the contrast (-100 to 100)</param>
        public static void AdjustContrast(string fileName, string newFileName, float value)
        {
            if (!IsGraphic(fileName))
                return;
            ImageFormat FormatUsing = GetFormat(newFileName);
            using (Bitmap NewBitmap = AdjustContrast(fileName, value))
            {
                NewBitmap.Save(newFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Adjusts the Contrast
        /// </summary>
        /// <param name="fileName">File to change</param>
        /// <param name="Value">Used to set the contrast (-100 to 100)</param>
        /// <returns>A bitmap object</returns>
        public static Bitmap AdjustContrast(string fileName, float Value)
        {
            if (!IsGraphic(fileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(fileName))
            {
                Bitmap ReturnBitmap = Image.AdjustContrast(TempBitmap, Value);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Adjusts the Contrast
        /// </summary>
        /// <param name="OriginalImage">Image to change</param>
        /// <param name="Value">Used to set the contrast (-100 to 100)</param>
        /// <returns>A bitmap object</returns>
        public static Bitmap AdjustContrast(Bitmap OriginalImage, float Value)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            Value = (100.0f + Value) / 100.0f;
            Value *= Value;

            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color Pixel = Image.GetPixel(OldData, x, y, OldPixelSize);
                    float Red = Pixel.R / 255.0f;
                    float Green = Pixel.G / 255.0f;
                    float Blue = Pixel.B / 255.0f;
                    Red = (((Red - 0.5f) * Value) + 0.5f) * 255.0f;
                    Green = (((Green - 0.5f) * Value) + 0.5f) * 255.0f;
                    Blue = (((Blue - 0.5f) * Value) + 0.5f) * 255.0f;
                    Image.SetPixel(NewData, x, y,
                        Color.FromArgb(Clamp((int)Red, 255, 0),
                        Clamp((int)Green, 255, 0),
                        Clamp((int)Blue, 255, 0)),
                        NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region AdjustGamma

        /// <summary>
        /// Adjusts the Gamma
        /// </summary>
        /// <param name="FileName">File to change</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="Value">Used to build the gamma ramp (usually .2 to 5)</param>
        public static void AdjustGamma(string FileName, string NewFileName, float Value)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = AdjustGamma(FileName, Value))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Adjusts the Gamma
        /// </summary>
        /// <param name="FileName">File to change</param>
        /// <param name="Value">Used to build the gamma ramp (usually .2 to 5)</param>
        /// <returns>A bitmap object</returns>
        public static Bitmap AdjustGamma(string FileName, float Value)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.AdjustGamma(TempBitmap, Value);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Adjusts the Gamma
        /// </summary>
        /// <param name="OriginalImage">Image to change</param>
        /// <param name="Value">Used to build the gamma ramp (usually .2 to 5)</param>
        /// <returns>A bitmap object</returns>
        public static Bitmap AdjustGamma(Bitmap OriginalImage, float Value)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);

            int[] RedRamp = new int[256];
            int[] GreenRamp = new int[256];
            int[] BlueRamp = new int[256];
            for (int x = 0; x < 256; ++x)
            {
                RedRamp[x] = Clamp((int)((255.0 * System.Math.Pow(x / 255.0, 1.0 / Value)) + 0.5), 255, 0);
                GreenRamp[x] = Clamp((int)((255.0 * System.Math.Pow(x / 255.0, 1.0 / Value)) + 0.5), 255, 0);
                BlueRamp[x] = Clamp((int)((255.0 * System.Math.Pow(x / 255.0, 1.0 / Value)) + 0.5), 255, 0);
            }

            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color Pixel = Image.GetPixel(OldData, x, y, OldPixelSize);
                    int Red = RedRamp[Pixel.R];
                    int Green = GreenRamp[Pixel.G];
                    int Blue = BlueRamp[Pixel.B];
                    Image.SetPixel(NewData, x, y, Color.FromArgb(Red, Green, Blue), NewPixelSize);
                }
            }

            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region And

        /// <summary>
        /// ands two images
        /// </summary>
        /// <param name="FileName1">Image to manipulate</param>
        /// <param name="FileName2">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>'
        public static void And(string FileName1, string FileName2, string NewFileName)
        {
            if (!IsGraphic(FileName1) || !IsGraphic(FileName2))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.And(FileName1, FileName2))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// ands two images
        /// </summary>
        /// <param name="FileName1">Image to manipulate</param>
        /// <param name="FileName2">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap And(string FileName1, string FileName2)
        {
            if (!IsGraphic(FileName1) || !IsGraphic(FileName2))
                return new Bitmap(1, 1);
            using (Bitmap TempImage1 = new Bitmap(FileName1))
            {
                using (Bitmap TempImage2 = new Bitmap(FileName2))
                {
                    Bitmap ReturnBitmap = Image.And(TempImage1, TempImage2);
                    return ReturnBitmap;
                }
            }
        }

        /// <summary>
        /// ands two images
        /// </summary>
        /// <param name="Image1">Image to manipulate</param>
        /// <param name="Image2">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap And(Bitmap Image1, Bitmap Image2)
        {
            if (Image1 == null)
                throw new ArgumentNullException("Image1");
            if (Image2 == null)
                throw new ArgumentNullException("Image2");
            Bitmap NewBitmap = new Bitmap(Image1.Width, Image1.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData1 = Image.LockImage(Image1);
            BitmapData OldData2 = Image.LockImage(Image2);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize1 = Image.GetPixelSize(OldData1);
            int OldPixelSize2 = Image.GetPixelSize(OldData2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color Pixel1 = Image.GetPixel(OldData1, x, y, OldPixelSize1);
                    Color Pixel2 = Image.GetPixel(OldData2, x, y, OldPixelSize2);
                    Image.SetPixel(NewData, x, y,
                        Color.FromArgb(Pixel1.R & Pixel2.R,
                            Pixel1.G & Pixel2.G,
                            Pixel1.B & Pixel2.B),
                        NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(Image1, OldData1);
            Image.UnlockImage(Image2, OldData2);
            return NewBitmap;
        }

        #endregion

        #region Colorize

        /// <summary>
        /// Colorizes a black and white image
        /// </summary>
        /// <param name="FileName">File name</param>
        /// <param name="OutputFileName">Output file</param>
        /// <param name="Colors">Color array to use for the image</param>
        public static void Colorize(string FileName, string OutputFileName, Color[] Colors)
        {
            if (Colors.Length < 256)
                return;
            ImageFormat FormatUsing = GetFormat(OutputFileName);
            using (Bitmap Image = Colorize(FileName, Colors))
            {
                Image.Save(OutputFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Colorizes a black and white image
        /// </summary>
        /// <param name="FileName">File name</param>
        /// <param name="Colors">Color array to use for the image</param>
        /// <returns>The colorized image</returns>
        public static Bitmap Colorize(string FileName, Color[] Colors)
        {
            if (Colors.Length < 256)
                return new Bitmap(1, 1);
            using (Bitmap TempImage = new Bitmap(FileName))
            {
                Bitmap Image2 = Colorize(TempImage, Colors);
                return Image2;
            }
        }

        /// <summary>
        /// Colorizes a black and white image
        /// </summary>
        /// <param name="OriginalImage">Black and white image</param>
        /// <param name="Colors">Color array to use for the image</param>
        /// <returns>The colorized image</returns>
        public static Bitmap Colorize(Bitmap OriginalImage, Color[] Colors)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            if (Colors.Length < 256)
                return new Bitmap(1, 1);
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            for (int x = 0; x < OriginalImage.Width; ++x)
            {
                for (int y = 0; y < OriginalImage.Height; ++y)
                {
                    int ColorUsing = Image.GetPixel(OldData, x, y, OldPixelSize).R;
                    Image.SetPixel(NewData, x, y, Colors[ColorUsing], NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region ConvertSepiaTone

        /// <summary>
        /// Converts an image to sepia tone
        /// </summary>
        /// <param name="FileName">File to change</param>
        /// <param name="NewFileName">Location to save the image to</param>

        /// <summary>
        /// Converts an image to sepia tone
        /// </summary>
        /// <param name="FileName">File to change</param>
        /// <returns>A bitmap object of the sepia tone image</returns>


        #endregion

        #region CropImage

        /// <summary>
        /// Crops an image
        /// </summary>
        /// <param name="FileName">Name of the file to crop</param>
        /// <param name="NewFileName">The name to save the new file as</param>
        /// <param name="Width">Width of the cropped image</param>
        /// <param name="Height">Height of the cropped image</param>
        /// <param name="VAlignment">The verticle alignment of the cropping (top or bottom)</param>
        /// <param name="HAlignment">The horizontal alignment of the cropping (left or right)</param>
        public static void CropImage(string FileName, string NewFileName, int Width, int Height, Image.Align VAlignment, Image.Align HAlignment)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap CroppedBitmap = Image.CropImage(FileName, Width, Height, VAlignment, HAlignment))
            {
                CroppedBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Crops an image
        /// </summary>
        /// <param name="FileName">Name of the file to crop</param>
        /// <param name="Width">Width of the cropped image</param>
        /// <param name="Height">Height of the cropped image</param>
        /// <param name="VAlignment">The verticle alignment of the cropping (top or bottom)</param>
        /// <param name="HAlignment">The horizontal alignment of the cropping (left or right)</param>
        /// <returns>A Bitmap object of the cropped image</returns>
        public static Bitmap CropImage(string FileName, int Width, int Height, Image.Align VAlignment, Image.Align HAlignment)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnImage = Image.CropImage(TempBitmap, Width, Height, VAlignment, HAlignment);
                return ReturnImage;
            }
        }

        /// <summary>
        /// Crops an image
        /// </summary>
        /// <param name="ImageUsing">Image to crop</param>
        /// <param name="Width">Width of the cropped image</param>
        /// <param name="Height">Height of the cropped image</param>
        /// <param name="VAlignment">The verticle alignment of the cropping (top or bottom)</param>
        /// <param name="HAlignment">The horizontal alignment of the cropping (left or right)</param>
        /// <returns>A Bitmap object of the cropped image</returns>
        public static Bitmap CropImage(Bitmap ImageUsing, int Width, int Height, Image.Align VAlignment, Image.Align HAlignment)
        {
            if (ImageUsing == null)
                throw new ArgumentNullException("ImageUsing");
            Bitmap TempBitmap = ImageUsing;
            System.Drawing.Rectangle TempRectangle = new System.Drawing.Rectangle();
            TempRectangle.Height = Height;
            TempRectangle.Width = Width;
            if (VAlignment == Image.Align.Top)
            {
                TempRectangle.Y = 0;
            }
            else
            {
                TempRectangle.Y = TempBitmap.Height - Height;
                if (TempRectangle.Y < 0)
                    TempRectangle.Y = 0;
            }
            if (HAlignment == Image.Align.Left)
            {
                TempRectangle.X = 0;
            }
            else
            {
                TempRectangle.X = TempBitmap.Width - Width;
                if (TempRectangle.X < 0)
                    TempRectangle.X = 0;
            }
            Bitmap CroppedBitmap = TempBitmap.Clone(TempRectangle, TempBitmap.PixelFormat);
            return CroppedBitmap;
        }

        #endregion

        #region Dilate

        /// <summary>
        /// Does dilation
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="Size">Size of the aperture</param>
        public static void Dilate(string FileName, string NewFileName, int Size)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Dilate(FileName, Size))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Does dilation
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="Size">Size of the aperture</param>
        public static Bitmap Dilate(string FileName, int Size)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.Dilate(TempBitmap, Size);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Does dilation
        /// </summary>
        /// <param name="OriginalImage">Image to manipulate</param>
        /// <param name="Size">Size of the aperture</param>
        public static Bitmap Dilate(Bitmap OriginalImage, int Size)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            int ApetureMin = -(Size / 2);
            int ApetureMax = (Size / 2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    int RValue = 0;
                    int GValue = 0;
                    int BValue = 0;
                    for (int x2 = ApetureMin; x2 < ApetureMax; ++x2)
                    {
                        int TempX = x + x2;
                        if (TempX >= 0 && TempX < NewBitmap.Width)
                        {
                            for (int y2 = ApetureMin; y2 < ApetureMax; ++y2)
                            {
                                int TempY = y + y2;
                                if (TempY >= 0 && TempY < NewBitmap.Height)
                                {
                                    Color TempColor = Image.GetPixel(OldData, TempX, TempY, OldPixelSize);
                                    if (TempColor.R > RValue)
                                        RValue = TempColor.R;
                                    if (TempColor.G > GValue)
                                        GValue = TempColor.G;
                                    if (TempColor.B > BValue)
                                        BValue = TempColor.B;
                                }
                            }
                        }
                    }
                    Color TempPixel = Color.FromArgb(RValue, GValue, BValue);
                    Image.SetPixel(NewData, x, y, TempPixel, NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region DrawText

        /// <summary>
        /// Draws text on an image within the bounding box specified.
        /// </summary>
        /// <param name="FileName">Name of the file to load</param>
        /// <param name="NewFileName">Name of the file to save to</param>
        /// <param name="TextToDraw">The text to draw on the image</param>
        /// <param name="FontToUse">Font in which to draw the text</param>
        /// <param name="BrushUsing">Defines the brush using</param>
        /// <param name="BoxToDrawWithin">Rectangle to draw the image within</param>
        public static void DrawText(string FileName, string NewFileName, string TextToDraw,
            Font FontToUse, Brush BrushUsing, RectangleF BoxToDrawWithin)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap TempBitmap = Image.DrawText(FileName, TextToDraw, FontToUse, BrushUsing, BoxToDrawWithin))
            {
                TempBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Draws text on an image within the bounding box specified.
        /// </summary>
        /// <param name="FileName">Name of the file to load</param>
        /// <param name="TextToDraw">The text to draw on the image</param>
        /// <param name="FontToUse">Font in which to draw the text</param>
        /// <param name="BrushUsing">Defines the brush using</param>
        /// <param name="BoxToDrawWithin">Rectangle to draw the image within</param>
        /// <returns>A bitmap object with the text drawn on it</returns>
        public static Bitmap DrawText(string FileName, string TextToDraw,
            Font FontToUse, Brush BrushUsing, RectangleF BoxToDrawWithin)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.DrawText(TempBitmap, TextToDraw, FontToUse, BrushUsing, BoxToDrawWithin);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Draws text on an image within the bounding box specified.
        /// </summary>
        /// <param name="Image">Image to draw on</param>
        /// <param name="TextToDraw">The text to draw on the image</param>
        /// <param name="FontToUse">Font in which to draw the text</param>
        /// <param name="BrushUsing">Defines the brush using</param>
        /// <param name="BoxToDrawWithin">Rectangle to draw the image within</param>
        /// <returns>A bitmap object with the text drawn on it</returns>
        public static Bitmap DrawText(Bitmap Image, string TextToDraw,
            Font FontToUse, Brush BrushUsing, RectangleF BoxToDrawWithin)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            if (FontToUse == null)
                throw new ArgumentNullException("FontToUse");
            if (BrushUsing == null)
                throw new ArgumentNullException("BrushUsing");
            if (BoxToDrawWithin == null)
                throw new ArgumentNullException("BoxToDrawWithin");
            Bitmap TempBitmap = new Bitmap(Image, Image.Width, Image.Height);
            using (Graphics TempGraphics = Graphics.FromImage(TempBitmap))
            {
                TempGraphics.DrawString(TextToDraw, FontToUse, BrushUsing, BoxToDrawWithin);
            }
            return TempBitmap;
        }

        #endregion

        #region EdgeDetection

        /// <summary>
        /// Does basic edge detection on an image
        /// </summary>
        /// <param name="FileName">Image to do edge detection on</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="Threshold">Decides what is considered an edge</param>
        /// <param name="EdgeColor">Color of the edge</param>
        public static void EdgeDetection(string FileName, string NewFileName, float Threshold, Color EdgeColor)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.EdgeDetection(FileName, Threshold, EdgeColor))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Does basic edge detection on an image
        /// </summary>
        /// <param name="FileName">Image to do edge detection on</param>
        /// <param name="Threshold">Decides what is considered an edge</param>
        /// <param name="EdgeColor">Color of the edge</param>
        /// <returns>A bitmap which has the edges drawn on it</returns>
        public static Bitmap EdgeDetection(string FileName, float Threshold, Color EdgeColor)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.EdgeDetection(TempBitmap, Threshold, EdgeColor);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Does basic edge detection on an image
        /// </summary>
        /// <param name="OriginalImage">Image to do edge detection on</param>
        /// <param name="Threshold">Decides what is considered an edge</param>
        /// <param name="EdgeColor">Color of the edge</param>
        /// <returns>A bitmap which has the edges drawn on it</returns>
        public static Bitmap EdgeDetection(Bitmap OriginalImage, float Threshold, Color EdgeColor)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            if (EdgeColor == null)
                throw new ArgumentNullException("EdgeColor");
            Bitmap NewBitmap = new Bitmap(OriginalImage, OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color CurrentColor = Image.GetPixel(OldData, x, y, OldPixelSize);
                    if (y < NewBitmap.Height - 1 && x < NewBitmap.Width - 1)
                    {
                        Color TempColor = Image.GetPixel(OldData, x + 1, y + 1, OldPixelSize);
                        if (Distance(CurrentColor.R, TempColor.R, CurrentColor.G, TempColor.G, CurrentColor.B, TempColor.B) > Threshold)
                        {
                            Image.SetPixel(NewData, x, y, EdgeColor, NewPixelSize);
                        }
                    }
                    else if (y < NewBitmap.Height - 1)
                    {
                        Color TempColor = Image.GetPixel(OldData, x, y + 1, OldPixelSize);
                        if (Distance(CurrentColor.R, TempColor.R, CurrentColor.G, TempColor.G, CurrentColor.B, TempColor.B) > Threshold)
                        {
                            Image.SetPixel(NewData, x, y, EdgeColor, NewPixelSize);
                        }
                    }
                    else if (x < NewBitmap.Width - 1)
                    {
                        Color TempColor = Image.GetPixel(OldData, x + 1, y, OldPixelSize);
                        if (Distance(CurrentColor.R, TempColor.R, CurrentColor.G, TempColor.G, CurrentColor.B, TempColor.B) > Threshold)
                        {
                            Image.SetPixel(NewData, x, y, EdgeColor, NewPixelSize);
                        }
                    }
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region Flip

        /// <summary>
        /// Flips an image
        /// </summary>
        /// <param name="FileName">Image to flip</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="FlipX">Flips an image along the X axis</param>
        /// <param name="FlipY">Flips an image along the Y axis</param>
        public static void Flip(string FileName, string NewFileName, bool FlipX, bool FlipY)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Flip(FileName, FlipX, FlipY))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Flips an image
        /// </summary>
        /// <param name="FileName">Image to flip</param>
        /// <param name="FlipX">Flips an image along the X axis</param>
        /// <param name="FlipY">Flips an image along the Y axis</param>
        /// <returns>A bitmap which is flipped</returns>
        public static Bitmap Flip(string FileName, bool FlipX, bool FlipY)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.Flip(TempBitmap, FlipX, FlipY);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Flips an image
        /// </summary>
        /// <param name="Image">Image to flip</param>
        /// <param name="FlipX">Flips an image along the X axis</param>
        /// <param name="FlipY">Flips an image along the Y axis</param>
        /// <returns>A bitmap which is flipped</returns>
        public static Bitmap Flip(Bitmap Image, bool FlipX, bool FlipY)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            Bitmap NewBitmap = new Bitmap(Image, Image.Width, Image.Height);
            if (FlipX && !FlipY)
            {
                NewBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            else if (!FlipX && FlipY)
            {
                NewBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }
            else if (FlipX && FlipY)
            {
                NewBitmap.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            }
            return NewBitmap;
        }

        #endregion

        #region GetDimensions

        /// <summary>
        /// Gets the dimensions of an image
        /// </summary>
        /// <param name="FileName">File name</param>
        /// <param name="Width">Width of the image</param>
        /// <param name="Height">Height of the image</param>
        public static void GetDimensions(string FileName, out int Width, out int Height)
        {
            if (!IsGraphic(FileName))
            {
                Width = 0;
                Height = 0;
                return;
            }
            using (System.Drawing.Image TempImage = System.Drawing.Image.FromFile(FileName))
            {
                Width = TempImage.Width;
                Height = TempImage.Height;
            }
        }

        /// <summary>
        /// Gets the dimensions of an image
        /// </summary>
        /// <param name="Image">Image object</param>
        /// <param name="Width">Width of the image</param>
        /// <param name="Height">Height of the image</param>
        public static void GetDimensions(Bitmap Image, out int Width, out int Height)
        {
            if (Image == null)
            {
                Width = 0;
                Height = 0;
                return;
            }
            Width = Image.Width;
            Height = Image.Height;
        }

        #endregion

        #region GetFormat

        /// <summary>
        /// Returns the image format this file is using
        /// </summary>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static ImageFormat GetFormat(string FileName)
        {
            if (string.IsNullOrEmpty(FileName))
                throw new ArgumentNullException("FileName");
            if (FileName.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || FileName.EndsWith("jpeg", StringComparison.InvariantCultureIgnoreCase))
                return ImageFormat.Jpeg;
            if (FileName.EndsWith("png", StringComparison.InvariantCultureIgnoreCase))
                return ImageFormat.Png;
            if (FileName.EndsWith("tiff", StringComparison.InvariantCultureIgnoreCase))
                return ImageFormat.Tiff;
            if (FileName.EndsWith("ico", StringComparison.InvariantCultureIgnoreCase))
                return ImageFormat.Icon;
            if (FileName.EndsWith("gif", StringComparison.InvariantCultureIgnoreCase))
                return ImageFormat.Gif;
            return ImageFormat.Bmp;
        }

        #endregion

        #region GetHTMLPalette

        /// <summary>
        /// Gets a palette listing in HTML string format
        /// </summary>
        /// <param name="FileName">Image to get the palette of</param>
        /// <returns>A list containing HTML color values (ex: #041845)</returns>
        public static List<string> GetHTMLPalette(string FileName)
        {
            if (!IsGraphic(FileName))
                return new List<string>();
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                List<string> Palette = GetHTMLPalette(TempBitmap);
                return Palette;
            }
        }

        /// <summary>
        /// Gets a palette listing in HTML string format
        /// </summary>
        /// <param name="OriginalImage">Image to get the palette of</param>
        /// <returns>A list containing HTML color values (ex: #041845)</returns>
        public static List<string> GetHTMLPalette(Bitmap OriginalImage)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            List<string> ReturnArray = new List<string>();
            if (OriginalImage.Palette != null && OriginalImage.Palette.Entries.Length > 0)
            {
                for (int x = 0; x < OriginalImage.Palette.Entries.Length; ++x)
                {
                    string TempColor = ColorTranslator.ToHtml(OriginalImage.Palette.Entries[x]);
                    if (!ReturnArray.Contains(TempColor))
                    {
                        ReturnArray.Add(TempColor);
                    }
                }
                return ReturnArray;
            }
            BitmapData ImageData = Image.LockImage(OriginalImage);
            int PixelSize = Image.GetPixelSize(ImageData);
            for (int x = 0; x < OriginalImage.Width; ++x)
            {
                for (int y = 0; y < OriginalImage.Height; ++y)
                {
                    string TempColor = ColorTranslator.ToHtml(Image.GetPixel(ImageData, x, y, PixelSize));
                    if (!ReturnArray.Contains(TempColor))
                    {
                        ReturnArray.Add(TempColor);
                    }
                }
            }
            Image.UnlockImage(OriginalImage, ImageData);
            return ReturnArray;
        }

        #endregion

        #region IsGraphic

        /// <summary>
        /// Checks to make sure this is an image
        /// </summary>
        /// <param name="FileName">Name of the file to check</param>
        /// <returns>returns true if it is an image, false otherwise</returns>
        public static bool IsGraphic(string FileName)
        {
            System.Text.RegularExpressions.Regex Regex = new System.Text.RegularExpressions.Regex(@"\.ico$|\.tiff$|\.gif$|\.jpg$|\.jpeg$|\.png$|\.bmp$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return Regex.IsMatch(FileName);
        }

        #endregion

        #region KuwaharaBlur

        /// <summary>
        /// Does smoothing using a Kuwahara blur
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="Size">Size of the aperture</param>
        public static void KuwaharaBlur(string FileName, string NewFileName, int Size)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.KuwaharaBlur(FileName, Size))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Does smoothing using a kuwahara blur
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="Size">Size of the aperture</param>
        public static Bitmap KuwaharaBlur(string FileName, int Size)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.KuwaharaBlur(TempBitmap, Size);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Does smoothing using a kuwahara blur
        /// </summary>
        /// <param name="OriginalImage">Image to manipulate</param>
        /// <param name="Size">Size of the aperture</param>
        public static Bitmap KuwaharaBlur(Bitmap OriginalImage, int Size)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            int[] ApetureMinX = { -(Size / 2), 0, -(Size / 2), 0 };
            int[] ApetureMaxX = { 0, (Size / 2), 0, (Size / 2) };
            int[] ApetureMinY = { -(Size / 2), -(Size / 2), 0, 0 };
            int[] ApetureMaxY = { 0, 0, (Size / 2), (Size / 2) };
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    int[] RValues = { 0, 0, 0, 0 };
                    int[] GValues = { 0, 0, 0, 0 };
                    int[] BValues = { 0, 0, 0, 0 };
                    int[] NumPixels = { 0, 0, 0, 0 };
                    int[] MaxRValue = { 0, 0, 0, 0 };
                    int[] MaxGValue = { 0, 0, 0, 0 };
                    int[] MaxBValue = { 0, 0, 0, 0 };
                    int[] MinRValue = { 255, 255, 255, 255 };
                    int[] MinGValue = { 255, 255, 255, 255 };
                    int[] MinBValue = { 255, 255, 255, 255 };
                    for (int i = 0; i < 4; ++i)
                    {
                        for (int x2 = ApetureMinX[i]; x2 < ApetureMaxX[i]; ++x2)
                        {
                            int TempX = x + x2;
                            if (TempX >= 0 && TempX < NewBitmap.Width)
                            {
                                for (int y2 = ApetureMinY[i]; y2 < ApetureMaxY[i]; ++y2)
                                {
                                    int TempY = y + y2;
                                    if (TempY >= 0 && TempY < NewBitmap.Height)
                                    {
                                        Color TempColor = Image.GetPixel(OldData, TempX, TempY, OldPixelSize);
                                        RValues[i] += TempColor.R;
                                        GValues[i] += TempColor.G;
                                        BValues[i] += TempColor.B;
                                        if (TempColor.R > MaxRValue[i])
                                        {
                                            MaxRValue[i] = TempColor.R;
                                        }
                                        else if (TempColor.R < MinRValue[i])
                                        {
                                            MinRValue[i] = TempColor.R;
                                        }

                                        if (TempColor.G > MaxGValue[i])
                                        {
                                            MaxGValue[i] = TempColor.G;
                                        }
                                        else if (TempColor.G < MinGValue[i])
                                        {
                                            MinGValue[i] = TempColor.G;
                                        }

                                        if (TempColor.B > MaxBValue[i])
                                        {
                                            MaxBValue[i] = TempColor.B;
                                        }
                                        else if (TempColor.B < MinBValue[i])
                                        {
                                            MinBValue[i] = TempColor.B;
                                        }
                                        ++NumPixels[i];
                                    }
                                }
                            }
                        }
                    }
                    int j = 0;
                    int MinDifference = 10000;
                    for (int i = 0; i < 4; ++i)
                    {
                        int CurrentDifference = (MaxRValue[i] - MinRValue[i]) + (MaxGValue[i] - MinGValue[i]) + (MaxBValue[i] - MinBValue[i]);
                        if (CurrentDifference < MinDifference && NumPixels[i] > 0)
                        {
                            j = i;
                            MinDifference = CurrentDifference;
                        }
                    }

                    Color MeanPixel = Color.FromArgb(RValues[j] / NumPixels[j],
                        GValues[j] / NumPixels[j],
                        BValues[j] / NumPixels[j]);
                    Image.SetPixel(NewData, x, y, MeanPixel, NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region Negative

        /// <summary>
        /// gets the negative of the image
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        public static void Negative(string FileName, string NewFileName)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Negative(FileName))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// gets the negative of the image
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap Negative(string FileName)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.Negative(TempBitmap);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// gets the negative of the image
        /// </summary>
        /// <param name="OriginalImage">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap Negative(Bitmap OriginalImage)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color CurrentPixel = Image.GetPixel(OldData, x, y, OldPixelSize);
                    Color TempValue = Color.FromArgb(255 - CurrentPixel.R, 255 - CurrentPixel.G, 255 - CurrentPixel.B);
                    Image.SetPixel(NewData, x, y, TempValue, NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region Or

        /// <summary>
        /// Ors two images
        /// </summary>
        /// <param name="FileName1">Image to manipulate</param>
        /// <param name="FileName2">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>'
        public static void Or(string FileName1, string FileName2, string NewFileName)
        {
            if (!IsGraphic(FileName1) || !IsGraphic(FileName2))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Or(FileName1, FileName2))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Ors two images
        /// </summary>
        /// <param name="FileName1">Image to manipulate</param>
        /// <param name="FileName2">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap Or(string FileName1, string FileName2)
        {
            if (!IsGraphic(FileName1) || !IsGraphic(FileName2))
                return new Bitmap(1, 1);
            using (Bitmap TempImage1 = new Bitmap(FileName1))
            {
                using (Bitmap TempImage2 = new Bitmap(FileName2))
                {
                    Bitmap ReturnBitmap = Image.Or((Bitmap)TempImage1, (Bitmap)TempImage2);
                    return ReturnBitmap;
                }
            }
        }

        /// <summary>
        /// Ors two images
        /// </summary>
        /// <param name="Image1">Image to manipulate</param>
        /// <param name="Image2">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap Or(Bitmap Image1, Bitmap Image2)
        {
            if (Image1 == null)
                throw new ArgumentNullException("Image1");
            if (Image2 == null)
                throw new ArgumentNullException("Image2");
            Bitmap NewBitmap = new Bitmap(Image1.Width, Image1.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData1 = Image.LockImage(Image1);
            BitmapData OldData2 = Image.LockImage(Image2);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize1 = Image.GetPixelSize(OldData1);
            int OldPixelSize2 = Image.GetPixelSize(OldData2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color Pixel1 = Image.GetPixel(OldData1, x, y, OldPixelSize1);
                    Color Pixel2 = Image.GetPixel(OldData2, x, y, OldPixelSize2);
                    Image.SetPixel(NewData, x, y,
                        Color.FromArgb(Pixel1.R | Pixel2.R,
                            Pixel1.G | Pixel2.G,
                            Pixel1.B | Pixel2.B),
                        NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(Image1, OldData1);
            Image.UnlockImage(Image2, OldData2);
            return NewBitmap;
        }

        #endregion

        #region Pixelate

        /// <summary>
        /// Pixelates an image
        /// </summary>
        /// <param name="FileName">Image to pixelate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="PixelSize">Size of the "pixels" in pixels</param>
        public static void Pixelate(string FileName, string NewFileName, int PixelSize)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Pixelate(FileName, PixelSize))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Pixelates an image
        /// </summary>
        /// <param name="FileName">Image to pixelate</param>
        /// <param name="PixelSize">Size of the "pixels" in pixels</param>
        /// <returns>A bitmap which is pixelated</returns>
        public static Bitmap Pixelate(string FileName, int PixelSize)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.Pixelate(TempBitmap, PixelSize);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Pixelates an image
        /// </summary>
        /// <param name="OriginalImage">Image to pixelate</param>
        /// <param name="PixelSize">Size of the "pixels" in pixels</param>
        /// <returns>A bitmap which is pixelated</returns>
        public static Bitmap Pixelate(Bitmap OriginalImage, int PixelSize)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            for (int x = 0; x < NewBitmap.Width; x += (PixelSize / 2))
            {
                int MinX = Clamp(x - (PixelSize / 2), NewBitmap.Width, 0);
                int MaxX = Clamp(x + (PixelSize / 2), NewBitmap.Width, 0);
                for (int y = 0; y < NewBitmap.Height; y += (PixelSize / 2))
                {
                    int RValue = 0;
                    int GValue = 0;
                    int BValue = 0;
                    int MinY = Clamp(y - (PixelSize / 2), NewBitmap.Height, 0);
                    int MaxY = Clamp(y + (PixelSize / 2), NewBitmap.Height, 0);
                    for (int x2 = MinX; x2 < MaxX; ++x2)
                    {
                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            Color Pixel = Image.GetPixel(OldData, x2, y2, OldPixelSize);
                            RValue += Pixel.R;
                            GValue += Pixel.G;
                            BValue += Pixel.B;
                        }
                    }
                    RValue = RValue / (PixelSize * PixelSize);
                    GValue = GValue / (PixelSize * PixelSize);
                    BValue = BValue / (PixelSize * PixelSize);
                    Color TempPixel = Color.FromArgb(RValue, GValue, BValue);
                    for (int x2 = MinX; x2 < MaxX; ++x2)
                    {
                        for (int y2 = MinY; y2 < MaxY; ++y2)
                        {
                            Image.SetPixel(NewData, x2, y2, TempPixel, NewPixelSize);
                        }
                    }
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region ResizeImage

        /// <summary>
        /// Resizes an image to a certain height
        /// </summary>
        /// <param name="FileName">File to resize</param>
        /// <param name="NewFileName">Name to save the file to</param>
        /// <param name="MaxSide">Max height/width for the final image</param>
        public static void ResizeImage(string FileName, string NewFileName, int MaxSide)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap TempBitmap = Image.ResizeImage(FileName, MaxSide))
            {
                TempBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Resizes an image to a certain height
        /// </summary>
        /// <param name="FileName">File to resize</param>
        /// <param name="MaxSide">Max height/width for the final image</param>
        /// <returns>A bitmap object of the resized image</returns>
        public static Bitmap ResizeImage(string FileName, int MaxSide)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.ResizeImage(TempBitmap, MaxSide);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Resizes an image to a certain height
        /// </summary>
        /// <param name="Image">Image to resize</param>
        /// <param name="MaxSide">Max height/width for the final image</param>
        /// <returns>A bitmap object of the resized image</returns>
        public static Bitmap ResizeImage(Bitmap Image, int MaxSide)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            int NewWidth;
            int NewHeight;

            int OldWidth = Image.Width;
            int OldHeight = Image.Height;

            int OldMaxSide;

            if (OldWidth >= OldHeight)
            {
                OldMaxSide = OldWidth;
            }
            else
            {
                OldMaxSide = OldHeight;
            }

            double Coefficient = (double)MaxSide / (double)OldMaxSide;
            NewWidth = Convert.ToInt32(Coefficient * OldWidth);
            NewHeight = Convert.ToInt32(Coefficient * OldHeight);
            if (NewWidth <= 0)
                NewWidth = 1;
            if (NewHeight <= 0)
                NewHeight = 1;

            Bitmap TempBitmap = new Bitmap(Image, NewWidth, NewHeight);
            return TempBitmap;
        }

        /// <summary>
        /// Resizes an image to a certain height/width
        /// </summary>
        /// <param name="FileName">File to resize</param>
        /// <param name="NewFileName">Name to save the file to</param>
        /// <param name="Width">New width for the final image</param>
        /// <param name="Height">New height for the final image</param>
        /// <param name="Quality">Quality of the resizing</param>
        public static void ResizeImage(string FileName, string NewFileName, int Width, int Height, Quality Quality)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap TempBitmap = Image.ResizeImage(FileName, Width, Height, Quality))
            {
                TempBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Resizes an image to a certain height
        /// </summary>
        /// <param name="FileName">File to resize</param>
        /// <param name="Width">New width for the final image</param>
        /// <param name="Height">New height for the final image</param>
        /// <param name="Quality">Quality of the resizing</param>
        /// <returns>A bitmap object of the resized image</returns>
        public static Bitmap ResizeImage(string FileName, int Width, int Height, Quality Quality)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.ResizeImage(TempBitmap, Width, Height, Quality);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Resizes an image to a certain height
        /// </summary>
        /// <param name="Image">Image to resize</param>
        /// <param name="Width">New width for the final image</param>
        /// <param name="Height">New height for the final image</param>
        /// <param name="Quality">Quality of the resizing</param>
        /// <returns>A bitmap object of the resized image</returns>
        public static Bitmap ResizeImage(Bitmap Image, int Width, int Height, Quality Quality)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            Bitmap NewBitmap = new Bitmap(Width, Height);
            using (Graphics NewGraphics = Graphics.FromImage(NewBitmap))
            {
                if (Quality == Quality.High)
                {
                    NewGraphics.CompositingQuality = CompositingQuality.HighQuality;
                    NewGraphics.SmoothingMode = SmoothingMode.HighQuality;
                    NewGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                }
                else
                {
                    NewGraphics.CompositingQuality = CompositingQuality.HighSpeed;
                    NewGraphics.SmoothingMode = SmoothingMode.HighSpeed;
                    NewGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                }
                NewGraphics.DrawImage(Image, new System.Drawing.Rectangle(0, 0, Width, Height));
            }
            return NewBitmap;
        }

        #endregion

        #region Rotate

        /// <summary>
        /// Rotates an image
        /// </summary>
        /// <param name="FileName">Image to rotate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="DegreesToRotate">Degrees to rotate the image</param>
        public static void Rotate(string FileName, string NewFileName, float DegreesToRotate)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Rotate(FileName, DegreesToRotate))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Rotates an image
        /// </summary>
        /// <param name="FileName">Image to rotate</param>
        /// <param name="DegreesToRotate">Degrees to rotate the image</param>
        /// <returns>A bitmap object containing the rotated image</returns>
        public static Bitmap Rotate(string FileName, float DegreesToRotate)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.Rotate(TempBitmap, DegreesToRotate);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Rotates an image
        /// </summary>
        /// <param name="Image">Image to rotate</param>
        /// <param name="DegreesToRotate">Degrees to rotate the image</param>
        /// <returns>A bitmap object containing the rotated image</returns>
        public static Bitmap Rotate(Bitmap Image, float DegreesToRotate)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            Bitmap NewBitmap = new Bitmap(Image.Width, Image.Height);
            using (Graphics NewGraphics = Graphics.FromImage(NewBitmap))
            {
                NewGraphics.TranslateTransform((float)Image.Width / 2.0f, (float)Image.Height / 2.0f);
                NewGraphics.RotateTransform(DegreesToRotate);
                NewGraphics.TranslateTransform(-(float)Image.Width / 2.0f, -(float)Image.Height / 2.0f);
                NewGraphics.DrawImage(Image,
                    new System.Drawing.Rectangle(0, 0, Image.Width, Image.Height),
                    new System.Drawing.Rectangle(0, 0, Image.Width, Image.Height),
                    GraphicsUnit.Pixel);
            }
            return NewBitmap;
        }

        #endregion

        #region SinWave

        /// <summary>
        /// Does a "wave" effect on the image
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="Amplitude">Amplitude of the sine wave</param>
        /// <param name="Frequency">Frequency of the sine wave</param>
        /// <param name="XDirection">Determines if this should be done in the X direction</param>
        /// <param name="YDirection">Determines if this should be done in the Y direction</param>
        public static void SinWave(string FileName, string NewFileName, float Amplitude, float Frequency, bool XDirection, bool YDirection)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.SinWave(FileName, Amplitude, Frequency, XDirection, YDirection))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Does a "wave" effect on the image
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="Amplitude">Amplitude of the sine wave</param>
        /// <param name="Frequency">Frequency of the sine wave</param>
        /// <param name="XDirection">Determines if this should be done in the X direction</param>
        /// <param name="YDirection">Determines if this should be done in the Y direction</param>
        /// <returns>A bitmap which has been modified</returns>
        public static Bitmap SinWave(string FileName, float Amplitude, float Frequency, bool XDirection, bool YDirection)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.SinWave(TempBitmap, Amplitude, Frequency, XDirection, YDirection);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Does a "wave" effect on the image
        /// </summary>
        /// <param name="OriginalImage">Image to manipulate</param>
        /// <param name="Amplitude">Amplitude of the sine wave</param>
        /// <param name="Frequency">Frequency of the sine wave</param>
        /// <param name="XDirection">Determines if this should be done in the X direction</param>
        /// <param name="YDirection">Determines if this should be done in the Y direction</param>
        /// <returns>A bitmap which has been modified</returns>
        public static Bitmap SinWave(Bitmap OriginalImage, float Amplitude, float Frequency, bool XDirection, bool YDirection)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    double Value1 = 0;
                    double Value2 = 0;
                    if (YDirection)
                        Value1 = System.Math.Sin(((x * Frequency) * System.Math.PI) / 180.0d) * Amplitude;
                    if (XDirection)
                        Value2 = System.Math.Sin(((y * Frequency) * System.Math.PI) / 180.0d) * Amplitude;
                    Value1 = y - (int)Value1;
                    Value2 = x - (int)Value2;
                    while (Value1 < 0)
                        Value1 += NewBitmap.Height;
                    while (Value2 < 0)
                        Value2 += NewBitmap.Width;
                    while (Value1 >= NewBitmap.Height)
                        Value1 -= NewBitmap.Height;
                    while (Value2 >= NewBitmap.Width)
                        Value2 -= NewBitmap.Width;
                    Image.SetPixel(NewData, x, y,
                        Image.GetPixel(OldData, (int)Value2, (int)Value1, OldPixelSize),
                        NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region SNNBlur

        /// <summary>
        /// Does smoothing using a SNN blur
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        /// <param name="Size">Size of the aperture</param>
        public static void SNNBlur(string FileName, string NewFileName, int Size)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.SNNBlur(FileName, Size))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Does smoothing using a SNN blur
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="Size">Size of the aperture</param>
        public static Bitmap SNNBlur(string FileName, int Size)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.SNNBlur(TempBitmap, Size);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Does smoothing using a SNN blur
        /// </summary>
        /// <param name="OriginalImage">Image to manipulate</param>
        /// <param name="Size">Size of the aperture</param>
        public static Bitmap SNNBlur(Bitmap OriginalImage, int Size)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            int ApetureMinX = -(Size / 2);
            int ApetureMaxX = (Size / 2);
            int ApetureMinY = -(Size / 2);
            int ApetureMaxY = (Size / 2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    int RValue = 0;
                    int GValue = 0;
                    int BValue = 0;
                    int NumPixels = 0;
                    for (int x2 = ApetureMinX; x2 < ApetureMaxX; ++x2)
                    {
                        int TempX1 = x + x2;
                        int TempX2 = x - x2;
                        if (TempX1 >= 0 && TempX1 < NewBitmap.Width && TempX2 >= 0 && TempX2 < NewBitmap.Width)
                        {
                            for (int y2 = ApetureMinY; y2 < ApetureMaxY; ++y2)
                            {
                                int TempY1 = y + y2;
                                int TempY2 = y - y2;
                                if (TempY1 >= 0 && TempY1 < NewBitmap.Height && TempY2 >= 0 && TempY2 < NewBitmap.Height)
                                {
                                    Color TempColor = Image.GetPixel(OldData, x, y, OldPixelSize);
                                    Color TempColor2 = Image.GetPixel(OldData, TempX1, TempY1, OldPixelSize);
                                    Color TempColor3 = Image.GetPixel(OldData, TempX2, TempY2, OldPixelSize);
                                    if (Distance(TempColor.R, TempColor2.R, TempColor.G, TempColor2.G, TempColor.B, TempColor2.B) <
                                        Distance(TempColor.R, TempColor3.R, TempColor.G, TempColor3.G, TempColor.B, TempColor3.B))
                                    {
                                        RValue += TempColor2.R;
                                        GValue += TempColor2.G;
                                        BValue += TempColor2.B;
                                    }
                                    else
                                    {
                                        RValue += TempColor3.R;
                                        GValue += TempColor3.G;
                                        BValue += TempColor3.B;
                                    }
                                    ++NumPixels;
                                }
                            }
                        }
                    }
                    Color MeanPixel = Color.FromArgb(RValue / NumPixels,
                        GValue / NumPixels,
                        BValue / NumPixels);
                    Image.SetPixel(NewData, x, y, MeanPixel, NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region StretchContrast

        /// <summary>
        /// Stretches the contrast
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>
        public static void StretchContrast(string FileName, string NewFileName)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.StretchContrast(FileName))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Stretches the contrast
        /// </summary>
        /// <param name="FileName">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap StretchContrast(string FileName)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.StretchContrast(TempBitmap);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Stretches the contrast
        /// </summary>
        /// <param name="OriginalImage">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap StretchContrast(Bitmap OriginalImage)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            Color MinValue;
            Color MaxValue;
            GetMinMaxPixel(out MinValue, out MaxValue, OldData, OldPixelSize);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color CurrentPixel = Image.GetPixel(OldData, x, y, OldPixelSize);
                    Color TempValue = Color.FromArgb(Map(CurrentPixel.R, MinValue.R, MaxValue.R),
                        Map(CurrentPixel.G, MinValue.G, MaxValue.G),
                        Map(CurrentPixel.B, MinValue.B, MaxValue.B));
                    Image.SetPixel(NewData, x, y, TempValue, NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region Threshold

        /// <summary>
        /// Does threshold manipulation of the image
        /// </summary>
        /// <param name="FileName">File to change</param>
        /// <param name="Threshold">Float defining the threshold at which to set the pixel to black vs white.</param>
        /// <param name="NewFileName">Location to save the black and white image to</param>
        public static void Threshold(string FileName, string NewFileName, float Threshold)
        {
            if (!IsGraphic(FileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Threshold(FileName, Threshold))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Does threshold manipulation of the image
        /// </summary>
        /// <param name="FileName">File to change</param>
        /// <param name="Threshold">Float defining the threshold at which to set the pixel to black vs white.</param>
        /// <returns>A bitmap object containing the new image</returns>
        public static Bitmap Threshold(string FileName, float Threshold)
        {
            if (!IsGraphic(FileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                Bitmap ReturnBitmap = Image.Threshold(TempBitmap, Threshold);
                return ReturnBitmap;
            }
        }

        /// <summary>
        /// Does threshold manipulation of the image
        /// </summary>
        /// <param name="OriginalImage">Image to transform</param>
        /// <param name="Threshold">Float defining the threshold at which to set the pixel to black vs white.</param>
        /// <returns>A bitmap object containing the new image</returns>
        public static Bitmap Threshold(Bitmap OriginalImage, float Threshold)
        {
            if (OriginalImage == null)
                throw new ArgumentNullException("OriginalImage");
            Bitmap NewBitmap = new Bitmap(OriginalImage.Width, OriginalImage.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData = Image.LockImage(OriginalImage);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize = Image.GetPixelSize(OldData);
            for (int x = 0; x < OriginalImage.Width; ++x)
            {
                for (int y = 0; y < OriginalImage.Height; ++y)
                {
                    Color TempColor = Image.GetPixel(OldData, x, y, OldPixelSize);
                    if ((TempColor.R + TempColor.G + TempColor.B) / 755.0f > Threshold)
                    {
                        Image.SetPixel(NewData, x, y, Color.White, NewPixelSize);
                    }
                    else
                    {
                        Image.SetPixel(NewData, x, y, Color.Black, NewPixelSize);
                    }
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(OriginalImage, OldData);
            return NewBitmap;
        }

        #endregion

        #region Watermark

        /// <summary>
        /// Adds a watermark to an image
        /// </summary>
        /// <param name="FileName">File of the image to add the watermark to</param>
        /// <param name="WatermarkFileName">Watermark file name</param>
        /// <param name="NewFileName">Location to save the resulting image</param>
        /// <param name="Opacity">Opacity of the watermark (1.0 to 0.0 with 1 being completely visible and 0 being invisible)</param>
        /// <param name="X">X position in pixels for the watermark</param>
        /// <param name="KeyColor">Transparent color used in watermark image, set to null if not used</param>
        /// <param name="Y">Y position in pixels for the watermark</param>
        public static void Watermark(string FileName, string WatermarkFileName, string NewFileName, float Opacity, int X, int Y, Color KeyColor)
        {
            if (!IsGraphic(FileName) || !IsGraphic(WatermarkFileName))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Watermark(FileName, WatermarkFileName, Opacity, X, Y, KeyColor))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Adds a watermark to an image
        /// </summary>
        /// <param name="FileName">File of the image to add the watermark to</param>
        /// <param name="WatermarkFileName">Watermark file name</param>
        /// <param name="Opacity">Opacity of the watermark (1.0 to 0.0 with 1 being completely visible and 0 being invisible)</param>
        /// <param name="X">X position in pixels for the watermark</param>
        /// <param name="Y">Y position in pixels for the watermark</param>
        /// <param name="KeyColor">Transparent color used in watermark image, set to null if not used</param>
        /// <returns>The results in the form of a bitmap object</returns>
        public static Bitmap Watermark(string FileName, string WatermarkFileName, float Opacity, int X, int Y, Color KeyColor)
        {
            if (!IsGraphic(FileName) || !IsGraphic(WatermarkFileName))
                return new Bitmap(1, 1);
            using (Bitmap TempBitmap = new Bitmap(FileName))
            {
                using (Bitmap TempWatermarkBitmap = new Bitmap(WatermarkFileName))
                {
                    Bitmap ReturnBitmap = Image.Watermark(TempBitmap, TempWatermarkBitmap, Opacity, X, Y, KeyColor);
                    return ReturnBitmap;
                }
            }
        }

        /// <summary>
        /// Adds a watermark to an image
        /// </summary>
        /// <param name="Image">image to add the watermark to</param>
        /// <param name="WatermarkImage">Watermark image</param>
        /// <param name="Opacity">Opacity of the watermark (1.0 to 0.0 with 1 being completely visible and 0 being invisible)</param>
        /// <param name="X">X position in pixels for the watermark</param>
        /// <param name="Y">Y position in pixels for the watermark</param>
        /// <param name="KeyColor">Transparent color used in watermark image, set to null if not used</param>
        /// <returns>The results in the form of a bitmap object</returns>
        public static Bitmap Watermark(Bitmap Image, Bitmap WatermarkImage, float Opacity, int X, int Y, Color KeyColor)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            if (WatermarkImage == null)
                throw new ArgumentNullException("WatermarkImage");
            Bitmap NewBitmap = new Bitmap(Image, Image.Width, Image.Height);
            using (Graphics NewGraphics = Graphics.FromImage(NewBitmap))
            {
                float[][] FloatColorMatrix ={
                            new float[] {1, 0, 0, 0, 0},
                            new float[] {0, 1, 0, 0, 0},
                            new float[] {0, 0, 1, 0, 0},
                            new float[] {0, 0, 0, Opacity, 0},
                            new float[] {0, 0, 0, 0, 1}
                        };

                System.Drawing.Imaging.ColorMatrix NewColorMatrix = new System.Drawing.Imaging.ColorMatrix(FloatColorMatrix);
                using (ImageAttributes Attributes = new ImageAttributes())
                {
                    Attributes.SetColorMatrix(NewColorMatrix);
                    if (KeyColor != null)
                    {
                        Attributes.SetColorKey(KeyColor, KeyColor);
                    }
                    NewGraphics.DrawImage(WatermarkImage,
                        new System.Drawing.Rectangle(X, Y, WatermarkImage.Width, WatermarkImage.Height),
                        0, 0, WatermarkImage.Width, WatermarkImage.Height,
                        GraphicsUnit.Pixel,
                        Attributes);
                }
            }
            return NewBitmap;
        }

        #endregion

        #region Xor

        /// <summary>
        /// Xors two images
        /// </summary>
        /// <param name="FileName1">Image to manipulate</param>
        /// <param name="FileName2">Image to manipulate</param>
        /// <param name="NewFileName">Location to save the image to</param>'
        public static void Xor(string FileName1, string FileName2, string NewFileName)
        {
            if (!IsGraphic(FileName1) || !IsGraphic(FileName2))
                return;
            ImageFormat FormatUsing = GetFormat(NewFileName);
            using (Bitmap NewBitmap = Image.Xor(FileName1, FileName2))
            {
                NewBitmap.Save(NewFileName, FormatUsing);
            }
        }

        /// <summary>
        /// Xors two images
        /// </summary>
        /// <param name="FileName1">Image to manipulate</param>
        /// <param name="FileName2">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap Xor(string FileName1, string FileName2)
        {
            if (!IsGraphic(FileName1) || !IsGraphic(FileName2))
                return new Bitmap(1, 1);
            using (Bitmap TempImage1 = new Bitmap(FileName1))
            {
                using (Bitmap TempImage2 = new Bitmap(FileName2))
                {
                    Bitmap ReturnBitmap = Image.Xor((Bitmap)TempImage1, (Bitmap)TempImage2);
                    return ReturnBitmap;
                }
            }
        }

        /// <summary>
        /// Xors two images
        /// </summary>
        /// <param name="Image1">Image to manipulate</param>
        /// <param name="Image2">Image to manipulate</param>
        /// <returns>A bitmap image</returns>
        public static Bitmap Xor(Bitmap Image1, Bitmap Image2)
        {
            if (Image1 == null)
                throw new ArgumentNullException("Image1");
            if (Image2 == null)
                throw new ArgumentNullException("Image2");
            Bitmap NewBitmap = new Bitmap(Image1.Width, Image1.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData1 = Image.LockImage(Image1);
            BitmapData OldData2 = Image.LockImage(Image2);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize1 = Image.GetPixelSize(OldData1);
            int OldPixelSize2 = Image.GetPixelSize(OldData2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color Pixel1 = Image.GetPixel(OldData1, x, y, OldPixelSize1);
                    Color Pixel2 = Image.GetPixel(OldData2, x, y, OldPixelSize2);
                    Image.SetPixel(NewData, x, y,
                        Color.FromArgb(Pixel1.R ^ Pixel2.R,
                            Pixel1.G ^ Pixel2.G,
                            Pixel1.B ^ Pixel2.B),
                        NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(Image1, OldData1);
            Image.UnlockImage(Image2, OldData2);
            return NewBitmap;
        }

        #endregion

        #endregion

        #region Private Functions

        private static float GetHeightDifferences(int x, int y, int X1, int Y1, BitmapData BlackAndWhiteData, int BlackAndWhitePixelSize)
        {
            if (BlackAndWhiteData == null)
                throw new ArgumentNullException("BlackAndWhiteData");
            Color TempColor = Image.GetPixel(BlackAndWhiteData, x, y, BlackAndWhitePixelSize);
            float Height = GetHeight(TempColor);
            TempColor = Image.GetPixel(BlackAndWhiteData, X1, Y1, BlackAndWhitePixelSize);
            float Height2 = GetHeight(TempColor);
            return Height - Height2;
        }

        private static void SetHeight(int x, int y, float Value,BitmapData Data,int PixelSize)
        {
            if (Data == null)
                throw new ArgumentNullException("Data");
            Value *= 0.5f;
            Value += 0.5f;
            int Value2 = (int)(Value * 255.0f);
            Color TempColor = Color.FromArgb(Value2, Value2, Value2);
            Image.SetPixel(Data, x, y, TempColor, PixelSize);
        }

        private static float GetHeight(int x, int y, BitmapData BlackAndWhiteData, int BlackAndWhitePixelSize)
        {
            if (BlackAndWhiteData == null)
                throw new ArgumentNullException("BlackAndWhiteData");
            Color TempColor = Image.GetPixel(BlackAndWhiteData, x, y, BlackAndWhitePixelSize);
            return GetHeight(TempColor);
        }

        private static float GetHeight(Color Color)
        {
            if (Color == null)
                throw new ArgumentNullException("Color");
            return (float)Color.R / 255.0f;
        }

        private static double Distance(int R1, int R2, int G1, int G2, int B1, int B2)
        {
            return System.Math.Sqrt(((R1 - R2) * (R1 - R2)) + ((G1 - G2) * (G1 - G2)) + ((B1 - B2) * (B1 - B2)));
        }

        private static void GetMinMaxPixel(out Color Min, out Color Max, BitmapData ImageData, int PixelSize)
        {
            if (ImageData == null)
                throw new ArgumentNullException("ImageData");
            int MinR = 255, MinG = 255, MinB = 255;
            int MaxR = 0, MaxG = 0, MaxB = 0;
            for (int x = 0; x < ImageData.Width; ++x)
            {
                for (int y = 0; y < ImageData.Height; ++y)
                {
                    Color TempImage = Image.GetPixel(ImageData, x, y, PixelSize);
                    if (MinR > TempImage.R)
                    {
                        MinR = TempImage.R;
                    }
                    if (MaxR < TempImage.R)
                    {
                        MaxR = TempImage.R;
                    }

                    if (MinG > TempImage.G)
                    {
                        MinG = TempImage.G;
                    }
                    if (MaxG < TempImage.G)
                    {
                        MaxG = TempImage.G;
                    }

                    if (MinB > TempImage.B)
                    {
                        MinB = TempImage.B;
                    }
                    if (MaxB < TempImage.B)
                    {
                        MaxB = TempImage.B;
                    }
                }
            }
            Min = Color.FromArgb(MinR, MinG, MinB);
            Max = Color.FromArgb(MaxR, MaxG, MaxB);
        }

        private static int Map(int Value, int Min, int Max)
        {
            double TempVal = (Value - Min);
            TempVal /= (double)(Max - Min);
            return (int)(TempVal * 255);
        }

        #endregion

        #region Internal Static Functions

        public static int GetPixelSize(Bitmap image)
        {
            switch (image.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 4;
                case PixelFormat.Format8bppIndexed:
                    return 1;
                default:
                    throw new ArgumentException("unhandled image format");
            }
        }

        public static int GetPixelSize(BitmapData Data)
        {
            if (Data == null)
                throw new ArgumentNullException("Data");
            switch (Data.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 3;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 4;
                // 2011/12/17 this isn't working: resulting in 'black' pixels for many monochrome images
//                case PixelFormat.Format8bppIndexed:
//                    return 1;
                default:
                    throw new ArgumentException("unhandled image format");
            }
        }

        internal static unsafe Color GetPixel(BitmapData Data, int x, int y, int PixelSizeInBytes)
        {
            if (Data == null)
                throw new ArgumentNullException("Data");
            byte* DataPointer = (byte*)Data.Scan0;
            DataPointer = DataPointer + (y * Data.Stride) + (x * PixelSizeInBytes);
            if (PixelSizeInBytes == 1)
            {
                return Color.FromArgb(255, DataPointer[0], DataPointer[0], DataPointer[0]); // Assuming greyscale!!
            }
            if (PixelSizeInBytes == 3)
            {
                return Color.FromArgb(DataPointer[2], DataPointer[1], DataPointer[0]);
            }
            return Color.FromArgb(DataPointer[3], DataPointer[2], DataPointer[1], DataPointer[0]);
        }

        internal static unsafe void SetPixel(BitmapData Data, int x, int y, Color PixelColor, int PixelSizeInBytes)
        {
            if (Data == null)
                throw new ArgumentNullException("Data");
            if (PixelColor == null)
                throw new ArgumentNullException("PixelColor");
            byte* DataPointer = (byte*)Data.Scan0;
            DataPointer = DataPointer + (y * Data.Stride) + (x * PixelSizeInBytes);
            if (PixelSizeInBytes == 3)
            {
                DataPointer[2] = PixelColor.R;
                DataPointer[1] = PixelColor.G;
                DataPointer[0] = PixelColor.B;
                return;
            }
            DataPointer[3] = PixelColor.A;
            DataPointer[2] = PixelColor.R;
            DataPointer[1] = PixelColor.G;
            DataPointer[0] = PixelColor.B;
        }

        internal static BitmapData LockImage(Bitmap Image)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            return Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height),
                ImageLockMode.ReadWrite, Image.PixelFormat);
        }

        internal static void UnlockImage(Bitmap Image, BitmapData ImageData)
        {
            if (Image == null)
                throw new ArgumentNullException("Image");
            if (ImageData == null)
                throw new ArgumentNullException("ImageData");
            Image.UnlockBits(ImageData);
        }

        #endregion

        #region Enums

        /// <summary>
        /// Enum defining alignment
        /// </summary>
        public enum Align
        {
            Top,
            Bottom,
            Left,
            Right
        }

        /// <summary>
        /// Enum defining quality
        /// </summary>
        public enum Quality
        {
            High,
            Low
        }

        #endregion

        public static Bitmap kbrDiff(string fileName1, string fileName2, bool stretch)
        {
            if (!IsGraphic(fileName1) || !IsGraphic(fileName2))
                return new Bitmap(1, 1);

            Bitmap tempImage1 = new Bitmap(fileName1);
            Bitmap tempImage2 = new Bitmap(fileName2);

            if (GetPixelSize(tempImage1) == 1)
                tempImage1 = ConvertTo24(tempImage1);
            if (GetPixelSize(tempImage2) == 1)
                tempImage2 = ConvertTo24(tempImage2);

            Bitmap res = kbrDiff(tempImage1, tempImage2, stretch);

            tempImage1.Dispose(); // TODO really need a try..finally
            tempImage2.Dispose();
            return res;
        }

        private static Bitmap ConvertTo24(Bitmap bmpIn)
        {
            Bitmap converted = new Bitmap(bmpIn.Width, bmpIn.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(converted))
            {
                // Prevent DPI conversion
                g.PageUnit = GraphicsUnit.Pixel;
                // Draw the image
                g.DrawImageUnscaled(bmpIn, 0, 0);
            }
            return converted;
        }

        // Diff two images which are the same dimensions
        private static Bitmap kbrDiffBase(Bitmap image1, Bitmap image2)
        {
            Bitmap NewBitmap = new Bitmap(image1.Width, image1.Height);
            BitmapData NewData = Image.LockImage(NewBitmap);
            BitmapData OldData1 = Image.LockImage(image1);
            BitmapData OldData2 = Image.LockImage(image2);
            int NewPixelSize = Image.GetPixelSize(NewData);
            int OldPixelSize1 = Image.GetPixelSize(OldData1);
            int OldPixelSize2 = Image.GetPixelSize(OldData2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    Color Pixel1 = Image.GetPixel(OldData1, x, y, OldPixelSize1);
                    Color Pixel2 = Image.GetPixel(OldData2, x, y, OldPixelSize2);

                    int clrDiff = Math.Abs(Pixel1.R - Pixel2.R +
                                                  Pixel1.G - Pixel2.G +
                                                  Pixel1.B - Pixel2.B);
                    if (clrDiff < 10)
                        Image.SetPixel(NewData, x, y, Color.Black, NewPixelSize);
                    else
                        Image.SetPixel(NewData, x, y,
                            Color.FromArgb(Pixel1.R, Pixel1.G, Pixel1.B),
                            NewPixelSize);
                }
            }
            Image.UnlockImage(NewBitmap, NewData);
            Image.UnlockImage(image1, OldData1);
            Image.UnlockImage(image2, OldData2);
            image1.Dispose();
            image2.Dispose();
            return NewBitmap;
        }

        public static Bitmap kbrDiff(Bitmap image1, Bitmap image2, bool stretch)
        {
            if (image1 == null)
                throw new ArgumentNullException("image1");
            if (image2 == null)
                throw new ArgumentNullException("image2");

            if (image1.Height != image2.Height ||
                image1.Width != image2.Width)
            {
                if (!stretch)
                    throw new InvalidDataException("Size mismatch");

                // TODO RESIZE HERE

                int newH = Math.Max(image1.Height, image2.Height);
                int newW = Math.Max(image1.Width, image2.Width);
                Bitmap newImage1 = ResizeImage(image1, newW, newH);
                Bitmap newImage2 = ResizeImage(image2, newW, newH);
                return kbrDiffBase(newImage1, newImage2);
            }

            return kbrDiffBase(image1, image2);
        }

        private static int Clamp(int Value, int Max, int Min)
        {
            Value = Value > Max ? Max : Value;
            return Value < Min ? Min : Value;
        }

        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}