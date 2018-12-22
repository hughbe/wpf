// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Xaml.Tests
{
    public class XamlXmlWriterExceptionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var exception = new XamlXmlWriterException();
            Assert.NotEmpty(exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(0, exception.LineNumber);
            Assert.Equal(0, exception.LinePosition);
        }

        [Fact]
        public void Ctor_String()
        {
            var exception = new XamlXmlWriterException("message");
            Assert.Equal("message", exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Equal(0, exception.LineNumber);
            Assert.Equal(0, exception.LinePosition);
        }

        public static IEnumerable<object[]> Ctor_String_Exception_TestData()
        {
            yield return new object[] { "message", null, 0, 0 };
            yield return new object[] { "message", new DivideByZeroException(), 0, 0 };
            yield return new object[] { "message", new XamlException("message", null, 1, 2), 1, 2 };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Exception_TestData))]
        public void Ctor_String_Exception(string message, Exception innerException, int expectedLineNumber, int expectedLinePosition)
        {
            var exception = new XamlXmlWriterException(message, innerException);
            Assert.Contains(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Equal(expectedLineNumber, exception.LineNumber);
            Assert.Equal(expectedLinePosition, exception.LinePosition);
        }

        [Fact]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, new XamlXmlWriterException());

                stream.Seek(0, SeekOrigin.Begin);
                Assert.IsType<XamlXmlWriterException>(formatter.Deserialize(stream));
            }
        }

        [Fact]
        public void Ctor_NullSerializationInfo_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("info", () => new SubXamlXmlWriterException(null, new StreamingContext()));
        }

        [Fact]
        public void GetObjectData_NullInfo_ThrowsArgumentNullException()
        {
            var exception = new XamlXmlWriterException();
            Assert.Throws<ArgumentNullException>("info", () => exception.GetObjectData(null, new StreamingContext()));
        }

        private class SubXamlXmlWriterException : XamlXmlWriterException
        {
            public SubXamlXmlWriterException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}