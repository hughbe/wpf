// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using System.Xaml.Tests.Common;
using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Tests
{
    public partial class XamlObjectReaderTests
    {
        public static IEnumerable<object[]> Ctor_NoMatchingMarkupExtensionConstructor_TestData()
        {
            yield return new object[] { new NonDefaultConstructibleMarkupExtension("value") };
            yield return new object[] { new NullConstructorArgumentValueMarkupExtension("value") };
            yield return new object[] { new EmptyConstructorArgumentValueMarkupExtension("value") };
            yield return new object[] { new NullGetOnlyConstructorArgumentValueMarkupExtension("value") };
            yield return new object[] { new EmptyGetOnlyConstructorArgumentValueMarkupExtension("value") };
            yield return new object[] { new NoSuchNameConstructorArgumentValueMarkupExtension("value") };
            yield return new object[] { new NoSuchTypeConstructorArgumentValueMarkupExtension("value") };
            yield return new object[] { new ProtectedValidConstructorArgumentValueMarkupExtension(1) };
            yield return new object[] { new ProtectedGetOnlyValidConstructorArgumentValueMarkupExtension(1) };
        }

        [Theory]
        [MemberData(nameof(Ctor_NoMatchingMarkupExtensionConstructor_TestData))]
        public void Ctor_NoMatchingMarkupExtensionConstructor_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
        }

        public static IEnumerable<object[]> Ctor_CriticalExceptionThrown_TestData()
        {
            yield return new object[] { new CriticalExceptionCanConvertToStringParameterExtension(new ClassWithCriticalExceptionCanConvertToStringValueSerializer()) };
            yield return new object[] { new CriticalExceptionConvertToStringParameterExtension(new ClassWithCriticalExceptionConvertToStringValueSerializer()) };
            yield return new object[] { new CriticalExceptionCanConvertToParameterExtension(new ClassWithCriticalExceptionCanConvertToTypeConverter()) };
            yield return new object[] { new CriticalExceptionConvertToParameterExtension(new ClassWithCriticalExceptionConvertToTypeConverter()) };
            yield return new object[] { new CriticalExceptionCanConvertFromParameterExtension(new ClassWithCriticalExceptionCanConvertFromTypeConverter()) };
            yield return new object[] { new CriticalExceptionCanConvertToExtension() };
        }

        [Theory]
        [MemberData(nameof(Ctor_CriticalExceptionThrown_TestData))]
        public void Ctor_CriticalExceptionThrown_ThrowsCriticalException(object instance)
        {
            Assert.Throws<NullReferenceException>(() => new XamlObjectReader(instance));
        }

        public static IEnumerable<object[]> Ctor_NonCriticalExceptionThrown_TestData()
        {
            yield return new object[] { new NonCriticalExceptionCanConvertToStringParameterExtension(new ClassWithNonCriticalExceptionCanConvertToStringValueSerializer()) };
            yield return new object[] { new NonCriticalExceptionConvertToStringParameterExtension(new ClassWithNonCriticalExceptionConvertToStringValueSerializer()) };
            yield return new object[] { new NonCriticalExceptionCanConvertToParameterExtension(new ClassWithNonCriticalExceptionCanConvertToTypeConverter()) };
            yield return new object[] { new NonCriticalExceptionConvertToParameterExtension(new ClassWithNonCriticalExceptionConvertToTypeConverter()) };
            yield return new object[] { new NonCriticalExceptionCanConvertFromParameterExtension(new ClassWithNonCriticalExceptionCanConvertFromTypeConverter()) };
            yield return new object[] { new NonCriticalExceptionCanConvertToExtension() };
        }

        [Theory]
        [MemberData(nameof(Ctor_NonCriticalExceptionThrown_TestData))]
        public void Ctor_NonCriticalExceptionThrown_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
        }

        public static IEnumerable<object[]> Ctor_InvalidMemberInfoFromInstanceDescriptor_TestData()
        {
            yield return new object[] { new FieldInfoInstanceDescriptorExtension("value") };
            yield return new object[] { new PropertyInfoInstanceDescriptorExtension("value") };
            yield return new object[] { new UnknownMemberInfoInstanceDescriptorExtension("value") };
        }

        [Theory]
        [MemberData(nameof(Ctor_InvalidMemberInfoFromInstanceDescriptor_TestData))]
        public void Ctor_InvalidMemberInfoFromInstanceDescriptor_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
        }

        [Fact]
        public void Ctor_InvalidMemberInfoFromInstanceDescriptorWithArguments_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new XamlObjectReader(new UnknownMemberInfoWithArgumentsInstanceDescriptorExtension("value")));
        }

        public static IEnumerable<object[]> Ctor_InvalidInstanceDescriptorArguments_TestData()
        {
            yield return new object[] { new NonNullableNullMethodInfoInstanceExtension(1) };
            yield return new object[] { new IncorrectParametersMethodInfoInstanceExtension(1) };
        }

        [Theory]
        [MemberData(nameof(Ctor_InvalidInstanceDescriptorArguments_TestData))]
        public void Ctor_InvalidInstanceDescriptorArguments_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
        }

        [Fact]
        public void Ctor_LookupAllMembersNull_ThrowsNullReferenceException()
        {
            // TypeReflector has a cache that includes null if the member does not
            // exist. This is cached after calling XamlType.GetMember.
            // However, ConstructorArgument attributes are allowed on properties
            // without setters as these can be set from constructors.
            // We now end up reading null values from the cache when fetching
            // get-only properties.
            var context = new CustomXamlSchemaContext()
            {
                GetXamlTypeFactory = t =>
                {
                    if (t == typeof(GetOnlyConstructorArgumentValueMarkupExtension))
                    {
                        var result = new CustomXamlType(typeof(GetOnlyConstructorArgumentValueMarkupExtension), new XamlSchemaContext())
                        {
                            LookupAllMembersResult = null
                        };
                        Assert.Null(result.GetMember(nameof(GetOnlyConstructorArgumentValueMarkupExtension.GetOnlyValue)));
                        return result;
                    }

                    return new XamlType(t, new XamlSchemaContext());
                }
            };
            Assert.Throws<NullReferenceException>(() => new XamlObjectReader(new GetOnlyConstructorArgumentValueMarkupExtension("value", 1), context));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_CustomMarkupExtensionPublic_Success(bool allowProtectedMembersOnRoot)
        {
            // If there is a matching constructor for all properties, then they
            // are passed as x:_PositionalParameters converted to string.
            var reader = new XamlObjectReader(new PublicValidConstructorArgumentValueMarkupExtension("value"), new XamlObjectReaderSettings
            {
                AllowProtectedMembersOnRoot = allowProtectedMembersOnRoot
            });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(PublicValidConstructorArgumentValueMarkupExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_CustomMarkupExtensionPublicWithGetOnlyMembers_Success(bool allowProtectedMembersOnRoot)
        {
            // If there is a matching constructor for all properties, including
            // get-only members, then they are passed as x:_PositionalParameters
            // converted to string.
            var reader = new XamlObjectReader(new GetOnlyConstructorArgumentValueMarkupExtension("value", 1), new XamlObjectReaderSettings
            {
                AllowProtectedMembersOnRoot = allowProtectedMembersOnRoot
            });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(GetOnlyConstructorArgumentValueMarkupExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionValueSerializerAndConverter_ReturnsExpected()
        {
            // If there is a matching constructor for a property and the
            // property has a ValueSerializer and TypeConverter, then the
            // value is converted to string using the ValueSerializer.
            var reader = new XamlObjectReader(new CustomValueSerializerAndTypeConverterConstructorArgumentValueMarkupExtension(new ClassWithValueSerializerAndTypeConverter()));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(CustomValueSerializerAndTypeConverterConstructorArgumentValueMarkupExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "ValueSerializer"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void ReadCustomMarkupExtensionCantConvertToStringValueSerializer_ReturnsExpected()
        {
            // If there is a matching constructor for a property and the
            // property has a ValueSerializer that can't convert to string
            // and TypeConverter that can roundtrip to/from string, then the
            // value is converted to string using the TypeConverter.
            var reader = new XamlObjectReader(new CantConvertToStringParameterExtension(new ClassWithCantConvertToStringValueSerializer()));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(CantConvertToStringParameterExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "TypeConverter"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void ReadCustomMarkupExtensionCantConvertFromTypeConverter_ReturnsExpected()
        {
            // If there is a matching constructor for a property and the
            // property has a ValueSerializer that can roundtrip to/from string
            // and TypeConverter that can't convert from string, then the
            // arguments are passed as x:Arguments.
            var reader = new XamlObjectReader(new CantConvertFromParameterExtension(new ClassWithCantConvertFromTypeConverter()));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(CantConvertFromParameterExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithCantConvertFromTypeConverter), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionNoConverter_ReturnsExpected()
        {
            // If there is a matching constructor for a property and the
            // property is a MarkupExtension that can't be represented
            // in xaml, then the arguments are passed as x:Arguments.
            var reader = new XamlObjectReader(new NoParameterExtension(new ClassWithNoTypeConverter()));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NoParameterExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithNoTypeConverter), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionWithMarkupExtensionParameter_Success()
        {
            // If there is a matching constructor for a property and the
            // property does not have a TypeConverter, then the arguments
            // are passed as x:Arguments.
            var reader = new XamlObjectReader(new ClassWithSameMarkupExtensionParameter(new ClassWithSameMarkupExtensionParameter(1)));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithSameMarkupExtensionParameter), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithSameMarkupExtensionParameter), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionWithMarkupExtensionTypeConverter_Success()
        {
            // If there is a matching constructor for a property and the
            // property has a TypeConverter that can convert to MarkupExtension,
            // then the arguments are passed as a MarkupExtension object.
            var reader = new XamlObjectReader(new MarkupExtensionParameterExtension(new ClassWithMarkupExtensionTypeConverter()));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(MarkupExtensionParameterExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Static),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "member"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_StringWithoutConverterOrValueSerializer_ReturnsExpected()
        {
            // If a string parameter does not have a value serializer or
            // type converter, then its raw value is used.
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = (t) =>
                {
                    if (t == typeof(string))
                    {
                        return new CustomXamlType(t, new XamlSchemaContext())
                        {
                            LookupTypeConverterResult = null,
                            LookupValueSerializerResult = null
                        };
                    }

                    return new XamlType(t, new XamlSchemaContext());
                }
            };
            var reader = new XamlObjectReader(new PublicValidConstructorArgumentValueMarkupExtension("value"), context);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(PublicValidConstructorArgumentValueMarkupExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionWithInstanceDescriptor_Success()
        {
            // If all arguments of an instance descriptor can be represented
            // in xaml, then the arguments are passed as x:_PositionalParameters.
            var reader = new XamlObjectReader(new ValidInstanceDescriptorExtension("value"));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ValidInstanceDescriptorExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "hello"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionEmptyInstanceDescriptorArguments_Success()
        {
            // If there are no arguments of an instance descriptor, then the
            // default constructor is used. 
            var reader = new XamlObjectReader(new EmptyInstanceDescriptorExtension());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(EmptyInstanceDescriptorExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionArgumentCantConvertInstanceDescriptorDefaultConstructor_UsesDefaultConstructor()
        {
            // If an argument of an instance descriptor cannot be represented
            // in xaml and the type has a default constructor, then the
            // default constructor is used.
            var reader = new XamlObjectReader(new ArgumentCantConvertInstanceDescriptorDefaultConstructorExtension(null));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ArgumentCantConvertInstanceDescriptorDefaultConstructorExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionWithArgumentCantConvertInstanceDescriptorNoDefaultConstructor_Success()
        {
            // If an argument of an instance descriptor cannot be represented
            // in xaml and the type does not have a default constructor, then
            // the arguments are passed as x:arguments members.
            var reader = new XamlObjectReader(new ArgumentCantConvertInstanceDescriptorNoDefaultConstructorExtension(null));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ArgumentCantConvertInstanceDescriptorNoDefaultConstructorExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithCantConvertFromTypeConverter), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionWithDeclaringTypeMethodInfoInstance_Success()
        {
            var reader = new XamlObjectReader(new DeclaringTypeMethodInfoInstanceExtension("hello"));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(DeclaringTypeMethodInfoInstanceExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.FactoryMethod),
                new XamlNode(XamlNodeType.Value, "Factory"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionArgumentWithNullableNullParameterMethodInfoInstance_Success()
        {
            var reader = new XamlObjectReader(new NullableNullMethodInfoInstanceExtension(2));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NullableNullMethodInfoInstanceExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, ""),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.FactoryMethod),
                new XamlNode(XamlNodeType.Value, "Factory"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionArgumentWithNullableNonNullParameterMethodInfoInstance_Success()
        {
            var reader = new XamlObjectReader(new NullableNonNullMethodInfoInstanceExtension(2));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NullableNonNullMethodInfoInstanceExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.FactoryMethod),
                new XamlNode(XamlNodeType.Value, "Factory"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionWithNonDeclaringTypeMethodInfoInstance_Success()
        {
            var reader = new XamlObjectReader(new NonDeclaringTypeMethodInfoInstanceExtension("hello"));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NonDeclaringTypeMethodInfoInstanceExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.FactoryMethod),
                new XamlNode(XamlNodeType.Value, "FactoryClass.Factory"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionProtected_Success()
        {
            var reader = new XamlObjectReader(new ProtectedValidConstructorArgumentValueMarkupExtension(2), new XamlObjectReaderSettings
            {
                AllowProtectedMembersOnRoot = true
            });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ProtectedValidConstructorArgumentValueMarkupExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_CustomMarkupExtensionProtectedGetOnly_Success()
        {
            var reader = new XamlObjectReader(new ProtectedGetOnlyValidConstructorArgumentValueMarkupExtension(2), new XamlObjectReaderSettings
            {
                AllowProtectedMembersOnRoot = true
            });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ProtectedGetOnlyValidConstructorArgumentValueMarkupExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }
    }

    public class NonDefaultConstructibleMarkupExtension : MarkupExtensionBase
    {
        public NonDefaultConstructibleMarkupExtension(string value)
        {
        }
    }

    public class NullConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public NullConstructorArgumentValueMarkupExtension(string value) { }

        [ConstructorArgument(null)]
        public string Value { get; set; }
    }

    public class EmptyConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public EmptyConstructorArgumentValueMarkupExtension(string value) { }

        [ConstructorArgument("")]
        public string Value { get; set; }
    }

    public class NullGetOnlyConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public NullGetOnlyConstructorArgumentValueMarkupExtension(string value) { }

        [ConstructorArgument(null)]
        public string Value { get; }
    }

    public class EmptyGetOnlyConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public EmptyGetOnlyConstructorArgumentValueMarkupExtension(string value) { }

        [ConstructorArgument("")]
        public string Value { get; }
    }

    public class NoSuchNameConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public NoSuchNameConstructorArgumentValueMarkupExtension(string value) { }

        [ConstructorArgument("Value")]
        public string Value { get; set; }
    }

    public class NoSuchTypeConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public NoSuchTypeConstructorArgumentValueMarkupExtension(string value) { }

        [ConstructorArgument("value")]
        public int Value { get; set; }
    }

    public class PublicValidConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public PublicValidConstructorArgumentValueMarkupExtension(string value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public string PublicValue { get; set; }

        public event EventHandler Event { add { } remove { } }
    }

    public class GetOnlyConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public GetOnlyConstructorArgumentValueMarkupExtension(string stringValue, int intValue)
        {
            PublicValue = stringValue;
            GetOnlyValue = intValue;
        }

        [ConstructorArgument("stringValue")]
        public string PublicValue { get; set; }

        [ConstructorArgument("intValue")]
        public int GetOnlyValue { get; }

        public List<int> GetOnlyListValue { get; }

        public Dictionary<int, string> GetOnlyDictionaryValue { get; }

        public IXmlSerializable GetOnlyXDataValue { get; }
    }

    public class ProtectedValidConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public ProtectedValidConstructorArgumentValueMarkupExtension(int value)
        {
            ProtectedValue = value;
        }

        [ConstructorArgument("value")]
        protected int ProtectedValue { get; set; }
    }

    public class ProtectedGetOnlyValidConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public ProtectedGetOnlyValidConstructorArgumentValueMarkupExtension(int value)
        {
            ProtectedValue = value;
        }

        [ConstructorArgument("value")]
        protected int ProtectedValue { get; }
    }

    public class CustomValueSerializerAndTypeConverterConstructorArgumentValueMarkupExtension : MarkupExtensionBase
    {
        public CustomValueSerializerAndTypeConverterConstructorArgumentValueMarkupExtension(ClassWithValueSerializerAndTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithValueSerializerAndTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CustomValueSerializer))]
    [TypeConverter(typeof(CustomTypeConverter))]
    public class ClassWithValueSerializerAndTypeConverter { }

    public class CriticalExceptionCanConvertToStringParameterExtension : MarkupExtensionBase
    {
        public CriticalExceptionCanConvertToStringParameterExtension(ClassWithCriticalExceptionCanConvertToStringValueSerializer value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCriticalExceptionCanConvertToStringValueSerializer PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CriticalExceptionCanConvertToStringValueSerializer))]
    [TypeConverter(typeof(CustomTypeConverter))]
    public class ClassWithCriticalExceptionCanConvertToStringValueSerializer { }

    public class CriticalExceptionCanConvertToStringValueSerializer : CustomValueSerializer
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            throw new NullReferenceException();
        }
    }
    public class NonCriticalExceptionCanConvertToStringParameterExtension : MarkupExtension
    {
        public NonCriticalExceptionCanConvertToStringParameterExtension(ClassWithNonCriticalExceptionCanConvertToStringValueSerializer value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithNonCriticalExceptionCanConvertToStringValueSerializer PublicValue { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) => null;
    }

    [ValueSerializer(typeof(NonCriticalExceptionCanConvertToStringValueSerializer))]
    [TypeConverter(typeof(CustomTypeConverter))]
    public class ClassWithNonCriticalExceptionCanConvertToStringValueSerializer { }

    public class NonCriticalExceptionCanConvertToStringValueSerializer : CustomValueSerializer
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            throw new DivideByZeroException();
        }
    }

    public class CriticalExceptionConvertToStringParameterExtension : MarkupExtensionBase
    {
        public CriticalExceptionConvertToStringParameterExtension(ClassWithCriticalExceptionConvertToStringValueSerializer value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCriticalExceptionConvertToStringValueSerializer PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CriticalExceptionConvertToStringValueSerializer))]
    [TypeConverter(typeof(CustomTypeConverter))]
    public class ClassWithCriticalExceptionConvertToStringValueSerializer { }

    public class CriticalExceptionConvertToStringValueSerializer : CustomValueSerializer
    {
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            throw new NullReferenceException();
        }
    }
    public class NonCriticalExceptionConvertToStringParameterExtension : MarkupExtension
    {
        public NonCriticalExceptionConvertToStringParameterExtension(ClassWithNonCriticalExceptionConvertToStringValueSerializer value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithNonCriticalExceptionConvertToStringValueSerializer PublicValue { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) => null;
    }

    [ValueSerializer(typeof(NonCriticalExceptionConvertToStringValueSerializer))]
    [TypeConverter(typeof(CustomTypeConverter))]
    public class ClassWithNonCriticalExceptionConvertToStringValueSerializer { }

    public class NonCriticalExceptionConvertToStringValueSerializer : CustomValueSerializer
    {
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            throw new DivideByZeroException();
        }
    }

    public class CriticalExceptionCanConvertToParameterExtension : MarkupExtensionBase
    {
        public CriticalExceptionCanConvertToParameterExtension(ClassWithCriticalExceptionCanConvertToTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCriticalExceptionCanConvertToTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CantConvertToStringValueSerializer))]
    [TypeConverter(typeof(CriticalExceptionCanConvertToTypeConverter))]
    public class ClassWithCriticalExceptionCanConvertToTypeConverter { }

    public class CriticalExceptionCanConvertToTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            throw new NullReferenceException();
        }
    }

    public class NonCriticalExceptionCanConvertToParameterExtension : MarkupExtensionBase
    {
        public NonCriticalExceptionCanConvertToParameterExtension(ClassWithNonCriticalExceptionCanConvertToTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithNonCriticalExceptionCanConvertToTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CantConvertToStringValueSerializer))]
    [TypeConverter(typeof(NonCriticalExceptionCanConvertToTypeConverter))]
    public class ClassWithNonCriticalExceptionCanConvertToTypeConverter { }

    public class NonCriticalExceptionCanConvertToTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            throw new DivideByZeroException();
        }
    }



    public class CriticalExceptionConvertToParameterExtension : MarkupExtensionBase
    {
        public CriticalExceptionConvertToParameterExtension(ClassWithCriticalExceptionConvertToTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCriticalExceptionConvertToTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CantConvertToStringValueSerializer))]
    [TypeConverter(typeof(CriticalExceptionConvertToTypeConverter))]
    public class ClassWithCriticalExceptionConvertToTypeConverter { }

    public class CriticalExceptionConvertToTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return true;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NullReferenceException();
        }
    }

    public class NonCriticalExceptionConvertToParameterExtension : MarkupExtensionBase
    {
        public NonCriticalExceptionConvertToParameterExtension(ClassWithNonCriticalExceptionConvertToTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithNonCriticalExceptionConvertToTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CantConvertToStringValueSerializer))]
    [TypeConverter(typeof(NonCriticalExceptionConvertToTypeConverter))]
    public class ClassWithNonCriticalExceptionConvertToTypeConverter { }

    public class NonCriticalExceptionConvertToTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return true;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new DivideByZeroException();
        }
    }

    public class CriticalExceptionCanConvertFromParameterExtension : MarkupExtensionBase
    {
        public CriticalExceptionCanConvertFromParameterExtension(ClassWithCriticalExceptionCanConvertFromTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCriticalExceptionCanConvertFromTypeConverter PublicValue { get; set; }
    }

    [TypeConverter(typeof(CriticalExceptionCanConvertFromTypeConverter))]
    public class ClassWithCriticalExceptionCanConvertFromTypeConverter { }

    public class CriticalExceptionCanConvertFromTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            throw new NullReferenceException();
        }
    }

    public class NonCriticalExceptionCanConvertFromParameterExtension : MarkupExtensionBase
    {
        public NonCriticalExceptionCanConvertFromParameterExtension(ClassWithNonCriticalExceptionCanConvertFromTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithNonCriticalExceptionCanConvertFromTypeConverter PublicValue { get; set; }
    }

    [TypeConverter(typeof(NonCriticalExceptionCanConvertFromTypeConverter))]
    public class ClassWithNonCriticalExceptionCanConvertFromTypeConverter { }

    public class NonCriticalExceptionCanConvertFromTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            throw new DivideByZeroException();
        }
    }

    public class NoParameterExtension : MarkupExtensionBase
    {
        public NoParameterExtension(ClassWithNoTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithNoTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CustomValueSerializer))]
    public class ClassWithNoTypeConverter { }

    public class CustomTypeConverter : StringConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return "TypeConverter";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    public class CustomValueSerializer : ValueSerializer
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            return true;
        }

        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }
        
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            return "ValueSerializer";
        }
    }

    public class CantConvertToStringParameterExtension : MarkupExtensionBase
    {
        public CantConvertToStringParameterExtension(ClassWithCantConvertToStringValueSerializer value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCantConvertToStringValueSerializer PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CantConvertToStringValueSerializer))]
    [TypeConverter(typeof(CustomTypeConverter))]
    public class ClassWithCantConvertToStringValueSerializer { }

    public class CantConvertToStringValueSerializer : CustomValueSerializer
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            return false;
        }
    }

    public class CantConvertFromParameterExtension : MarkupExtensionBase
    {
        public CantConvertFromParameterExtension(ClassWithCantConvertFromTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithCantConvertFromTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CustomValueSerializer))]
    [TypeConverter(typeof(CantConvertFromTypeConverter))]
    public class ClassWithCantConvertFromTypeConverter { }

    public class CantConvertFromTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }
    }

    public class MarkupExtensionParameterExtension : MarkupExtensionBase
    {
        public MarkupExtensionParameterExtension(ClassWithMarkupExtensionTypeConverter value)
        {
            PublicValue = value;
        }

        [ConstructorArgument("value")]
        public ClassWithMarkupExtensionTypeConverter PublicValue { get; set; }
    }

    [ValueSerializer(typeof(CantConvertToStringValueSerializer))]
    [TypeConverter(typeof(MarkupExtensionTypeConverter))]
    public class ClassWithMarkupExtensionTypeConverter { }

    public class MarkupExtensionTypeConverter : CustomTypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(MarkupExtension);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new StaticExtension("member");
        }
    }

    public class ClassWithSameMarkupExtensionParameter : MarkupExtensionBase
    {
        public ClassWithSameMarkupExtensionParameter(int i) { } 

        public ClassWithSameMarkupExtensionParameter(ClassWithSameMarkupExtensionParameter value)
        {
            PublicValue = value;
        }
        
        [ConstructorArgument("value")]
        public ClassWithSameMarkupExtensionParameter PublicValue { get; set; }
    }

    public class ClassWithMarkupExtensionParameter : MarkupExtensionBase
    {
        public ClassWithMarkupExtensionParameter(MarkupExtension value)
        {
            PublicValue = value;
        }
        
        [ConstructorArgument("value")]
        public MarkupExtension PublicValue { get; set; }
    }

    [TypeConverter(typeof(CriticalExceptionCanConvertToInstanceDescriptorTypeConverter))]
    public class CriticalExceptionCanConvertToExtension : MarkupExtensionBase { }
    
    public class CriticalExceptionCanConvertToInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                throw new NullReferenceException();
            }

            return base.CanConvertTo(context, destinationType);
        }
    }

    [TypeConverter(typeof(NonCriticalExceptionCanConvertToInstanceDescriptorTypeConverter))]
    public class NonCriticalExceptionCanConvertToExtension : MarkupExtensionBase { }

    public class NonCriticalExceptionCanConvertToInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                throw new DivideByZeroException();
            }

            return base.CanConvertTo(context, destinationType);
        }
    }

    [TypeConverter(typeof(ValidInstanceDescriptorTypeConverter))]
    public class ValidInstanceDescriptorExtension : MarkupExtensionBase
    {
        public ValidInstanceDescriptorExtension(string value) { }
    }

    public class ValidInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(ValidInstanceDescriptorExtension).GetConstructors()[0], new object[] { "hello" });
        }
    }

    [TypeConverter(typeof(ArgumentCantConvertInstanceDescriptorNoDefaultConstructorTypeConverter))]
    public class ArgumentCantConvertInstanceDescriptorNoDefaultConstructorExtension : MarkupExtensionBase
    {
        public ArgumentCantConvertInstanceDescriptorNoDefaultConstructorExtension(ClassWithCantConvertFromTypeConverter value) { }
    }

    public class ArgumentCantConvertInstanceDescriptorNoDefaultConstructorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(ArgumentCantConvertInstanceDescriptorNoDefaultConstructorExtension).GetConstructors()[0], new object[] { new ClassWithCantConvertFromTypeConverter() });
        }
    }

    [TypeConverter(typeof(ArgumentCantConvertInstanceDescriptorDefaultConstructorTypeConverter))]
    public class ArgumentCantConvertInstanceDescriptorDefaultConstructorExtension : MarkupExtensionBase
    {
        public ArgumentCantConvertInstanceDescriptorDefaultConstructorExtension() { }
        public ArgumentCantConvertInstanceDescriptorDefaultConstructorExtension(ClassWithCantConvertFromTypeConverter value) { }
    }

    public class ArgumentCantConvertInstanceDescriptorDefaultConstructorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(ArgumentCantConvertInstanceDescriptorDefaultConstructorExtension).GetConstructor(new Type[] { typeof(ClassWithCantConvertFromTypeConverter) }), new object[] { new ClassWithCantConvertFromTypeConverter() });
        }
    }

    [TypeConverter(typeof(EmptyInstanceDescriptorTypeConverter))]
    public class EmptyInstanceDescriptorExtension : MarkupExtensionBase { }

    public class EmptyInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(EmptyInstanceDescriptorExtension).GetConstructors()[0], null);
        }
    }

    [TypeConverter(typeof(PropertyInfoInstanceDescriptorTypeConverter))]
    public class PropertyInfoInstanceDescriptorExtension : MarkupExtensionBase
    {
        public PropertyInfoInstanceDescriptorExtension(string value) { }

        public static int Property { get; set; }
    }

    public class PropertyInfoInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(PropertyInfoInstanceDescriptorExtension).GetProperties(BindingFlags.Static | BindingFlags.Public)[0], null);
        }
    }

    [TypeConverter(typeof(FieldInfoInstanceDescriptorTypeConverter))]
    public class FieldInfoInstanceDescriptorExtension : MarkupExtensionBase
    {
        public FieldInfoInstanceDescriptorExtension(string value) { }

        public static int s_field;
    }

    public class FieldInfoInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(FieldInfoInstanceDescriptorExtension).GetFields(BindingFlags.Static | BindingFlags.Public)[0], null);
        }
    }

    [TypeConverter(typeof(UnknownMemberInfoInstanceDescriptorTypeConverter))]
    public class UnknownMemberInfoInstanceDescriptorExtension : MarkupExtensionBase
    {
        public UnknownMemberInfoInstanceDescriptorExtension(string value) { }
    }

    public class UnknownMemberInfoInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(new UnknownMemberInfo(), null);
        }
    }

    [TypeConverter(typeof(UnknownMemberInfoWithArgumentsInstanceDescriptorTypeConverter))]
    public class UnknownMemberInfoWithArgumentsInstanceDescriptorExtension : MarkupExtensionBase
    {
        public UnknownMemberInfoWithArgumentsInstanceDescriptorExtension(string value) { }
    }

    public class UnknownMemberInfoWithArgumentsInstanceDescriptorTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(new UnknownMemberInfo(), new object[] { 1 });
        }
    }

    [TypeConverter(typeof(DeclaringTypeMethodInfoInstanceTypeConverter))]
    public class DeclaringTypeMethodInfoInstanceExtension : MarkupExtensionBase
    {
        public DeclaringTypeMethodInfoInstanceExtension(string value) { }

        public static DeclaringTypeMethodInfoInstanceExtension Factory(string value) => null;
    }

    public class DeclaringTypeMethodInfoInstanceTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(DeclaringTypeMethodInfoInstanceExtension).GetMethods(BindingFlags.Static | BindingFlags.Public)[0], new object[] { "value" });
        }
    }

    [TypeConverter(typeof(NullableNullMethodInfoInstanceTypeConverter))]
    public class NullableNullMethodInfoInstanceExtension : MarkupExtensionBase
    {
        public NullableNullMethodInfoInstanceExtension(int value) { }

        public static NullableNullMethodInfoInstanceExtension Factory(int? value) => null;
    }

    public class NullableNullMethodInfoInstanceTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(NullableNullMethodInfoInstanceExtension).GetMethods(BindingFlags.Static | BindingFlags.Public)[0], new object[] { null });
        }
    }

    [TypeConverter(typeof(NullableNonNullMethodInfoInstanceTypeConverter))]
    public class NullableNonNullMethodInfoInstanceExtension : MarkupExtensionBase
    {
        public NullableNonNullMethodInfoInstanceExtension(int value) { }

        public static NullableNonNullMethodInfoInstanceExtension Factory(int? value) => null;
    }

    public class NullableNonNullMethodInfoInstanceTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(NullableNonNullMethodInfoInstanceExtension).GetMethods(BindingFlags.Static | BindingFlags.Public)[0], new object[] { 1 });
        }
    }

    [TypeConverter(typeof(NonNullableNullMethodInfoInstanceTypeConverter))]
    public class NonNullableNullMethodInfoInstanceExtension : MarkupExtensionBase
    {
        public NonNullableNullMethodInfoInstanceExtension(int value) { }

        public static NonNullableNullMethodInfoInstanceExtension Factory(int value) => null;
    }

    public class NonNullableNullMethodInfoInstanceTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(NonNullableNullMethodInfoInstanceExtension).GetMethods(BindingFlags.Static | BindingFlags.Public)[0], new object[] { null });
        }
    }

    [TypeConverter(typeof(IncorrectParametersMethodInfoInstanceTypeConverter))]
    public class IncorrectParametersMethodInfoInstanceExtension : MarkupExtensionBase
    {
        public IncorrectParametersMethodInfoInstanceExtension(int value) { }

        public static IncorrectParametersMethodInfoInstanceExtension Factory(int value) => null;
    }

    public class IncorrectParametersMethodInfoInstanceTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(IncorrectParametersMethodInfoInstanceExtension).GetMethods(BindingFlags.Static | BindingFlags.Public)[0], new object[] { "string" });
        }
    }

    [TypeConverter(typeof(NonDeclaringTypeMethodInfoInstanceTypeConverter))]
    public class NonDeclaringTypeMethodInfoInstanceExtension : MarkupExtensionBase
    {
        public NonDeclaringTypeMethodInfoInstanceExtension(string value) { }
    }

    public class FactoryClass
    {
        public static NonDeclaringTypeMethodInfoInstanceExtension Factory(string value) => null;
    }

    public class NonDeclaringTypeMethodInfoInstanceTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(FactoryClass).GetMethods(BindingFlags.Static | BindingFlags.Public)[0], new object[] { "value" });
        }
    }
}
