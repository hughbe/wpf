// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Schema.Tests
{
    public class XamlTypeConverterTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(InstanceDescriptor), false)]
        public void CanConvertFrom_Invoke_ReturnsExpected(Type sourceType, bool expected)
        {
            var converter = new XamlTypeTypeConverter();
            Assert.Equal(expected, converter.CanConvertFrom(sourceType));
        }
        
        public static IEnumerable<object[]> ConvertFrom_TestData()
        {
            var context = new XamlSchemaContext();
            yield return new object[]
            {
                "prefix:name",
                "namespace",
                context,
                new XamlType("namespace", "name", null, context)
            };
            yield return new object[]
            {
                "prefix:name(prefix:typeName)",
                "namespace",
                context,
                new XamlType("namespace", "name", new XamlType[]
                {
                    new XamlType("namespace", "typeName", null, context)
                }, context)
            };

            yield return new object[]
            {
                "prefix:Int32",
                "http://schemas.microsoft.com/winfx/2006/xaml",
                context,
                new XamlType(typeof(int), context)
            };
        }

        [Theory]
        [MemberData(nameof(ConvertFrom_TestData))]
        public void ConvertFrom_ValidContextService_ReturnsExpected(string value, string namespaceResult, XamlSchemaContext schemaContext, XamlType expected)
        {
            var converter = new XamlTypeTypeConverter();
            var context = new CustomTypeDescriptorContext
            {
                ExpectedServiceTypes = new Type[] { typeof(IXamlNamespaceResolver), typeof(IXamlSchemaContextProvider) },
                Services = new object[]
                {
                    new CustomXamlNamespaceResolver
                    {
                        GetNamespaceResult = namespaceResult
                    },
                    new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = schemaContext
                    }
                }
            };
            XamlType actual = Assert.IsType<XamlType>(converter.ConvertFrom(context, null, value));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1:name")]
        [InlineData("prefix:1")]
        public void ConvertFrom_InvalidStringValue_ThrowsFormatException(string value)
        {
            var converter = new XamlTypeTypeConverter();
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    new CustomXamlNamespaceResolver
                    {
                        GetNamespaceResult = "namespace"
                    },
                    new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new XamlSchemaContext()
                    }
                }
            };
            Assert.Throws<FormatException>(() => converter.ConvertFrom(context, null, value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ConvertFrom_InvalidIXamlNamespaceResolverService_ThrowsNotSupportedException(object service)
        {
            var converter = new XamlTypeTypeConverter();
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[] { service }
            };
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "value"));
        }

        public static IEnumerable<object[]> ConvertFrom_InvalidIXamlSchemaContextProvider_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { 1 };
            yield return new object[] { new CustomXamlSchemaContextProvider() };
        }

        [Theory]
        [MemberData(nameof(ConvertFrom_InvalidIXamlSchemaContextProvider_TestData))]
        public void ConvertFrom_InvalidIXamlSchemaContextProvider_ThrowsNotSupportedException(object service)
        {
            var converter = new XamlTypeTypeConverter();
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    new CustomXamlNamespaceResolver
                    {
                        GetNamespaceResult = "namespace"
                    },
                    service
                }
            };
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "prefix:namespace"));
        }

        [Fact]
        public void ConvertFrom_NullContext_ThrowsNotSupportedException()
        {
            var converter = new XamlTypeTypeConverter();
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom("value"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(null)]
        public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object value)
        {
            var converter = new XamlTypeTypeConverter();
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value));
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), null, value));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(InstanceDescriptor), false)]
        public void CanConvertTo_Invoke_ReturnsExpected(Type sourceType, bool expected)
        {
            var converter = new XamlTypeTypeConverter();
            Assert.Equal(expected, converter.CanConvertTo(sourceType));
        }

        [Fact]
        public void ConvertTo_ValidContextService_ReturnsExpected()
        {
            var converter = new XamlTypeTypeConverter();
            var context = new CustomTypeDescriptorContext
            {
                ExpectedServiceTypes = new Type[] { typeof(INamespacePrefixLookup) },
                Services = new object[]
                {
                    new CustomNamespacePrefixLookup
                    {
                        ExpectedNamespaces = new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" },
                        Prefixes = new string[] { "prefix" }
                    }
                }
            };

            var type = new XamlType(typeof(int), new XamlSchemaContext());
            Assert.Equal("prefix:Int32", converter.ConvertTo(context, null, type, typeof(string)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ConvertTo_InvalidINamespacePrefixLookup_ReturnsExpected(object service)
        {
            var converter = new XamlTypeTypeConverter();
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[] { service }
            };

            var type = new XamlType(typeof(int), new XamlSchemaContext());
            Assert.Equal(type.ToString(), converter.ConvertTo(context, null, type, typeof(string)));
        }
        
        [Theory]
        [InlineData("notXamlType")]
        [InlineData(null)]
        public void ConvertTo_NotXamlType_ReturnsExpected(object value)
        {
            var converter = new XamlTypeTypeConverter();
            Assert.Equal(value ?? "", converter.ConvertTo(value, typeof(string)));
            Assert.Equal(value ?? "", converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        }
        
        [Theory]
        [InlineData(typeof(int))]
        public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
        {
            var converter = new XamlTypeTypeConverter();
            var type = new XamlType(typeof(int), new XamlSchemaContext());
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(type, destinationType));
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, type, destinationType));
        }

        [Fact]
        public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
        {
            var converter = new XamlTypeTypeConverter();
            var type = new XamlType(typeof(int), new XamlSchemaContext());
            Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(type, null));
        }
    }
}
