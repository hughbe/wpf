// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Xaml.Tests.Common;
using System.Windows.Markup;
using Xunit;
using System.Collections;

namespace System.Xaml.Tests
{
    public partial class XamlObjectReaderTests
    {
        [Fact]
        public void Ctor_MultidimensionalInstance_ThrowsXamlObjectReaderException()
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(new int[1, 1]));
        }

        [Fact]
        public void Ctor_PrivateArrayType_ThrowsXamlObjectReaderException()
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(new PrivateClass[1]));
        }

        [Fact]
        public void Ctor_UnknownArrayType_ThrowsArgumentNullException()
        {
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeResult = new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            };
            Assert.Throws<ArgumentNullException>("xamlType", () => new XamlObjectReader(new object[1], context));
        }

        public static IEnumerable<object[]> Ctor_GetEnumeratorThrowsCriticalException_TestData()
        {
            yield return new object[] { new CriticalExceptionCollectionGetEnumeratorClass() };
            yield return new object[] { new CriticalExceptionMoveNextCollectionGetEnumerator() };
        }

        [Theory]
        [MemberData(nameof(Ctor_GetEnumeratorThrowsCriticalException_TestData))]
        public void Ctor_GetEnumeratorThrowsCriticalException_ThrowsCriticalException(object instance)
        {
            Assert.Throws<NullReferenceException>(() => new XamlObjectReader(instance));
        }

        public static IEnumerable<object[]> Ctor_GetEnumeratorThrowsNonCriticalException_TestData()
        {
            yield return new object[] { new NonCriticalExceptionCollectionGetEnumeratorClass() };
            yield return new object[] { new NonCriticalExceptionMoveNextCollectionGetEnumerator() };
        }

        [Theory]
        [MemberData(nameof(Ctor_GetEnumeratorThrowsNonCriticalException_TestData))]
        public void Ctor_GetEnumeratorThrowsNonCriticalException_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
        }

        public static IEnumerable<object[]> Ctor_InvalidGetEnumeratorResult_TestData()
        {
            yield return new object[] { new NullGetEnumeratorClass() };
        }

        [Theory]
        [MemberData(nameof(Ctor_InvalidGetEnumeratorResult_TestData))]
        public void Ctor_InvalidGetEnumerator_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
        }

        [Fact]
        public void Read_EmptyArray_Success()
        {
            var reader = new XamlObjectReader(new object[0]);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Array),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Array.GetMember(nameof(ArrayExtension.Type))),
                new XamlNode(XamlNodeType.Value, "x:Object"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_NonEmptyArray_Success()
        {
            var reader = new XamlObjectReader(new object[] { 1, 2, 3 });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Array),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Array.GetMember(nameof(ArrayExtension.Type))),
                new XamlNode(XamlNodeType.Value, "x:Object"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Array.GetMember(nameof(ArrayExtension.Items))),
                new XamlNode(XamlNodeType.GetObject),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "3"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_IntContentWrapper_Success()
        {
            var reader = new XamlObjectReader(new IntContentWrapperList { new IntWrapper(1), new IntWrapper(2), new IntWrapper(3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(IntContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<IntWrapper>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(IntWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(IntWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.Value, "3"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_ListContainingNullAndVariousTypes_Success()
        {
            var reader = new XamlObjectReader(new ObjectContentWrapperList { null, new ObjectWrapper("1"), "2", new ClassWithObjectWrapperValue(new ObjectWrapper("value")) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(string), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }
        [Fact]
        public void Read_NoMatchingContentWrapper_Success()
        {
            var reader = new XamlObjectReader(new NoMatchingContentWrapper { 1, 2, 3 });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NoMatchingContentWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<int>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "3"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_MultipleProperties_Success()
        {
            var reader = new XamlObjectReader(new MultiplePropertiesContentWrapper
            {
                new ClassWithMultipleProperties(1, "value"),
                new ClassWithMultipleProperties(2, ""),
                new ClassWithMultipleProperties(3, null),
            });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(MultiplePropertiesContentWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<ClassWithMultipleProperties>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithMultipleProperties), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ClassWithMultipleProperties), new XamlSchemaContext()).GetMember("RuntimeName")),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(string), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithMultipleProperties), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(string), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, ""),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ClassWithMultipleProperties), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "3"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        public static IEnumerable<object[]> Read_SignificantWhitespace_TestData()
        {
            yield return new object[] { " 1  ", " 2  3  ", "  4  " };
            yield return new object[] { "\r\n\t 1 \r\n\t", "\r\n\t 2  3 \r\n\t", "\r\n\t 4 \r\n\t" };
            yield return new object[] { "\r 1 \r", "\n 2  3 \n", "\t 4 \t" };
            yield return new object[] { "1  2", "2  3 ", "4  5" };
        }

        [Theory]
        [MemberData(nameof(Read_SignificantWhitespace_TestData))]
        public void Read_WhitespaceSignificantCollectionSignificantWhitespaceValue_Success(string value1, string value2, string value3)
        {
            var reader = new XamlObjectReader(new WhitespaceSignificantObjectContentWrapperList { new ObjectWrapper(value1), new ObjectWrapper(value2), new ObjectWrapper(value3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(WhitespaceSignificantObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value1),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value2),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value3),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        public static IEnumerable<object[]> Read_SignificantWhitespaceFirstValue_TestData()
        {
            yield return new object[] { " 1", " 23", "  4" };
        }

        [Theory]
        [MemberData(nameof(Read_SignificantWhitespaceFirstValue_TestData))]
        public void Read_WhitespaceSignficantCollectionSignificantWhitespaceFirstValue_Success(string value1, string value2, string value3)
        {
            var reader = new XamlObjectReader(new WhitespaceSignificantObjectContentWrapperList { new ObjectWrapper(value1), new ObjectWrapper(value2), new ObjectWrapper(value3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(WhitespaceSignificantObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value1),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.Value, value2),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value3),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        public static IEnumerable<object[]> Read_InsignificantWhitespaceFirstValue_TestData()
        {
            yield return new object[] { "1 ", "23 ", "4 " };
        }

        [Theory]
        [MemberData(nameof(Read_InsignificantWhitespaceFirstValue_TestData))]
        public void ReadWhitespaceSignficiantCollectionInsignificiantWhitespaceFirstValue_Success(string value1, string value2, string value3)
        {
            var reader = new XamlObjectReader(new WhitespaceSignificantObjectContentWrapperList { new ObjectWrapper(value1), new ObjectWrapper(value2), new ObjectWrapper(value3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(WhitespaceSignificantObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.Value, value1),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value2),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value3),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        public static IEnumerable<object[]> Read_InsignificantWhitespace_TestData()
        {
            yield return new object[] { "", "", "" };
            yield return new object[] { "\u0008\u000B\u000E", "", "" };
            yield return new object[] { "1", "2 3 ", "4" };
            yield return new object[] { "1", "2  3 ", "4" };
        }

        [Theory]
        [MemberData(nameof(Read_InsignificantWhitespace_TestData))]
        public void Read_WhitespaceSignificantCollectionInsignificantWhitespaceValue_Success(string value1, string value2, string value3)
        {
            var reader = new XamlObjectReader(new WhitespaceSignificantObjectContentWrapperList { new ObjectWrapper(value1), new ObjectWrapper(value2), new ObjectWrapper(value3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(WhitespaceSignificantObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.Value, value1),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value2),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.Value, value3),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Theory]
        [MemberData(nameof(Read_SignificantWhitespace_TestData))]
        [MemberData(nameof(Read_SignificantWhitespaceFirstValue_TestData))]
        [MemberData(nameof(Read_InsignificantWhitespaceFirstValue_TestData))]
        public void Read_NonWhitespaceSignificantCollectionSignificantWhitespaceValue_Success(string value1, string value2, string value3)
        {
            var reader = new XamlObjectReader(new ObjectContentWrapperList { new ObjectWrapper(value1), new ObjectWrapper(value2), new ObjectWrapper(value3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value1),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value2),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value3),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Theory]
        [MemberData(nameof(Read_InsignificantWhitespace_TestData))]
        public void Read_NonWhitespaceSignificantCollectionInsignificantWhitespaceValue_Success(string value1, string value2, string value3)
        {
            var reader = new XamlObjectReader(new ObjectContentWrapperList { new ObjectWrapper(value1), new ObjectWrapper(value2), new ObjectWrapper(value3) });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectContentWrapperList), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<object>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.Value, value1),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(ObjectWrapper), new XamlSchemaContext()).GetMember("Value")),
                new XamlNode(XamlNodeType.Value, value2),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.Value, value3),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_EmptyList_Success()
        {
            var reader = new XamlObjectReader(new List<int>());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Collections.Generic;assembly=" + typeof(int).Assembly.Name(), "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(List<int>), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<int>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "0"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }
        
        [Fact]
        public void Read_NonEmptyList_Success()
        {
            var reader = new XamlObjectReader(new List<int> { 1, 2, 3 });
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Collections.Generic;assembly=" + typeof(int).Assembly.Name(), "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(List<int>), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(List<int>), new XamlSchemaContext()).GetMember("Capacity")),
                new XamlNode(XamlNodeType.Value, "4"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "3"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_EmptyDictionary_Success()
        {
            var reader = new XamlObjectReader(new Dictionary<int, string>());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Collections.Generic;assembly=System.Private.CoreLib", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(Dictionary<int, String>), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_NonEmptyDictionary_Success()
        {
            var reader = new XamlObjectReader(new Dictionary<object, string>()
            {
                {new object(), "a"},
                {2, "b"},
                {3, null}
            });
            //Dump(reader);
            /*AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Collections.Generic;assembly=System.Private.CoreLib", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(Dictionary<int, String>), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(String), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Key),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "a"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(String), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Key),
                new XamlNode(XamlNodeType.Value, "2"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "b"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Key),
                new XamlNode(XamlNodeType.Value, "3"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
             */
        }


        [Fact]
        public void T()
        {

        }
    }

    public class CriticalExceptionCollectionGetEnumeratorClass : IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new NullReferenceException();
        public void Add(object value) { }
    }

    public class NonCriticalExceptionCollectionGetEnumeratorClass : IEnumerable
    {
        public IEnumerator GetEnumerator() => throw new DivideByZeroException();
        public void Add(object value) { }
    }

    public class NullGetEnumeratorClass : IEnumerable
    {
        public IEnumerator GetEnumerator() => null;
        public void Add(object value) { }
    }

    public class CriticalExceptionMoveNextCollectionGetEnumerator : IEnumerable
    {
        public IEnumerator GetEnumerator() => new CriticalExceptionMoveNextEnumerator();
        public void Add(object value) { }
    }

    public class CriticalExceptionMoveNextEnumerator : IEnumerator
    {
        public bool MoveNext() => throw new NullReferenceException();

        public object Current { get; set; }
        public void Reset() { }
    }

    public class NonCriticalExceptionMoveNextCollectionGetEnumerator : IEnumerable
    {
        public IEnumerator GetEnumerator() => new NonCriticalExceptionMoveNextEnumerator();
        public void Add(object value) { }
    }

    public class NonCriticalExceptionMoveNextEnumerator : IEnumerator
    {
        public bool MoveNext() => throw new DivideByZeroException();

        public object Current { get; set; }
        public void Reset() { }
    }

    [ContentWrapper(typeof(ObjectWrapper))]
    [ContentWrapper(typeof(ClassWithObjectWrapperValue))]
    public class ObjectContentWrapperList : List<object>
    {
    }

    [ContentProperty(nameof(ClassWithObjectWrapperValue.Value))]
    public class ClassWithObjectWrapperValue
    {
        public ClassWithObjectWrapperValue() { }
    
        public ClassWithObjectWrapperValue(ObjectWrapper value) => Value = value;

        public ObjectWrapper Value { get; set; }
    }

    [ContentWrapper(typeof(ObjectWrapper))]
    [WhitespaceSignificantCollection]
    public class WhitespaceSignificantObjectContentWrapperList : List<object>
    {
    }

    [ContentProperty(nameof(ObjectWrapper.Value))]
    public class ObjectWrapper
    {
        public ObjectWrapper() { }
        public ObjectWrapper(string value) => Value = value;

        public object Value { get; set; }
    }

    [ContentWrapper(typeof(IntWrapper))]
    public class IntContentWrapperList : List<IntWrapper>
    {
    }

    [ContentProperty(nameof(IntWrapper.Value))]
    public class IntWrapper
    {
        public IntWrapper() { }
        public IntWrapper(int value) => Value = value;

        public int Value { get; set; }
    }

    [ContentWrapper(typeof(IntWrapper))]
    public class NoMatchingContentWrapper : List<int>
    {
    }

    [ContentWrapper(typeof(IntWrapper))]
    public class MultiplePropertiesContentWrapper : List<ClassWithMultipleProperties>
    {
    }

    [RuntimeNameProperty("RuntimeName")]
    public class ClassWithMultipleProperties
    {
        public ClassWithMultipleProperties(int intValue, string stringValue)
        {
            IntValue = intValue;
            StringValue = stringValue;
            RuntimeName = stringValue;
        }

        [ConstructorArgument("intValue")]
        public int IntValue { get; set; }

        [ConstructorArgument("stringValue")]
        public string StringValue { get; set; }

        public string RuntimeName { get; set; }
    }
}
