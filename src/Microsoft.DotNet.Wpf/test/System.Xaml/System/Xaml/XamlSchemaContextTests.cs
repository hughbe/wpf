// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Markup;
using System.Xaml.Schema;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests
{
    public class XamlSchemaContextTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var context = new XamlSchemaContext();
            Assert.False(context.SupportMarkupExtensionsWithDuplicateArity);
            Assert.False(context.FullyQualifyAssemblyNamesInClrNamespaces);
            Assert.Null(context.ReferenceAssemblies);
        }

        public static IEnumerable<object[]> Ctor_XamlSchemaContextSettings_TestData()
        {
            yield return new object[] { null, false, false };
            yield return new object[]
            {
                new XamlSchemaContextSettings
                {
                    SupportMarkupExtensionsWithDuplicateArity = true,
                    FullyQualifyAssemblyNamesInClrNamespaces = true
                }, true, true
            };
        }

        [Theory]
        [MemberData(nameof(Ctor_XamlSchemaContextSettings_TestData))]
        public void Ctor_XamlSchemaContextSettings(XamlSchemaContextSettings settings, bool expectdSupportMarkupExtensionsWithDuplicateArity, bool expectedFullyQualifyAssemblyNamesInClrNamespaces)
        {
            var context = new XamlSchemaContext(settings);
            Assert.Equal(expectdSupportMarkupExtensionsWithDuplicateArity, context.SupportMarkupExtensionsWithDuplicateArity);
            Assert.Equal(expectedFullyQualifyAssemblyNamesInClrNamespaces, context.FullyQualifyAssemblyNamesInClrNamespaces);
            Assert.Null(context.ReferenceAssemblies);
        }

        public static IEnumerable<object[]> Ctor_ReferencedAssemblies_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Assembly[0] };
            yield return new object[] { new Assembly[] { typeof(XamlSchemaContextSettings).Assembly, typeof(int).Assembly, null } };
        }

        [Theory]
        [MemberData(nameof(Ctor_ReferencedAssemblies_TestData))]
        public void Ctor_ReferencedAssemblies(IEnumerable<Assembly> referenceAssemblies)
        {
            var context = new XamlSchemaContext(referenceAssemblies);
            Assert.False(context.SupportMarkupExtensionsWithDuplicateArity);
            Assert.False(context.FullyQualifyAssemblyNamesInClrNamespaces);
            Assert.Equal(referenceAssemblies, context.ReferenceAssemblies);
            if (referenceAssemblies != null)
            {
                Assert.NotSame(referenceAssemblies, context.ReferenceAssemblies);
            }
        }

        public static IEnumerable<object[]> Ctor_XamlSchemaContextSettings_ReferenceAssemblies_TestData()
        {
            yield return new object[] { null, null, false, false };
            yield return new object[]
            {
                new XamlSchemaContextSettings
                {
                    SupportMarkupExtensionsWithDuplicateArity = true,
                    FullyQualifyAssemblyNamesInClrNamespaces = true
                }, new Assembly[] { typeof(XamlSchemaContextSettings).Assembly, typeof(int).Assembly, null }, true, true
            };
            yield return new object[] { null, new Assembly[0], false, false };
        }

        [Theory]
        [MemberData(nameof(Ctor_XamlSchemaContextSettings_ReferenceAssemblies_TestData))]
        public void Ctor_XamlSchemaContextSettings_ReferenceAssemblies(XamlSchemaContextSettings settings, IEnumerable<Assembly> referenceAssemblies, bool expectdSupportMarkupExtensionsWithDuplicateArity, bool expectedFullyQualifyAssemblyNamesInClrNamespaces)
        {
            var context = new XamlSchemaContext(referenceAssemblies, settings);
            Assert.Equal(expectdSupportMarkupExtensionsWithDuplicateArity, context.SupportMarkupExtensionsWithDuplicateArity);
            Assert.Equal(expectedFullyQualifyAssemblyNamesInClrNamespaces, context.FullyQualifyAssemblyNamesInClrNamespaces);
            Assert.Equal(referenceAssemblies, context.ReferenceAssemblies);
            if (referenceAssemblies != null)
            {
                Assert.NotSame(referenceAssemblies, context.ReferenceAssemblies);
            }
        }

        [Theory]
        [InlineData("", "p")]
        [InlineData("namespace", "p")]
        [InlineData("http://schemas.microsoft.com/winfx/2006/xaml", "x")]
        [InlineData("clr-namespace:", "local")]
        [InlineData("clr-namespace:namespace", "local")]
        [InlineData("clr-namespace:namespace;assembly=", "local")]
        [InlineData("clr-namespace:namespace;assembly=assemblyName", "n")]
        [InlineData("clr-namespace:..;assembly=assemblyName", "local")]
        [InlineData("clr-namespace:;assembly=assemblyName", "local")]
        [InlineData("clr-namespace:namespace.sub.type;assembly=assemblyName", "nst")]
        [InlineData("clr-namespace:NAMESPACE.SUB.type;assembly=assemblyName", "nst")]
        [InlineData("clr-namespace:x;assembly=assemblyName", "p")]
        [InlineData("clr-namespace:x.n;assembly=assemblyName", "xn")]
        [InlineData("clr-namespace:x.m.l;assembly=assemblyName", "p")]
        [InlineData("clr-namespace:x.m.l.s;assembly=assemblyName", "xmls")]
        [InlineData("clr-namespace", "p")]
        [InlineData("other-namespace:", "p")]
        [InlineData(":", "p")]
        [InlineData("clr-namespace:namespace;", "p")]
        [InlineData("clr-namespace:namespace;assembly", "p")]
        [InlineData("clr-namespace:namespace;other-assembly=assemblyName", "p")]
        public void GetPreferredPrefix_Invoke_ReturnsExpected(string xmlns, string expected)
        {
            var context = new XamlSchemaContext();
            Assert.Equal(expected, context.GetPreferredPrefix(xmlns));
            Assert.Equal(expected, context.GetPreferredPrefix(xmlns));
        }

        [Theory]
        [InlineData("namespace", "prefix")]
        [InlineData("doubleNamespace1", "doublePrefix")]
        [InlineData("doubleNamespace2", "doublePrefix")]
        [InlineData("sameNamespace", "prefix1")]
        [InlineData("other", "p")]
        [InlineData("longerAssemblyNamespace", "longerAssemblyPrefix")]
        [InlineData("shorterAssemblyNamespace", "shorterAssemblyPrefi")]
        [InlineData("greaterAssemblyNamespace", "greaterAssemblyPrefix1")]
        [InlineData("lesserAssemblyNamespace", "lesserAssemblyPrefix1")]
        public void GetPreferredPrefix_OverridenCustomPrefixes_Success(string xmlns, string expected)
        {
            var assembly1 = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    {
                        typeof(XmlnsPrefixAttribute),
                        new Attribute[]
                        {
                            new XmlnsPrefixAttribute("namespace", "prefix"),
                            new XmlnsPrefixAttribute("doubleNamespace1", "doublePrefix"),
                            new XmlnsPrefixAttribute("doubleNamespace2", "doublePrefix"),
                            new XmlnsPrefixAttribute("sameNamespace", "prefix1"),
                            new XmlnsPrefixAttribute("sameNamespace", "prefix2"),
                            new XmlnsPrefixAttribute("longerAssemblyNamespace", "longerAssemblyPrefix"),
                            new XmlnsPrefixAttribute("shorterAssemblyNamespace", "shorterAssemblyPrefix"),
                            new XmlnsPrefixAttribute("greaterAssemblyNamespace", "greaterAssemblyPrefix2"),
                            new XmlnsPrefixAttribute("lesserAssemblyNamespace", "lesserAssemblyPrefix1")
                        }
                    }
                }
            };
            var assembly2 = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    {
                        typeof(XmlnsPrefixAttribute),
                        new Attribute[]
                        {
                            new XmlnsPrefixAttribute("longerAssemblyNamespace", "longerAssemblyPrefix2"),
                            new XmlnsPrefixAttribute("shorterAssemblyNamespace", "shorterAssemblyPrefi"),
                            new XmlnsPrefixAttribute("greaterAssemblyNamespace", "greaterAssemblyPrefix1"),
                            new XmlnsPrefixAttribute("lesserAssemblyNamespace", "lesserAssemblyPrefix2"),
                        }
                    }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly1, assembly2 });
            Assert.Equal(expected, context.GetPreferredPrefix(xmlns));
            Assert.Equal(expected, context.GetPreferredPrefix(xmlns));
        }

        [Fact]
        public void GetPreferredPrefix_NewAssemblySinceLastCall_Success()
        {
            var assembly1 = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    {
                        typeof(XmlnsPrefixAttribute),
                        new Attribute[]
                        {
                            new XmlnsPrefixAttribute("namespace1", "prefix1")
                        }
                    }
                }
            };
            var assembly2 = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    {
                        typeof(XmlnsPrefixAttribute),
                        new Attribute[]
                        {
                            new XmlnsPrefixAttribute("namespace2", "prefix2")
                        }
                    }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly1 });
            Assert.Equal("prefix1", context.GetPreferredPrefix("namespace1"));
            Assert.Equal("p", context.GetPreferredPrefix("namespace2"));

            AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("Name");
            TypeBuilder dynamicType = dynamicModule.DefineType("Type");
            Type createdDynamicType = dynamicType.CreateType();
            //context.ReferenceAssemblies.Add(assembly2);
            Assert.Equal("prefix1", context.GetPreferredPrefix("namespace1"));
            Assert.Equal("p", context.GetPreferredPrefix("namespace2"));
        }

        [Theory]
        [InlineData("namespace", "prefix")]
        [InlineData("doubleNamespace1", "doublePrefix")]
        [InlineData("doubleNamespace2", "doublePrefix")]
        [InlineData("sameNamespace", "prefix1")]
        [InlineData("other", "p")]
        public void GetPreferredPrefix_OverridenCustomPrefixesReflectionOnly_Success(string xmlns, string expected)
        {
            ConstructorInfo constructor = typeof(XmlnsPrefixAttribute).GetConstructors()[0];
            var assemblyWithPrefix = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                ReflectionOnlyResult = true,
                GetCustomAttributesDataResult = new CustomAttributeData[]
                {
                    new SubCustomAttributeData
                    {
                        ConstructorResult = constructor,
                        ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                        {
                            new CustomAttributeTypedArgument(typeof(string), "namespace"),
                            new CustomAttributeTypedArgument(typeof(string), "prefix")
                        }
                    },
                    new SubCustomAttributeData
                    {
                        ConstructorResult = constructor,
                        ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                        {
                            new CustomAttributeTypedArgument(typeof(string), "doubleNamespace1"),
                            new CustomAttributeTypedArgument(typeof(string), "doublePrefix")
                        }
                    },
                    new SubCustomAttributeData
                    {
                        ConstructorResult = constructor,
                        ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                        {
                            new CustomAttributeTypedArgument(typeof(string), "doubleNamespace2"),
                            new CustomAttributeTypedArgument(typeof(string), "doublePrefix")
                        }
                    },
                    new SubCustomAttributeData
                    {
                        ConstructorResult = constructor,
                        ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                        {
                            new CustomAttributeTypedArgument(typeof(string), "sameNamespace"),
                            new CustomAttributeTypedArgument(typeof(string), "prefix1")
                        }
                    },
                    new SubCustomAttributeData
                    {
                        ConstructorResult = constructor,
                        ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                        {
                            new CustomAttributeTypedArgument(typeof(string), "sameNamespace"),
                            new CustomAttributeTypedArgument(typeof(string), "prefix1")
                        }
                    },
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assemblyWithPrefix });
            Assert.Equal(expected, context.GetPreferredPrefix(xmlns));
        }

        [Fact]
        public void GetPreferredPrefix_NullXmlns_ThrowsArgumentNullException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<ArgumentNullException>("xmlns", () => context.GetPreferredPrefix(null));
        }

        public static IEnumerable<object[]> GetPreferredPrefix_InvalidXmlnsPrefixAttribute_TestData()
        {
            yield return new object[]
            {
                new CustomAttributeTypedArgument[]
                {
                    new CustomAttributeTypedArgument(typeof(string), null),
                    new CustomAttributeTypedArgument(typeof(string), "prefix")
                }
            };
            yield return new object[]
            {
                new CustomAttributeTypedArgument[]
                {
                    new CustomAttributeTypedArgument(typeof(string), ""),
                    new CustomAttributeTypedArgument(typeof(string), "prefix")
                }
            };
            yield return new object[]
            {
                new CustomAttributeTypedArgument[]
                {
                    new CustomAttributeTypedArgument(typeof(string), "xmlNamespace"),
                    new CustomAttributeTypedArgument(typeof(string), null)
                }
            };
            yield return new object[]
            {
                new CustomAttributeTypedArgument[]
                {
                    new CustomAttributeTypedArgument(typeof(string), "xmlNamespace"),
                    new CustomAttributeTypedArgument(typeof(string), "")
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetPreferredPrefix_InvalidXmlnsPrefixAttribute_TestData))]
        public void GetPreferredPrefix_InvalidXmlnsPrefixAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
        {
            var assembly = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                ReflectionOnlyResult = true,
                GetCustomAttributesDataResult = new CustomAttributeData[]
                {
                    new SubCustomAttributeData
                    {
                        ConstructorResult = typeof(XmlnsPrefixAttribute).GetConstructors()[0],
                        ConstructorArgumentsResult = arguments
                    }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly });
            Assert.Throws<XamlSchemaException>(() => context.GetPreferredPrefix("xmlns"));
        }

        [Fact]
        public void GetPreferredPrefix_NullAttributeResult_ThrowsNullReferenceException()
        {
            var assembly = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    { typeof(XmlnsPrefixAttribute), null }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly });
            Assert.Throws<NullReferenceException>(() => context.GetPreferredPrefix("xmlns"));
        }

        [Fact]
        public void GetPreferredPrefix_InvalidAttributeResultType_ThrowsInvalidCastException()
        {
            var assembly = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    { typeof(XmlnsPrefixAttribute), new object[] { new XmlnsDefinitionAttribute("xmlNamespace", "clrNamespace") } }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly });
            Assert.Throws<InvalidCastException>(() => context.GetPreferredPrefix("xmlns"));
        }

        [Fact]
        public void GetPreferredPrefix_NullItemInAttributeResult_ThrowsNullReferenceException()
        {
            var assembly = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    { typeof(XmlnsPrefixAttribute), new Attribute[] { null } }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly });
            Assert.Throws<NullReferenceException>(() => context.GetPreferredPrefix("xmlns"));
        }

        [Fact]
        public void GetPreferredPrefix_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
        {
            var assembly = new CustomAssembly(typeof(XamlTypeTests).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object[]>
                {
                    { typeof(XmlnsDefinitionAttribute), new Attribute[] { new AttributeUsageAttribute(AttributeTargets.All) } }
                }
            };
            var context = new XamlSchemaContext(new Assembly[] { assembly });
            Assert.Throws<InvalidCastException>(() => context.GetPreferredPrefix("xmlns"));
        }

        public static IEnumerable<object[]> GetXamlDirective_TestData()
        {
            const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
            const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

            yield return new object[] { XamlNamespace, "AsyncRecords", XamlLanguage.AsyncRecords };
            yield return new object[] { XamlNamespace, "Arguments", XamlLanguage.Arguments };
            yield return new object[] { XamlNamespace, "Class", XamlLanguage.Class };
            yield return new object[] { XamlNamespace, "ClassModifier", XamlLanguage.ClassModifier };
            yield return new object[] { XamlNamespace, "Code", XamlLanguage.Code };
            yield return new object[] { XamlNamespace, "ConnectionId", XamlLanguage.ConnectionId };
            yield return new object[] { XamlNamespace, "FactoryMethod", XamlLanguage.FactoryMethod };
            yield return new object[] { XamlNamespace, "FieldModifier", XamlLanguage.FieldModifier };
            yield return new object[] { XamlNamespace, "_Initialization", XamlLanguage.Initialization };
            yield return new object[] { XamlNamespace, "_Items", XamlLanguage.Items };
            yield return new object[] { XamlNamespace, "Key", XamlLanguage.Key };
            yield return new object[] { XamlNamespace, "Members", XamlLanguage.Members };
            yield return new object[] { XamlNamespace, "ClassAttributes", XamlLanguage.ClassAttributes };
            yield return new object[] { XamlNamespace, "Name", XamlLanguage.Name };
            yield return new object[] { XamlNamespace, "_PositionalParameters", XamlLanguage.PositionalParameters };
            yield return new object[] { XamlNamespace, "Shared", XamlLanguage.Shared };
            yield return new object[] { XamlNamespace, "Subclass", XamlLanguage.Subclass };
            yield return new object[] { XamlNamespace, "SynchronousMode", XamlLanguage.SynchronousMode };
            yield return new object[] { XamlNamespace, "TypeArguments", XamlLanguage.TypeArguments };
            yield return new object[] { XamlNamespace, "Uid", XamlLanguage.Uid };
            yield return new object[] { XamlNamespace, "_UnknownContent", XamlLanguage.UnknownContent };
            yield return new object[] { XamlNamespace, "name", null };
            yield return new object[] { XamlNamespace, "Base", null };
            yield return new object[] { XamlNamespace, "Language", null };
            yield return new object[] { XamlNamespace, "Space", null };
            yield return new object[] { XamlNamespace, "NoSuchDirective", null };
            yield return new object[] { XamlNamespace, "", null };

            yield return new object[] { XmlNamespace, "base", XamlLanguage.Base };
            yield return new object[] { XmlNamespace, "lang", XamlLanguage.Lang };
            yield return new object[] { XmlNamespace, "space", XamlLanguage.Space };
            yield return new object[] { XmlNamespace, "Base", null };
            yield return new object[] { XmlNamespace, "NoSuchDirective", null };
            yield return new object[] { XmlNamespace, "", null };

            yield return new object[] { "NoSuchNamespace", "Name", null };
        }

        [Theory]
        [MemberData(nameof(GetXamlDirective_TestData))]
        public void GetXamlDirective_Invoke_ReturnsExpected(string xamlNamespace, string name, XamlDirective expected)
        {
            var context = new XamlSchemaContext();
            Assert.Equal(expected, context.GetXamlDirective(xamlNamespace, name));
        }

        public static IEnumerable<object[]> GetXamlType_XamlTypeName_TestData()
        {
            const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
            string Name(Assembly assembly) => new AssemblyName(assembly.FullName).Name;

            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Array"),
                new XamlType(typeof(ArrayExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "ArrayExtension"),
                new XamlType(typeof(ArrayExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Member"),
                new XamlType(typeof(MemberDefinition), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Null"),
                new XamlType(typeof(NullExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "NullExtension"),
                new XamlType(typeof(NullExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Property"),
                new XamlType(typeof(PropertyDefinition), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Reference"),
                new XamlType(typeof(Reference), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "ReferenceExtension"),
                new XamlType(typeof(Reference), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Static"),
                new XamlType(typeof(StaticExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "StaticExtension"),
                new XamlType(typeof(StaticExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Type"),
                new XamlType(typeof(TypeExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "TypeExtension"),
                new XamlType(typeof(TypeExtension), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "String"),
                new XamlType(typeof(string), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Double"),
                new XamlType(typeof(double), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Int16"),
                new XamlType(typeof(short), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Int32"),
                new XamlType(typeof(int), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Int64"),
                new XamlType(typeof(long), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Boolean"),
                new XamlType(typeof(bool), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "XData"),
                new XamlType(typeof(XData), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Object"),
                new XamlType(typeof(object), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Char"),
                new XamlType(typeof(char), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Single"),
                new XamlType(typeof(float), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Byte"),
                new XamlType(typeof(byte), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Decimal"),
                new XamlType(typeof(decimal), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Uri"),
                new XamlType(typeof(Uri), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "TimeSpan"),
                new XamlType(typeof(TimeSpan), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "double"),
                null
            };
            yield return new object[]
            {
                new XamlTypeName(XamlNamespace, "Name"),
                null
            };

            // Valid assembly.
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System;assembly=" + typeof(int).Assembly.FullName, "Int32"),
                new XamlType(typeof(int), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System;assembly=" + Name(typeof(int).Assembly), "Int32"),
                new XamlType(typeof(int), new XamlSchemaContext())
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System.Xaml.Tests;assembly=" + typeof(XamlSchemaContextTests).Assembly.FullName, "XamlSchemaContextTests"),
                new XamlType(typeof(XamlSchemaContextTests), new XamlSchemaContext())
            };
        
            // Invalid assembly.
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System", "Int32"),
                null
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System;assembly=", "Int32"),
                null
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System;assembly=NoSuchAssembly", "Int32"),
                null
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System;assembly=NoSuchAssembly, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "Int32"),
                null
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:;assembly=" + Name(typeof(int).Assembly), "Int32"),
                null
            };
            yield return new object[]
            {
                new XamlTypeName("clr-namespace:System.Inner;assembly=" + Name(typeof(int).Assembly), "Int32"),
                null
            };

            yield return new object[]
            {
                new XamlTypeName("", ""),
                null
            };
        }

        [Theory]
        [MemberData(nameof(GetXamlType_XamlTypeName_TestData))]
        public void GetXamlType_TypeName_ReturnsExpected(XamlTypeName xamlTypeName, XamlType expected)
        {
            var context = new XamlSchemaContext();
            XamlType actual = context.GetXamlType(xamlTypeName);
            Assert.Equal(expected, actual);
            Assert.Same(actual, context.GetXamlType(xamlTypeName));
        }

        [Theory]
        [MemberData(nameof(GetXamlType_XamlTypeName_TestData))]
        public void GetXamlType_TypeNameFullyQualifyAssemblyNamesInClrNamespaces_ReturnsExpected(XamlTypeName xamlTypeName, XamlType expected)
        {
            var settings = new XamlSchemaContextSettings
            {
                FullyQualifyAssemblyNamesInClrNamespaces = true
            };
            var context = new XamlSchemaContext(settings);
            XamlType actual = context.GetXamlType(xamlTypeName);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetXamlType_NullXamlNamespace_ThrowsArgumentNullException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<ArgumentNullException>("xamlNamespace", () => context.GetXamlDirective(null, "name"));
        }

        [Fact]
        public void GetXamlType_NullName_ThrowsArgumentNullException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<ArgumentNullException>("name", () => context.GetXamlDirective("xamlNamespace", null));
        }

        [Fact]
        public void GetXamlType_NullXamlTypeName_ThrowsArgumentNullException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<ArgumentNullException>("xamlTypeName", () => context.GetXamlType((XamlTypeName)null));
        }

        [Fact]
        public void GetXamlType_NullXamlTypeNameNamespace_ThrowsArgumentException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<ArgumentException>("xamlTypeName", () => context.GetXamlType(new XamlTypeName(null, "name")));
        }

        [Fact]
        public void GetXamlType_NullXamlTypeNameName_ThrowsArgumentException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<ArgumentException>("xamlTypeName", () => context.GetXamlType(new XamlTypeName("xamlNamespace", null)));
        }

        [Fact]
        public void GetXamlType_NullXamlTypeNameTypeArgument_ThrowsArgumentException()
        {
            var context = new XamlSchemaContext();
            var xamlTypeName = new XamlTypeName("xamlNamespace", "name", new XamlTypeName[] { null });
            Assert.Throws<ArgumentException>("xamlTypeName", () => context.GetXamlType(xamlTypeName));
        }

        [Theory]
        [InlineData(typeof(int), "Int32")]
        [InlineData(typeof(MemberDefinition), "Member")]
        [InlineData(typeof(PropertyDefinition), "Property")]
        public void GetXamlType_Invoke_ReturnsExpected(Type underlyingType, string expectedName)
        {
            var context = new XamlSchemaContext();
            XamlType type = context.GetXamlType(underlyingType);
            Assert.Equal(new XamlType(underlyingType, new XamlSchemaContext()), type);
            Assert.Same(type, context.GetXamlType(underlyingType));
            Assert.Equal(expectedName, type.Name);
            Assert.Equal(underlyingType, type.UnderlyingType);
        }

        [Fact]
        public void GetXamlType_NullXamlType_ThrowsNullReferenceException()
        {
            var context = new XamlSchemaContext();
            Assert.Throws<NullReferenceException>(() => context.GetXamlType((Type)null));
        }
    }
}
