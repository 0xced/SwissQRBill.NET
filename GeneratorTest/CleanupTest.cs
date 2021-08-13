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
        [Theory]
        // We don't test for _typefaceRegular and _typefaceBold because they ignore the public Dispose (IgnorePublicDispose == true for SKTypeface created with `FromFamilyName`)
        [InlineData("_surface")]
        [InlineData("_path")]
        public void ClosePngFreesResources(string fieldName)
        {
            Type type = typeof(PNGCanvas);
            FieldInfo surfaceField = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(surfaceField);

            object value;
            PropertyInfo isDisposedProperty;
            using (PNGCanvas canvas = new PNGCanvas(QRBill.QrBillWidth, QRBill.QrBillHeight, 300, "Arial"))
            {
                value = surfaceField.GetValue(canvas);
                Assert.NotNull(value);
                isDisposedProperty = GetIsDisposedProperty(value);
                Assert.NotNull(isDisposedProperty);
                Assert.False((bool)isDisposedProperty.GetValue(value)!, $"{fieldName}.IsDisposed must be false before the PNGCanvas is disposed");
            }
            Assert.True((bool)isDisposedProperty.GetValue(value)!, $"{fieldName}.IsDisposed must be true after the PNGCanvas is disposed");
        }

        private static PropertyInfo GetIsDisposedProperty(object value)
        {
            for (var type = value.GetType(); type != null; type = type.BaseType)
            {
                var fieldInfo = type.GetProperty("IsDisposed", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    return fieldInfo;
                }
            }
            return null;
        }
    }
}
