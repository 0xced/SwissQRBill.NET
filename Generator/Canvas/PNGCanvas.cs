//
// Swiss QR Bill Generator for .NET
// Copyright (c) 2018 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using System.IO;
using SkiaSharp;

namespace Codecrete.SwissQRBill.Generator.Canvas
{
    /// <summary>
    /// Canvas for generating PNG files.
    /// </summary>
    /// <remarks>
    /// PNGs are not an optimal file format for QR bills. Vector formats such a SVG
    /// or PDF are of better quality and use far less processing power to generate
    /// </remarks>
    public class PNGCanvas : AbstractCanvas
    {
        private readonly float _coordinateScale;
        private readonly float _fontScale;
        private readonly SKImageInfo _imageInfo;
        private readonly SKSurface _surface;
        private readonly SKTypeface _typefaceRegular;
        private readonly SKTypeface _typefaceBold;
        private readonly SKPath _path;

        /// <summary>
        /// Initializes a new instance of a PNG canvas with the given size, resolution and font family.
        /// <para>
        /// The QR bill will be drawn in the bottom left corner of the image.
        /// </para>
        /// <para>
        /// It is recommended to use at least 144 dpi for a readable result.
        /// </para>
        /// </summary>
        /// <param name="width">The image width, in mm.</param>
        /// <param name="height">The image height, in mm.</param>
        /// <param name="resolution">The resolution of the image to generate (in pixels per inch).</param>
        /// <param name="fontFamilyList">A list font family names, separated by comma (same syntax as for CSS). The first font family will be used.</param>
        public PNGCanvas(double width, double height, int resolution, string fontFamilyList)
        {
            // setup font metrics
            SetupFontMetrics(fontFamilyList);
            _typefaceRegular = SKTypeface.FromFamilyName(FontMetrics.FirstFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            _typefaceBold = SKTypeface.FromFamilyName(FontMetrics.FirstFontFamily, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            // create canvas
            _coordinateScale = (float)(resolution / 25.4);
            _fontScale = (float)(resolution / 72.0);
            int w = (int)(width * _coordinateScale + 0.5);
            int h = (int)(height * _coordinateScale + 0.5);
            _imageInfo = new SKImageInfo(w, h);
            _surface = SKSurface.Create(_imageInfo);
            _surface.Canvas.DrawColor(SKColors.White);
            _path = new SKPath();
        }

        /// <summary>
        /// Gets the resulting graphics encoded as a PNG image in a byte array.
        /// <para>
        /// The canvas can no longer be used for drawing after calling this method.</para>
        /// </summary>
        /// <returns>The byte array containing the PNG image</returns>
        public override byte[] ToByteArray()
        {
            MemoryStream stream = new MemoryStream();
            WriteTo(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Writes the resulting graphics as a PNG image to the specified stream.
        /// <para>
        /// The canvas can no longer be used for drawing after calling this method.</para>
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void WriteTo(Stream stream)
        {
            var image = _surface.Snapshot();
            var pngData = image.Encode(SKEncodedImageFormat.Png, quality: 100);
            pngData.AsStream().CopyTo(stream);
            Close();
        }

        /// <summary>
        /// Writes the resulting graphics as a PNG image to the specified file path.
        /// <para>
        /// The canvas can no longer be used for drawing after calling this method.</para>
        /// </summary>
        /// <param name="path">The path (file name) to write to.</param>
        public void SaveAs(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                WriteTo(stream);
            }
        }

        protected void Close()
        {
            _path.Dispose();
            _surface.Dispose();
            _typefaceRegular.Dispose();
            _typefaceBold.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            Close();
        }

        public override void SetTransformation(double translateX, double translateY, double rotate, double scaleX, double scaleY)
        {
            // Our coordinate system extends from the bottom upwards. SkiaSharp's system
            // extends from the top downwards. So Y coordinates need to be treated specially.
            translateX *= _coordinateScale;
            translateY *= _coordinateScale;

            _surface.Canvas.ResetMatrix();
            _surface.Canvas.Translate((float)translateX, _imageInfo.Height - (float)translateY);
            _surface.Canvas.RotateRadians((float)-rotate);
            _surface.Canvas.Scale((float)scaleX, (float)scaleY);
        }

        public override void StartPath()
        {
            _path.Reset();
        }

        public override void CloseSubpath()
        {
            _path.Close();
        }

        public override void MoveTo(double x, double y)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;

            _path.MoveTo((float)x, (float)y);
        }

        public override void LineTo(double x, double y)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;

            _path.LineTo((float)x, (float)y);
        }

        public override void AddRectangle(double x, double y, double width, double height)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;
            width *= _coordinateScale;
            height *= -_coordinateScale;

            var left = (float)x;
            var right = (float)(x + width);
            var top = (float)y;
            var bottom = (float)(y + height);
            _path.AddRect(new SKRect(left: left, top: top, right: right, bottom: bottom));
        }

        public override void CubicCurveTo(double x1, double y1, double x2, double y2, double x, double y)
        {
            x1 *= _coordinateScale;
            y1 *= -_coordinateScale;
            x2 *= _coordinateScale;
            y2 *= -_coordinateScale;
            x *= _coordinateScale;
            y *= -_coordinateScale;

            _path.CubicTo((float)x1, (float)y1, (float)x, (float)y, (float)x2, (float)y2);
        }

        private static SKColor GetColor(int color)
        {
            var r = (byte)(color >> 16 & 0xFF);
            var g = (byte)(color >>  8 & 0xFF);
            var b = (byte)(color >>  0 & 0xFF);
            return new SKColor(r, g, b);
        }

        public override void FillPath(int color)
        {
            var paint = new SKPaint
            {
                Color = GetColor(color),
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
            };
            _surface.Canvas.DrawPath(_path, paint);
        }

        public override void StrokePath(double strokeWidth, int color)
        {
            StrokePath(strokeWidth, color, LineStyle.Solid);
        }

        public override void StrokePath(double strokeWidth, int color, LineStyle lineStyle)
        {
            SKPathEffect pathEffect;
            switch (lineStyle)
            {
                case LineStyle.Dashed:
                    pathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0f);
                    break;
                case LineStyle.Dotted:
                    pathEffect = SKPathEffect.CreateDash(new[] { 0.01f, 9f }, 0f);
                    break;
                default:
                    pathEffect = null;
                    break;
            }
            var paint = new SKPaint
            {
                Color = GetColor(color),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)strokeWidth * _fontScale,
                StrokeCap = lineStyle == LineStyle.Dotted ? SKStrokeCap.Round : SKStrokeCap.Butt,
                PathEffect = pathEffect,
                IsAntialias = true,
            };
            _surface.Canvas.DrawPath(_path, paint);
        }

        public override void PutText(string text, double x, double y, int fontSize, bool isBold)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;

            var font = new SKFont(isBold ? _typefaceBold : _typefaceRegular, fontSize * _fontScale);
            var paint = new SKPaint(font)
            {
                Color = SKColors.Black,
                SubpixelText = true,
            };
            font.GetFontMetrics(out var metrics);
            var ascent = metrics.Ascent / 2048.0f * fontSize * _fontScale;
            _surface.Canvas.DrawText(text, (float)x, (float)y - ascent, paint);
        }
    }
}
