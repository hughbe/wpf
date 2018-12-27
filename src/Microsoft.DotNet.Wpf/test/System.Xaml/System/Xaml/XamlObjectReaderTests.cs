// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xaml.Tests.Common;
using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Tests
{
    public partial class XamlObjectReaderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void Ctor_Object(object instance)
        {
            var reader = new XamlObjectReader(instance);
            Assert.Null(reader.Instance);
            Assert.NotNull(reader.SchemaContext);
            Assert.Equal(XamlNodeType.None, reader.NodeType);
            Assert.False(reader.IsEof);
            Assert.Null(reader.Namespace);
            Assert.Null(reader.Type);
            Assert.Null(reader.Value);
            Assert.Null(reader.Member);
        }

        public static IEnumerable<object[]> Ctor_Object_XamlObjectReaderSettings_TestData()
        {
            yield return new object[] { 1, new XamlObjectReaderSettings() };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Object_XamlObjectReaderSettings_TestData))]
        public void Ctor_Object_XamlObjectReaderSettings(object instance, XamlObjectReaderSettings settings)
        {
            var reader = new XamlObjectReader(instance, settings);
            Assert.Null(reader.Instance);
            Assert.NotNull(reader.SchemaContext);
            Assert.Equal(XamlNodeType.None, reader.NodeType);
            Assert.False(reader.IsEof);
            Assert.Null(reader.Namespace);
            Assert.Null(reader.Type);
            Assert.Null(reader.Value);
            Assert.Null(reader.Member);
        }

        public static IEnumerable<object[]> Ctor_Object_XamlSchemaContext_TestData()
        {
            yield return new object[] { 1, new XamlSchemaContext() };
            yield return new object[] { null, new XamlSchemaContext() };
        }

        [Theory]
        [MemberData(nameof(Ctor_Object_XamlSchemaContext_TestData))]
        public void Ctor_Object_XamlSchemaContext(object instance, XamlSchemaContext schemaContext)
        {
            var reader = new XamlObjectReader(instance, schemaContext);
            Assert.Null(reader.Instance);
            Assert.Equal(schemaContext, reader.SchemaContext);
            Assert.Equal(XamlNodeType.None, reader.NodeType);
            Assert.False(reader.IsEof);
            Assert.Null(reader.Namespace);
            Assert.Null(reader.Type);
            Assert.Null(reader.Value);
            Assert.Null(reader.Member);
        }

        public static IEnumerable<object[]> Ctor_Object_XamlSchemaContext_XamlObjectReaderSettings_TestData()
        {
            yield return new object[] { 1, new XamlSchemaContext(), new XamlObjectReaderSettings() };
            yield return new object[] { null, new XamlSchemaContext(), null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Object_XamlSchemaContext_XamlObjectReaderSettings_TestData))]
        public void Ctor_Object_XamlSchemaContext_XamlObjectReaderSettings(object instance, XamlSchemaContext schemaContext, XamlObjectReaderSettings settings)
        {
            var reader = new XamlObjectReader(instance, schemaContext, settings);
            Assert.Null(reader.Instance);
            Assert.Equal(schemaContext, reader.SchemaContext);
            Assert.Equal(XamlNodeType.None, reader.NodeType);
            Assert.False(reader.IsEof);
            Assert.Null(reader.Namespace);
            Assert.Null(reader.Type);
            Assert.Null(reader.Value);
            Assert.Null(reader.Member);
        }

        [Fact]
        public void Ctor_NullSchemaContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlObjectReader(1, (XamlSchemaContext)null));
            Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlObjectReader(1, (XamlSchemaContext)null, new XamlObjectReaderSettings()));
        }

        [Fact]
        public void Ctor_SchemaContextReturnsNullType_ThrowsXamlObjectReaderException()
        {
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeResult = null
            };

            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(1, context));
        }

        public static IEnumerable<object[]> Ctor_InstanceNotVisible_TestData()
        {
            yield return new object[] { new InternalClass(), null };
            yield return new object[] { new InternalClass(), typeof(int).Assembly };
            yield return new object[] { new InternalClass.NestedClass(), null };
            yield return new object[] { new PrivateNestedClass(), typeof(PrivateNestedClass).Assembly };
            yield return new object[] { new PrivateNestedClass(), typeof(int).Assembly };
            yield return new object[] { new ProtectedNestedClass(), typeof(ProtectedNestedClass).Assembly };
            yield return new object[] { new ProtectedNestedClass(), typeof(int).Assembly };
            yield return new object[] { new InternalNestedClass(), typeof(InternalNestedClass).Assembly };
            yield return new object[] { new InternalNestedClass(), typeof(int).Assembly };
            yield return new object[] { new InternalGenericClass<InternalClass>(), null };
            yield return new object[] { new InternalGenericClass<InternalClass>(), typeof(int).Assembly };
        
            Type internalOtherAssemblyClass = typeof(Supporting.PublicClass).Assembly.GetType("System.Xaml.Tests.Supporting.InternalClass");
            yield return new object[] { Activator.CreateInstance(internalOtherAssemblyClass), null };
            yield return new object[] { Activator.CreateInstance(internalOtherAssemblyClass), typeof(XamlObjectReaderTests).Assembly };

            Type internalGenericArgumentType = typeof(PublicGenericClass<>).MakeGenericType(internalOtherAssemblyClass);
            yield return new object[] { Activator.CreateInstance(internalGenericArgumentType), null };
            yield return new object[] { Activator.CreateInstance(internalGenericArgumentType), typeof(XamlObjectReaderTests).Assembly };
        }

        [Theory]
        [MemberData(nameof(Ctor_InstanceNotVisible_TestData))]
        public void Ctor_InstanceNotVisible_ThrowsXamlObjectReaderException(object instance, Assembly localAssembly)
        {
            var settings = new XamlObjectReaderSettings { LocalAssembly = localAssembly };
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance, settings));
        }

        public static IEnumerable<object[]> Ctor_LookupIsPublicFalseNotVisible_TestData()
        {
            yield return new object[] { new EmptyClass(), null };
            yield return new object[] { new InternalClass[0], null };
            yield return new object[] { new InternalClass[0], typeof(Array).Assembly };

            Type internalOtherAssemblyClass = typeof(Supporting.PublicClass).Assembly.GetType("System.Xaml.Tests.Supporting.InternalClass");
            yield return new object[] { Array.CreateInstance(internalOtherAssemblyClass, 0), null };
            yield return new object[] { Array.CreateInstance(internalOtherAssemblyClass, 0), typeof(XamlObjectReaderTests).Assembly };
            yield return new object[] { Array.CreateInstance(internalOtherAssemblyClass, 0), typeof(Array).Assembly };
        }

        [Theory]
        [MemberData(nameof(Ctor_LookupIsPublicFalseNotVisible_TestData))]
        public void Ctor_LookupIsPublicFalseNoLocalAssembly_ThrowsXamlObjectReaderException(object instance, Assembly localAssembly)
        {
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new CustomXamlType(t, new XamlSchemaContext())
                {
                    LookupIsPublicResult = false
                }
            };

            var settings = new XamlObjectReaderSettings { LocalAssembly = localAssembly };
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance, context, settings));
        }

        [Fact]
        public void Ctor_AssemblyInternalsNullAttributeResult_ThrowsNullReferenceException()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), null }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<NullReferenceException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        [Fact]
        public void Ctor_AssemblyInternalsInvalidAttributeResultType_ThrowsInvalidCastException()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new object[] { new XmlnsDefinitionAttribute("xmlNamespace", "clrNamespace") } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<InvalidCastException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        [Fact]
        public void Ctor_AssemblyInternalsNullItemInAttributeResult_ThrowsNullReferenceException()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { null } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<NullReferenceException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        [Fact]
        public void Ctor_AssemblyInternalsInvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { new AttributeUsageAttribute(AttributeTargets.All) } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<InvalidCastException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("\0name")]
        [InlineData("Invalid, Version=1.1.1.1.1")]
        public void Ctor_AssemblyInternalsInvalidAssemblyName_ThrowsXamlSchemaException(string assemblyName)
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { new InternalsVisibleToAttribute(assemblyName) } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<XamlSchemaException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        public static IEnumerable<object[]> Ctor_AssemblyInternalsInvalidAssemblyNameAttribute_TestData()
        {
            yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
            yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(string), null) } };
            yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(string), "") } };
            yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(string), "\0name") } };
            yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(string), "Invalid, Version=1.1.1.1.1") } };
        }

        [Theory]
        [MemberData(nameof(Ctor_AssemblyInternalsInvalidAssemblyNameAttribute_TestData))]
        public void Ctor_AssemblyInternalsInvalidAssemblyNameAttribute_ThrowsXamlSchemaExcepiton(CustomAttributeTypedArgument[] arguments)
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        ReflectionOnlyResult = true,
                        GetCustomAttributesDataResult = new CustomAttributeData[]
                        {
                            new SubCustomAttributeData
                            {
                                ConstructorResult = typeof(InternalsVisibleToAttribute).GetConstructors()[0],
                                ConstructorArgumentsResult = arguments
                            }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<XamlSchemaException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        [Fact]
        public void Ctor_LocalAssemblyHasNoSuchInternalsVisibleTo_XamlObjectReaderException()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(XamlObjectReader).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { new InternalsVisibleToAttribute(typeof(XamlObjectReaderTests).Assembly.FullName) } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        [Fact]
        public void Ctor_LocalAssemblyHasNoSuchInternalsVisibleToWithPublicKeyToken_XamlObjectReaderException()
        {
            var name = new AssemblyName(typeof(XamlObjectReaderTests).Assembly.FullName);
            name.SetPublicKeyToken(new byte[] { 183, 122, 92, 86, 25, 52, 224, 137 });

            var otherName = new AssemblyName(typeof(XamlObjectReaderTests).Assembly.FullName);
            otherName.SetPublicKeyToken(new byte[] { 182, 122, 92, 86, 25, 52, 224, 137 });

            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = new CustomAssembly(typeof(XamlObjectReaderTests).Assembly)
                {
                    FullNameResult = otherName.FullName
                }
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { new InternalsVisibleToAttribute(name.FullName) } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(new InternalClass(), context, settings));
        }

        private class PrivateNestedClass { }

        protected class ProtectedNestedClass { }

        internal class InternalNestedClass { }

        public static IEnumerable<object[]> Ctor_NestedType_TestData()
        {
            yield return new object[] { new NestedClass() };
            yield return new object[] { new InternalClass.NestedClass() };
            yield return new object[] { new NestedStruct() };
        }

        [Theory]
        [MemberData(nameof(Ctor_NestedType_TestData))]
        public void Ctor_NestedType_ThrowsXamlObjectReaderException(object instance)
        {
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance));
            Assert.Throws<XamlObjectReaderException>(() => new XamlObjectReader(instance, new XamlObjectReaderSettings
            {
                LocalAssembly = instance.GetType().Assembly
            }));
        }

        public class NestedClass { }
        public class NestedStruct { }

        [Fact]
        public void Read_Null_Success()
        {
            var reader = new XamlObjectReader(null);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NullExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_NullExtension_Success()
        {
            var reader = new XamlObjectReader(new NullExtension());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(NullExtension), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_String_Success()
        {
            var reader = new XamlObjectReader("value");
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(String), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "value"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_Int_Success()
        {
            var reader = new XamlObjectReader(1);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(int), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                new XamlNode(XamlNodeType.Value, "1"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_Type_Success()
        {
            var reader = new XamlObjectReader(typeof(int));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Type),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "x:Int32"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_TypeExtensionType_Success()
        {
            var reader = new XamlObjectReader(new TypeExtension(typeof(int)));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Type),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "x:Int32"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_TypeExtensionString_Success()
        {
            var reader = new XamlObjectReader(new TypeExtension("typeName"));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Type),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, ""),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_StaticExtensionDefault_Success()
        {
            var reader = new XamlObjectReader(new StaticExtension());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Static),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, null),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_StaticExtensionString_Success()
        {
            var reader = new XamlObjectReader(new StaticExtension("member"));
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Static),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters),
                new XamlNode(XamlNodeType.Value, "member"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_EmptyClass_Success()
        {
            var reader = new XamlObjectReader(new EmptyClass());
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=" + typeof(EmptyClass).Assembly.Name(), "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(EmptyClass), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_PublicTypeWithLocalAssembly_Success()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(EmptyClass).Assembly
            };
            var reader = new XamlObjectReader(new EmptyClass(), settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=" + typeof(EmptyClass).Assembly.Name(), "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(EmptyClass), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_PublicTypeWithLocalAssemblyLookupIsPublicFalse_Success()
        {
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t =>
                {
                    return new CustomXamlType(t, new XamlSchemaContext())
                    {
                        LookupIsPublicResult = false
                    };
                }
            };
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(EmptyClass).Assembly
            };
            var reader = new XamlObjectReader(new EmptyClass(), context, settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=" + typeof(EmptyClass).Assembly.Name(), "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(EmptyClass), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_PublicTypeWithInternalGenericArgumentWithLocalAssembly_Success()
        {
            Type internalOtherAssemblyClass = typeof(Supporting.PublicClass).Assembly.GetType("System.Xaml.Tests.Supporting.InternalClass");
            Type internalGenericArgumentType = typeof(PublicGenericClass<>).MakeGenericType(internalOtherAssemblyClass);

            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new CustomXamlType(t, new XamlSchemaContext())
                {
                    LookupIsPublicResult = false
                }
            };
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = internalOtherAssemblyClass.Assembly
            };
            var reader = new XamlObjectReader(Activator.CreateInstance(internalGenericArgumentType), context, settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests.Supporting;assembly=System.Xaml.Tests.Supporting", "sxts")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(internalGenericArgumentType, new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_ArrayWithInternalGenericArgumentWithLocalAssembly_Success()
        {
            Type internalOtherAssemblyClass = typeof(Supporting.PublicClass).Assembly.GetType("System.Xaml.Tests.Supporting.InternalClass");

            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new CustomXamlType(t, new XamlSchemaContext())
                {
                    LookupIsPublicResult = false
                }
            };
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = internalOtherAssemblyClass.Assembly
            };
            var reader = new XamlObjectReader(Array.CreateInstance(internalOtherAssemblyClass, 0), context, settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests.Supporting;assembly=System.Xaml.Tests.Supporting", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, XamlLanguage.Array),
                new XamlNode(XamlNodeType.StartMember, XamlLanguage.Array.GetMember(nameof(ArrayExtension.Type))),
                new XamlNode(XamlNodeType.Value, "InternalClass"),
                new XamlNode(XamlNodeType.EndMember),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_InternalTypeWithLookupIsPublicTrue_Success()
        {
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new CustomXamlType(t, new XamlSchemaContext())
                {
                    LookupIsPublicResult = true
                }
            };
            var reader = new XamlObjectReader(new InternalClass(), context);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(InternalClass), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_InternalTypeWithLocalAssembly_Success()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(InternalClass).Assembly
            };
            var reader = new XamlObjectReader(new InternalClass(), settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(InternalClass), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_InternalGenericTypeWithInternalGenericArgumentWithLocalAssembly_Success()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(InternalClass).Assembly
            };
            var reader = new XamlObjectReader(new InternalGenericClass<InternalClass>(), settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("http://schemas.microsoft.com/winfx/2006/xaml", "x")),
                new XamlNode(XamlNodeType.StartObject, new XamlType(typeof(InternalGenericClass<InternalClass>), new XamlSchemaContext())),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_LocalAssemblyHasInternalsVisibleToWithoutPublicKeyToken_Success()
        {
            var name = new AssemblyName(typeof(int).Assembly.FullName);
            name.SetPublicKeyToken(null);

            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { new InternalsVisibleToAttribute(name.FullName) } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            var reader = new XamlObjectReader(new InternalClass(), context, settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, context.GetXamlType(typeof(InternalClass))),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_LocalAssemblyHasInternalsVisibleToWithPublicKeyToken_Success()
        {
            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        GetCustomAttributesMap = new Dictionary<Type, object[]>
                        {
                            { typeof(InternalsVisibleToAttribute), new Attribute[] { new InternalsVisibleToAttribute(typeof(int).Assembly.FullName) } }
                        }
                    }
                }, new XamlSchemaContext())
            };
            var reader = new XamlObjectReader(new InternalClass(), context, settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, context.GetXamlType(typeof(InternalClass))),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        [Fact]
        public void Read_LocalAssemblyHasInternalsVisibleToReflectionOnly_Success()
        {
            var name = new AssemblyName(typeof(int).Assembly.FullName);
            name.SetPublicKeyToken(null);

            var settings = new XamlObjectReaderSettings
            {
                LocalAssembly = typeof(int).Assembly
            };
            var context = new CustomXamlSchemaContext
            {
                GetXamlTypeFactory = t => new XamlType(new CustomType(t)
                {
                    AssemblyResult = new CustomAssembly(t.Assembly)
                    {
                        ReflectionOnlyResult = true,
                        GetCustomAttributesDataResult = new CustomAttributeData[]
                        {
                            new SubCustomAttributeData
                            {
                                ConstructorResult = typeof(InternalsVisibleToAttribute).GetConstructors()[0],
                                ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                                {
                                    new CustomAttributeTypedArgument(typeof(string), name.FullName)
                                }
                            }
                        }
                    }
                }, new XamlSchemaContext())
            };
            var reader = new XamlObjectReader(new InternalClass(), context, settings);
            AssertRead(reader,
                new XamlNode(XamlNodeType.NamespaceDeclaration, new NamespaceDeclaration("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", "")),
                new XamlNode(XamlNodeType.StartObject, context.GetXamlType(typeof(InternalClass))),
                new XamlNode(XamlNodeType.EndObject)
            );
        }

        private static void AssertRead(XamlObjectReader reader, params XamlNode[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.True(reader.Read());

                XamlNode node = expected[i];
                XamlNodeType type = node.Type;
                if (type == XamlNodeType.NamespaceDeclaration)
                {
                    Assert.Equal(XamlNodeType.NamespaceDeclaration, reader.NodeType);
                    Assert.False(reader.IsEof);
                    AssertEqualNamespaceDeclaration((NamespaceDeclaration)node.Value, reader.Namespace);
                    Assert.Null(reader.Type);
                    Assert.Null(reader.Value);
                    Assert.Null(reader.Member);
                }
                else if (type == XamlNodeType.StartObject)
                {
                    Assert.Equal(XamlNodeType.StartObject, reader.NodeType);
                    Assert.False(reader.IsEof);
                    Assert.Null(reader.Namespace);
                    Assert.Equal((XamlType)node.Value, reader.Type);
                    Assert.Null(reader.Value);
                    Assert.Null(reader.Member);
                }
                else if (type == XamlNodeType.StartMember)
                {
                    Assert.Equal(XamlNodeType.StartMember, reader.NodeType);
                    Assert.False(reader.IsEof);
                    Assert.Null(reader.Namespace);
                    Assert.Null(reader.Type);
                    Assert.Null(reader.Value);
                    Assert.Equal((XamlMember)node.Value, reader.Member);
                }
                else if (type == XamlNodeType.Value)
                {
                    Assert.Equal(XamlNodeType.Value, reader.NodeType);
                    Assert.False(reader.IsEof);
                    Assert.Null(reader.Namespace);
                    Assert.Null(reader.Type);
                    Assert.Equal(node.Value, reader.Value);
                    Assert.Null(reader.Member);
                }
                else if (type == XamlNodeType.GetObject)
                {
                    Assert.Equal(XamlNodeType.GetObject, reader.NodeType);
                    Assert.False(reader.IsEof);
                    Assert.Null(reader.Namespace);
                    Assert.Null(reader.Type);
                    Assert.Null(reader.Value);
                    Assert.Null(reader.Member);
                }
                else if (type == XamlNodeType.EndMember)
                {
                    Assert.Equal(XamlNodeType.EndMember, reader.NodeType);
                    Assert.False(reader.IsEof);
                    Assert.Null(reader.Namespace);
                    Assert.Null(reader.Type);
                    Assert.Null(reader.Value);
                    Assert.Null(reader.Member);
                }
                else if (type == XamlNodeType.EndObject)
                {
                    Assert.Equal(XamlNodeType.EndObject, reader.NodeType);
                    Assert.False(reader.IsEof);
                    Assert.Null(reader.Namespace);
                    Assert.Null(reader.Type);
                    Assert.Null(reader.Value);
                    Assert.Null(reader.Member);
                }
            }

            if (reader.Read())
            {
                throw new InvalidOperationException($"Expected no more to read, but got: {reader.NodeType}");
            }

            Assert.Equal(XamlNodeType.None, reader.NodeType);
            Assert.True(reader.IsEof);
            Assert.Null(reader.Namespace);
            Assert.Null(reader.Type);
            Assert.Null(reader.Value);
            Assert.Null(reader.Member);
        }

        private static void AssertEqualNamespaceDeclaration(NamespaceDeclaration expected, NamespaceDeclaration actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }

            Assert.Equal(expected.Namespace, actual.Namespace);
            Assert.Equal(expected.Prefix, actual.Prefix);
        }

        private static string TypeName(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }
            else if (type == typeof(object))
            {
                return "object";
            }
            else if (type == typeof(string))
            {
                return "string";
            }

            if (type.IsGenericType)
            {
                var nameBuilder = new StringBuilder();
                nameBuilder.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
                nameBuilder.Append("<");
                Type[] arguments = type.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    nameBuilder.Append(TypeName(arguments[i]));
                    if (i != arguments.Length - 1)
                    {
                        nameBuilder.Append(", ");
                    }
                }
                nameBuilder.Append(">");
                return nameBuilder.ToString();
            }

            return type.Name;
        }

        private static string XamlTypeName(XamlType type)
        {
            if (type == XamlLanguage.Array)
            {
                return "XamlLanguage.Array";
            }
            else if (type == XamlLanguage.Type)
            {
                return "XamlLanguage.Type";
            }
            else if (type == XamlLanguage.Null)
            {
                return "XamlLanguage.Null";
            }
            else if (type == XamlLanguage.Static)
            {
                return "XamlLanguage.Static";
            }
            else if (type == XamlLanguage.Reference)
            {
                return "XamlLanguage.Reference";
            }
            else
            {
                return "new XamlType(typeof(" + TypeName(type.UnderlyingType) + "), new XamlSchemaContext())";
            }
        }
        
        private static string Dump(XamlObjectReader reader)
        {
            var s = new StringBuilder();
            s.AppendLine("            AssertRead(reader,");
            while (reader.Read())
            {
                s.Append("                ");
                if (reader.NodeType == XamlNodeType.NamespaceDeclaration)
                {
                    s.Append("new XamlNode(XamlNodeType.NamespaceDeclaration, ");
                    s.Append("new NamespaceDeclaration(\"" + reader.Namespace.Namespace + "\", \"" + reader.Namespace.Prefix + "\")");
                    s.AppendLine("),");
                }
                else if (reader.NodeType == XamlNodeType.StartObject)
                {
                    s.Append("new XamlNode(XamlNodeType.StartObject, ");
                    s.Append(XamlTypeName(reader.Type));
                    s.AppendLine("),");
                }
                else if (reader.NodeType == XamlNodeType.StartMember)
                {
                    s.Append("new XamlNode(XamlNodeType.StartMember, ");
                    if (reader.Member == XamlLanguage.Array.GetMember("Type"))
                    {
                        s.Append("XamlLanguage.Array.GetMember(nameof(ArrayExtension.Type))");
                    }
                    else if (reader.Member == XamlLanguage.Array.GetMember("Items"))
                    {
                        s.Append("XamlLanguage.Array.GetMember(nameof(ArrayExtension.Items))");
                    }
                    else if (reader.Member == XamlLanguage.Items)
                    {
                        s.Append("XamlLanguage.Items");
                    }
                    else if (reader.Member == XamlLanguage.Initialization)
                    {
                        s.Append("XamlLanguage.Initialization");
                    }
                    else if (reader.Member == XamlLanguage.PositionalParameters)
                    {
                        s.Append("XamlLanguage.PositionalParameters");
                    }
                    else if (reader.Member == XamlLanguage.Arguments)
                    {
                        s.Append("XamlLanguage.Arguments");
                    }
                    else if (reader.Member == XamlLanguage.FactoryMethod)
                    {
                        s.Append("XamlLanguage.FactoryMethod");
                    }
                    else if (reader.Member == XamlLanguage.Key)
                    {
                        s.Append("XamlLanguage.Key");
                    }
                    else if (reader.Member == XamlLanguage.Items)
                    {
                        s.Append("XamlLanguage.Items");
                    }
                    else
                    {
                        if (reader.Member.DeclaringType == null)
                        {
                            throw new InvalidOperationException("Unknown member: " + reader.Member.ToString());
                        }

                        s.Append(XamlTypeName(reader.Member.DeclaringType) + ".GetMember(\"" + reader.Member.Name + "\")");
                    }
                    
                    s.AppendLine("),");
                }
                else if (reader.NodeType == XamlNodeType.Value)
                {
                    s.Append("new XamlNode(XamlNodeType.Value, ");
                    if (reader.Value == null)
                    {
                        s.Append("null");
                    }
                    else
                    {
                        s.Append("\"" + reader.Value + "\"");
                    }
                    s.AppendLine("),");
                }
                else if (reader.NodeType == XamlNodeType.GetObject)
                {
                    s.Append("new XamlNode(XamlNodeType.GetObject");
                    s.AppendLine("),");
                }
                else if (reader.NodeType == XamlNodeType.EndMember)
                {
                    s.Append("new XamlNode(XamlNodeType.EndMember");
                    s.AppendLine("),");
                }
                else if (reader.NodeType == XamlNodeType.EndObject)
                {
                    s.Append("new XamlNode(XamlNodeType.EndObject");
                    s.AppendLine("),");
                }
            }
            
            s.Remove(s.Length - 2, 1);
            s.Append("            );");
            Console.WriteLine(s.ToString());
            return s.ToString();
        }
        private struct XamlNode
        {
            public XamlNodeType Type { get; set; }
            public object Value { get; set; }

            public XamlNode(XamlNodeType type) : this(type, null)
            {
            }

            public XamlNode(XamlNodeType type, object value)
            {
                Type = type;
                Value = value;
            }
        }
    }

    public static class AssemblyExtensions
    {
        public static string Name(this Assembly assembly)
        {
            return new AssemblyName(assembly.FullName).Name;
        }
    }

    public class EmptyClass { }

    internal class InternalClass
    {
        public class NestedClass { }
    }

    public class PublicGenericClass<T> { }
    internal class InternalGenericClass<T> { }
}
