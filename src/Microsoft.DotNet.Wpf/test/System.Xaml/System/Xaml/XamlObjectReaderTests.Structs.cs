// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests
{
    public partial class XamlObjectReaderTests
    {
        public static IEnumerable<object[]> Ctor_NonConstructibleValueType_TestData()
        {
            yield return new object[] { new StructWithEmptyInstanceDescriptor() };
            yield return new object[] { new StructWithUnknownMemberInfoInstanceDescriptor() };
            yield return new object[] { new StructWithUnknownMemberInfoWithArgumentsInstanceDescriptor() };
        }

        [Theory]
        [MemberData(nameof(Ctor_NonConstructibleValueType_TestData))]
        public void Ctor_NonConstructibleValueType_ThrowsXamlObjectReaderException(object instance)
        {
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = (t) => 
                {
                    if (t == instance.GetType())
                    {
                        return new CustomXamlType(t, new XamlSchemaContext())
                        {
                            LookupIsConstructibleResult = false
                        };
                    }

                    return new XamlType(t, new XamlSchemaContext());
                }
            };
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance, context));
        }

        [Fact]
        public void Read_EmptyStruct_Success()
        {
            var reader = new XamlObjectReader(new EmptyStruct());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(EmptyStruct), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_StructWithEmptyValues_Success()
        {
            var reader = new XamlObjectReader(new StructWithProperties());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(StructWithProperties), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(StructWithProperties), new XamlSchemaContext()).GetMember("ListValue")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(StructWithProperties), new XamlSchemaContext()).GetMember("StringValue")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null),
                new XamlNode(XamlNodeType.EndObject),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.StartMember, new XamlType(typeof(StructWithProperties), new XamlSchemaContext()).GetMember("IntValue")),
                new XamlNode(XamlNodeType.Value, "0"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }
    }

    public struct EmptyStruct { }

    public struct StructWithProperties
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public List<string> ListValue { get; set; }
    }

    [TypeConverter(typeof(EmptyInstanceDescriptorStructTypeConverter))]
    public class StructWithEmptyInstanceDescriptor { }

    public class EmptyInstanceDescriptorStructTypeConverter : InstanceDescriptorTypeConverterBase
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return new InstanceDescriptor(typeof(StructWithEmptyInstanceDescriptor).GetConstructors()[0], null);
        }
    }

    [TypeConverter(typeof(UnknownMemberInfoInstanceDescriptorTypeConverter))]
    public struct StructWithUnknownMemberInfoInstanceDescriptor
    {
        public StructWithUnknownMemberInfoInstanceDescriptor(string value) { }
    }

    [TypeConverter(typeof(UnknownMemberInfoWithArgumentsInstanceDescriptorTypeConverter))]
    public struct StructWithUnknownMemberInfoWithArgumentsInstanceDescriptor
    {
        public StructWithUnknownMemberInfoWithArgumentsInstanceDescriptor(string value) { }
    }
}
