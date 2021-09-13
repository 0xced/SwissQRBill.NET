//
// Swiss QR Bill Generator for .NET
// Copyright (c) 2018 Manuel Bleichenbacher
// Licensed under MIT License
// https://opensource.org/licenses/MIT
//

using Codecrete.SwissQRBill.Generator;
using Codecrete.SwissQRBill.Generator.Canvas;
using System;
using System.Reflection;
using Xunit;

namespace Codecrete.SwissQRBill.GeneratorTest
{
    public class CleanupTest
    {
        [Fact]
        public void ClosePngFreesResources()
        {
            Type type = typeof(PNGCanvas);
            FieldInfo imageField = type.GetField("_image", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(imageField);

            object image;
            FieldInfo isDisposedField;
            using (PNGCanvas canvas = new PNGCanvas(QRBill.QrBillWidth, QRBill.QrBillHeight, 300, "Arial"))
            {
                image = imageField.GetValue(canvas);
                Assert.NotNull(image);
                isDisposedField = image.GetType().GetField("isDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(isDisposedField);
                Assert.False((bool)isDisposedField.GetValue(image)!);
            }
            Assert.True((bool)isDisposedField.GetValue(image)!);
        }
    }
}
