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
    public class EventConverterTest
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(InstanceDescriptor), false)]
        public void CanConvertFrom_Invoke_ReturnsExpected(Type sourceType, bool expected)
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Equal(expected, converter.CanConvertFrom(sourceType));
        }

        [Fact]
        public void ConvertFrom_ValidContextService_Success()
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                ExpectedServiceTypes = new Type[] { typeof(IRootObjectProvider), typeof(IDestinationTypeProvider) },
                Services = new object[]
                {
                    new CustomRootObjectProvider
                    {
                        RootObjectResult = new DelegateClass()
                    },
                    new CustomDestinationTypeProvider
                    {
                        GetDestinationTypeResult = typeof(EventHandler)
                    }
                }
            };
            EventHandler actual = Assert.IsType<EventHandler>(converter.ConvertFrom(context, null, "Method"));
            Assert.Equal(typeof(DelegateClass).GetMethod("Method"), actual.Method);
        }

        public static IEnumerable<object[]> ConvertFrom_InvalidIRootObjectProvider_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { 1 };
            yield return new object[] { new CustomDestinationTypeProvider() };
        }

        [Theory]
        [MemberData(nameof(ConvertFrom_InvalidIRootObjectProvider_TestData))]
        public void ConvertFrom_InvalidIRootObjectProvider_ThrowsNotSupportedException(object service)
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    service,
                    new CustomDestinationTypeProvider
                    {
                        GetDestinationTypeResult = typeof(EventHandler)
                    }
                }
            };
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "value"));
        }

        public static IEnumerable<object[]> ConvertFrom_InvalidIDestinationTypeProvider_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { 1 };
            yield return new object[] { new CustomDestinationTypeProvider() };
        }

        [Theory]
        [MemberData(nameof(ConvertFrom_InvalidIDestinationTypeProvider_TestData))]
        public void ConvertFrom_InvalidIDestinationTypeProvider_ThrowsNotSupportedException(object service)
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            var context = new CustomTypeDescriptorContext
            {
                Services = new object[]
                {
                    new CustomRootObjectProvider
                    {
                        RootObjectResult = new DelegateClass()
                    },
                    service
                }
            };
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "value"));
        }

        [Fact]
        public void ConvertFrom_NullContext_ThrowsNotSupportedException()
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom("value"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(null)]
        public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object value)
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
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
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Equal(expected, converter.CanConvertTo(sourceType));
        }
        
        [Theory]
        [InlineData("notType")]
        [InlineData(null)]
        public void ConvertTo_NotDelegate_ReturnsExpected(object value)
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Equal(value ?? "", converter.ConvertTo(value, typeof(string)));
            Assert.Equal(value ?? "", converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
        }
        
        [Theory]
        [InlineData(typeof(int))]
        public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(typeof(int), destinationType));
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, typeof(int), destinationType));
        }

        [Fact]
        public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
        {
            var type = new XamlType(typeof(Delegate), new XamlSchemaContext());
            TypeConverter converter = type.TypeConverter.ConverterInstance;
            Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(typeof(int), null));
        }

        private class DelegateClass
        {
            public void Method(object sender, EventArgs e) { }
        }
    }
}
