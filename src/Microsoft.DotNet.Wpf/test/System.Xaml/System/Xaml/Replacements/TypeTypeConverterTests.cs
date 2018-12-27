// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Replacements.Tests
{
    public class TypeTypeConverterTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(InstanceDescriptor), false)]
        public void CanConvertFrom_Invoke_ReturnsExpected(Type sourceType, bool expected)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Equal(expected, converter.CanConvertFrom(sourceType));
        }

        [Fact]
        public void ConvertFrom_ValidContextService_ReturnsExpected()
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                ExpectedServiceTypes = new Type[] { typeof(IXamlTypeResolver) },
                Services = new object[]
                {
                    new CustomXamlTypeResolver
                    {
                        ExpectedQualifiedTypeName = "value",
                        ResolveResult = typeof(int)
                    }
                }
            };
            Assert.Equal(typeof(int), converter.ConvertFrom(context, null, "value"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ConvertFrom_InvalidIXamlTypeResolverService_ThrowsNotSupportedException(object service)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[] { service }
            };
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "value"));
        }

        [Fact]
        public void ConvertFrom_NullContext_ThrowsNotSupportedException()
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom("value"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(null)]
        public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object value)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
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
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Equal(expected, converter.CanConvertTo(sourceType));
        }

        [Fact]
        public void ConvertTo_ValidContextService_ReturnsExpected()
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new XamlSchemaContext()
                    },
                    new CustomNamespacePrefixLookup
                    {
                        ExpectedNamespaces = new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" },
                        Prefixes = new string[] { "prefix" }
                    }
                }
            };
            Assert.Equal("prefix:Int32", converter.ConvertTo(context, null, typeof(int), typeof(string)));
        }

        [Fact]
        public void ConvertTo_CustomGetTypeContextService_ReturnsExpected()
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new CustomXamlSchemaContext
                        {
                            GetXamlTypeResult = new XamlType(typeof(short), new XamlSchemaContext())
                        }
                    },
                    new CustomNamespacePrefixLookup
                    {
                        ExpectedNamespaces = new string[] { "http://schemas.microsoft.com/winfx/2006/xaml" },
                        Prefixes = new string[] { "prefix" }
                    }
                }
            };
            Assert.Equal("prefix:Int16", converter.ConvertTo(context, null, typeof(int), typeof(string)));
        }

        [Fact]
        public void ConvertTo_NullGetXamlTypeResult_ReturnsExpected()
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new CustomXamlSchemaContext
                        {
                            GetXamlTypeResult = null
                        }
                    }
                }
            };
            Assert.Equal(typeof(int).ToString(), converter.ConvertTo(context, null, typeof(int), typeof(string)));
        }

        public static IEnumerable<object[]> ConvertTo_InvalidIXamlSchemaContextProvider_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { 1 };
            yield return new object[] { new CustomXamlSchemaContextProvider() };
        }

        [Theory]
        [MemberData(nameof(ConvertTo_InvalidIXamlSchemaContextProvider_TestData))]
        public void ConvertTo_InvalidIXamlSchemaContextProvider_ReturnsExpected(object service)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    service
                }
            };
            Assert.Equal(typeof(int).ToString(), converter.ConvertTo(context, null, typeof(int), typeof(string)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ConvertTo_InvalidINamespacePrefixLookup_ReturnsExpected(object service)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[] { service }
            };

            var expectedType = new XamlType(typeof(int), new XamlSchemaContext());
            Assert.Equal(expectedType.ToString(), converter.ConvertTo(context, null, typeof(int), typeof(string)));
        }
        
        [Theory]
        [InlineData("notType")]
        [InlineData(null)]
        public void ConvertTo_NotType_ReturnsExpected(object value)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Equal(value ?? "", converter.ConvertTo(value, typeof(string)));
            Assert.Equal(value ?? "", converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        }
        
        [Theory]
        [InlineData(typeof(int))]
        public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(typeof(int), destinationType));
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, typeof(int), destinationType));
        }

        [Fact]
        public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
        {
            var type = new XamlType(typeof(Type), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(typeof(int), null));
        }
    }
}
