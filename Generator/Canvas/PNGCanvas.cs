//
// Swiss QR Bill Generator for .NET
// Copyright (c) 2018 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        private static readonly IImageEncoder PngEncoder = new PngEncoder();

        private readonly FontFamily _fontFamily;
        private readonly float _coordinateScale;
        private readonly Image _image;
        private readonly DrawingOptions _drawingOptions;
        private readonly PathBuilder _pathBuilder;
        private PointF _currentPosition;

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
            : base(GetFirstAvailableFontFamily(fontFamilyList).Name)
        {
            _fontFamily = GetFirstAvailableFontFamily(fontFamilyList);

            // create image
            _coordinateScale = (float)(resolution / 25.4);
            int w = (int)(width * _coordinateScale + 0.5);
            int h = (int)(height * _coordinateScale + 0.5);
            _image = new Image<Argb32>(w, h, Color.White);
            _image.Metadata.HorizontalResolution = resolution;
            _image.Metadata.VerticalResolution = resolution;

            _drawingOptions = new DrawingOptions
            {
                GraphicsOptions = { Antialias = true },
                ShapeOptions = { IntersectionRule = IntersectionRule.Nonzero },
                TextOptions = { DpiX = resolution, DpiY = resolution }
            };
            _pathBuilder = new PathBuilder();
        }

        private static FontFamily GetFirstAvailableFontFamily(string fontFamilyList)
        {
            var exceptions = new List<Exception>();
            var fontFamilies = fontFamilyList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim(' ', '"')).ToList();
            foreach (var fontFamily in fontFamilies)
            {
                try
                {
                    return SystemFonts.Get(fontFamily);
                }
                catch (FontFamilyNotFoundException exception)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }
            throw new AggregateException(exceptions);
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
            _image.Save(stream, PngEncoder);
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
            _image.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            Close();
        }

        public override void SetTransformation(double translateX, double translateY, double rotate, double scaleX, double scaleY)
        {
            // Our coordinate system extends from the bottom upwards. ImageSharp's system
            // extends from the top downwards. So Y coordinates need to be treated specially.
            translateX *= _coordinateScale;
            translateY *= _coordinateScale;

            var transform = Matrix3x2.CreateScale((float)scaleX, (float)scaleY) *
                            Matrix3x2.CreateRotation((float)-rotate) *
                            Matrix3x2.CreateTranslation((float)translateX, _image.Height - (float)translateY);
            _drawingOptions.Transform = transform;
        }

        public override void StartPath()
        {
            _pathBuilder.StartFigure();
        }

        public override void CloseSubpath()
        {
            _pathBuilder.CloseFigure();
        }

        public override void MoveTo(double x, double y)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;

            _pathBuilder.StartFigure();
            _currentPosition = new PointF((float)x, (float)y);
        }

        public override void LineTo(double x, double y)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;

            var end = new PointF((float)x, (float)y);
            _pathBuilder.AddLine(_currentPosition, end);
            _currentPosition = end;
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
            var leftTop = new PointF(left, top);
            var leftBottom = new PointF(left, bottom);
            var rightTop = new PointF(right, top);
            var rightBottom = new PointF(right, bottom);
            _pathBuilder.StartFigure();
            _pathBuilder.AddLine(leftTop, rightTop);
            _pathBuilder.AddLine(rightTop, rightBottom);
            _pathBuilder.AddLine(rightBottom, leftBottom);
            _pathBuilder.AddLine(leftBottom, leftTop);
            _pathBuilder.CloseFigure();
            _currentPosition = leftTop;
        }

        public override void CubicCurveTo(double x1, double y1, double x2, double y2, double x, double y)
        {
            x1 *= _coordinateScale;
            y1 *= -_coordinateScale;
            x2 *= _coordinateScale;
            y2 *= -_coordinateScale;
            x *= _coordinateScale;
            y *= -_coordinateScale;

            var start = new PointF((float)x1, (float)y1);
            var control = new PointF((float)x, (float)y);
            var end = new PointF((float)x2, (float)y2);
            _pathBuilder.AddBezier(startPoint: start, controlPoint: control, endPoint: end);
            _currentPosition = end;
        }

        public override void FillPath(int color)
        {
            var path = _pathBuilder.Build();
            _pathBuilder.Reset();
            var rgba = new Rgba32((uint)color) { A = byte.MaxValue };
            _image.Mutate(b => b.Fill(_drawingOptions, rgba, path));
        }

        public override void StrokePath(double strokeWidth, int color)
        {
            StrokePath(strokeWidth, color, LineStyle.Solid);
        }

        public override void StrokePath(double strokeWidth, int color, LineStyle lineStyle)
        {
            var scale = _drawingOptions.TextOptions.DpiX / 72.0f;
            var rgba = new Rgba32((uint)color) { A = byte.MaxValue };
            IPath path;
            IPen pen;
            switch (lineStyle)
            {
                case LineStyle.Dashed:
                    path = _pathBuilder.Build();
                    pen = new Pen(rgba, (float)strokeWidth * scale, new[] { 4f, 4f });
                    break;
                case LineStyle.Dotted:
                    path = _pathBuilder.Build().GenerateOutline(1f, new[] { 0.01f * scale, 3f * scale }, false, JointStyle.Square, EndCapStyle.Round);
                    pen = new Pen(rgba, (float)strokeWidth * scale);
                    break;
                default:
                    path = _pathBuilder.Build();
                    pen = new Pen(rgba, (float)strokeWidth * scale);
                    break;
            }
            _pathBuilder.Reset();
            _image.Mutate(b => b.Draw(_drawingOptions, pen, path));
        }

        public override void PutText(string text, double x, double y, int fontSize, bool isBold)
        {
            x *= _coordinateScale;
            y *= -_coordinateScale;

            var font = new Font(_fontFamily, fontSize, isBold ? FontStyle.Bold : FontStyle.Regular);
            float ascent = font.FontMetrics.Ascender / 2048.0f * fontSize * _drawingOptions.TextOptions.DpiY / 72.0f;
            var position = new Vector2((float)x, (float)y - ascent);
            _image.Mutate(b => b.DrawText(_drawingOptions, text, font, Color.Black, position));
        }
    }
}
