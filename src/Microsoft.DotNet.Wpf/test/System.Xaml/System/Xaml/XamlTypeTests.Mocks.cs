// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Windows.Markup;
using System.Xaml.Schema;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests
{
    public partial class XamlTypeTests
    {
        public class SubXamlType : XamlType
        {
            public SubXamlType(string unknownTypeNamespace, string unknownTypeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext) : base(unknownTypeNamespace, unknownTypeName, typeArguments, schemaContext) { }

            public SubXamlType(Type underlyingType, XamlSchemaContext schemaContext) : base(underlyingType, schemaContext) { }

            public SubXamlType(string typeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext) : base(typeName, typeArguments, schemaContext) { }

            public XamlMember LookupAliasedPropertyEntry(XamlDirective directive) => LookupAliasedProperty(directive);

            public IEnumerable<XamlMember> LookupAllAttachableMembersEntry() => LookupAllAttachableMembers();

            public IEnumerable<XamlMember> LookupAllMembersEntry() => LookupAllMembers();

            public IList<XamlType> LookupAllowedContentTypesEntry() => LookupAllowedContentTypes();

            public XamlMember LookupAttachableMemberEntry(string name) => LookupAttachableMember(name);

            public XamlType LookupBaseTypeEntry() => LookupBaseType();

            public XamlCollectionKind LookupCollectionKindEntry() => LookupCollectionKind();

            public bool LookupConstructionRequiresArgumentsEntry() => LookupConstructionRequiresArguments();

            public XamlMember LookupContentPropertyEntry() => LookupContentProperty();

            public IList<XamlType> LookupContentWrappersEntry() => LookupContentWrappers();

            public ICustomAttributeProvider LookupCustomAttributeProviderEntry() => LookupCustomAttributeProvider();

            public XamlValueConverter<XamlDeferringLoader> LookupDeferringLoaderEntry() => LookupDeferringLoader();

            public XamlTypeInvoker LookupInvokerEntry() => LookupInvoker();

            public bool LookupIsAmbientEntry() => LookupIsAmbient();

            public bool LookupIsConstructibleEntry() => LookupIsConstructible();

            public bool LookupIsMarkupExtensionEntry() => LookupIsMarkupExtension();

            public bool LookupIsNameScopeEntry() => LookupIsNameScope();

            public bool LookupIsNullableEntry() => LookupIsNullable();

            public bool LookupIsPublicEntry() => LookupIsPublic();

            public bool LookupIsUnknownEntry() => LookupIsUnknown();

            public bool LookupIsWhitespaceSignificantCollectionEntry() => LookupIsWhitespaceSignificantCollection();

            public bool LookupIsXDataEntry() => LookupIsXData();

            public XamlType LookupItemTypeEntry() => LookupItemType();

            public XamlType LookupKeyTypeEntry() => LookupKeyType();

            public XamlType LookupMarkupExtensionReturnTypeEntry() => LookupMarkupExtensionReturnType();

            public XamlMember LookupMemberEntry(string name, bool skipReadOnlyCheck) => LookupMember(name, skipReadOnlyCheck);

            public IList<XamlType> LookupPositionalParametersEntry(int parameterCount) => LookupPositionalParameters(parameterCount);

            public EventHandler<XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandlerEntry() => LookupSetMarkupExtensionHandler();

            public EventHandler<XamlSetTypeConverterEventArgs> LookupSetTypeConverterHandlerEntry() => LookupSetTypeConverterHandler();

            public XamlValueConverter<TypeConverter> LookupTypeConverterEntry() => LookupTypeConverter();

            public Type LookupUnderlyingTypeEntry() => LookupUnderlyingType();

            public bool LookupUsableDuringInitializationEntry() => LookupUsableDuringInitialization();

            public bool LookupTrimSurroundingWhitespaceEntry() => LookupTrimSurroundingWhitespace();

            public XamlValueConverter<ValueSerializer> LookupValueSerializerEntry() => LookupValueSerializer();
        }

        private class NoUnderlyingOrBaseType : SubXamlType
        {
            public NoUnderlyingOrBaseType() : base("name", null, new XamlSchemaContext()) { }

            protected override bool LookupIsUnknown() => false;
            protected override XamlType LookupBaseType() => null;
        }

        private class CustomXamlType : SubXamlType
        {
            public CustomXamlType(string unknownTypeNamespace, string unknownTypeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext) : base(unknownTypeNamespace, unknownTypeName, typeArguments, schemaContext) { }

            public CustomXamlType(Type underlyingType, XamlSchemaContext schemaContext) : base(underlyingType, schemaContext) { }

            public CustomXamlType(string typeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext) : base(typeName, typeArguments, schemaContext) { }

            public Optional<XamlMember> LookupAliasedPropertyResult { get; set; }
            protected override XamlMember LookupAliasedProperty(XamlDirective directive)
            {
                return LookupAliasedPropertyResult.Or(base.LookupAliasedProperty, directive);
            }

            public Optional<IEnumerable<XamlMember>> LookupAllAttachableMembersResult { get; set; }
            protected override IEnumerable<XamlMember> LookupAllAttachableMembers()
            {
                return LookupAllAttachableMembersResult.Or(base.LookupAllAttachableMembers);
            }

            public Optional<IEnumerable<XamlMember>> LookupAllMembersResult { get; set; }
            protected override IEnumerable<XamlMember> LookupAllMembers()
            {
                return LookupAllMembersResult.Or(base.LookupAllMembers);
            }

            public Optional<IList<XamlType>> LookupAllowedContentTypesResult { get; set; }
            protected override IList<XamlType> LookupAllowedContentTypes()
            {
                return LookupAllowedContentTypesResult.Or(base.LookupAllowedContentTypes);
            }

            public Optional<XamlMember> LookupAttachableMemberResult { get; set; }
            protected override XamlMember LookupAttachableMember(string name)
            {
                return LookupAttachableMemberResult.Or(base.LookupAttachableMember, name);
            }

            public Optional<XamlType> LookupBaseTypeResult { get; set; }
            protected override XamlType LookupBaseType()
            {
                return LookupBaseTypeResult.Or(base.LookupBaseType);
            }

            public Optional<XamlCollectionKind> LookupCollectionKindResult { get; set; }
            protected override XamlCollectionKind LookupCollectionKind()
            {
                return LookupCollectionKindResult.Or(base.LookupCollectionKind);
            }

            public Optional<bool> LookupConstructionRequiresArgumentsResult { get; set; }
            protected override bool LookupConstructionRequiresArguments()
            {
                return LookupConstructionRequiresArgumentsResult.Or(base.LookupConstructionRequiresArguments);
            }

            public Optional<ICustomAttributeProvider> LookupCustomAttributeProviderResult { get; set; }

            protected override ICustomAttributeProvider LookupCustomAttributeProvider()
            {
                return LookupCustomAttributeProviderResult.Or(base.LookupCustomAttributeProvider);
            }

            public Optional<XamlMember> LookupContentPropertyResult { get; set; }
            protected override XamlMember LookupContentProperty()
            {
                return LookupContentPropertyResult.Or(base.LookupContentProperty);
            }

            public Optional<IList<XamlType>> LookupContentWrappersResult { get; set; }
            protected override IList<XamlType> LookupContentWrappers()
            {
                return LookupContentWrappersResult.Or(base.LookupContentWrappers);
            }

            public Optional<XamlValueConverter<XamlDeferringLoader>> LookupDeferringLoaderResult { get; set; }
            protected override XamlValueConverter<XamlDeferringLoader> LookupDeferringLoader()
            {
                return LookupDeferringLoaderResult.Or(base.LookupDeferringLoader);
            }

            public Optional<XamlTypeInvoker> LookupInvokerResult { get; set; }
            protected override XamlTypeInvoker LookupInvoker()
            {
                return LookupInvokerResult.Or(base.LookupInvoker);
            }

            public Optional<bool> LookupIsAmbientResult { get; set; }
            protected override bool LookupIsAmbient()
            {
                return LookupIsAmbientResult.Or(base.LookupIsAmbient);
            }

            public Optional<bool> LookupIsConstructibleResult { get; set; }
            protected override bool LookupIsConstructible()
            {
                return LookupIsConstructibleResult.Or(base.LookupIsConstructible);
            }

            public Optional<bool> LookupIsMarkupExtensionResult { get; set; }
            protected override bool LookupIsMarkupExtension()
            {
                return LookupIsMarkupExtensionResult.Or(base.LookupIsMarkupExtension);
            }

            public Optional<bool> LookupIsNameScopeResult { get; set; }
            protected override bool LookupIsNameScope()
            {
                return LookupIsNameScopeResult.Or(base.LookupIsNameScope);
            }

            public Optional<bool> LookupIsNullableResult { get; set; }
            protected override bool LookupIsNullable()
            {
                return LookupIsNullableResult.Or(base.LookupIsNullable);
            }

            public Optional<bool> LookupIsPublicResult { get; set; }
            protected override bool LookupIsPublic()
            {
                return LookupIsPublicResult.Or(base.LookupIsPublic);
            }

            public Optional<bool> LookupIsUnknownResult { get; set; }
            protected override bool LookupIsUnknown()
            {
                return LookupIsUnknownResult.Or(base.LookupIsUnknown);
            }

            public Optional<bool> LookupIsWhitespaceSignificantCollectionResult { get; set; }
            protected override bool LookupIsWhitespaceSignificantCollection()
            {
                return LookupIsWhitespaceSignificantCollectionResult.Or(base.LookupIsWhitespaceSignificantCollection);
            }

            public Optional<bool> LookupIsXDataResult { get; set; }
            protected override bool LookupIsXData()
            {
                return LookupIsXDataResult.Or(base.LookupIsXData);
            }

            public Optional<XamlType> LookupItemTypeResult { get; set; }
            protected override XamlType LookupItemType()
            {
                return LookupItemTypeResult.Or(base.LookupItemType);
            }

            public Optional<XamlType> LookupKeyTypeResult { get; set; }
            protected override XamlType LookupKeyType()
            {
                return LookupKeyTypeResult.Or(base.LookupKeyType);
            }

            public Optional<XamlType> LookupMarkupExtensionReturnTypeResult { get; set; }
            protected override XamlType LookupMarkupExtensionReturnType()
            {
                return LookupMarkupExtensionReturnTypeResult.Or(base.LookupMarkupExtensionReturnType);
            }

            public Optional<XamlMember> LookupMemberResult { get; set; }
            protected override XamlMember LookupMember(string name, bool skipReadOnlyCheck)
            {
                return LookupMemberResult.Or(base.LookupMember, name, skipReadOnlyCheck);
            }

            public Optional<IList<XamlType>> LookupPositionalParametersResult { get; set; }
            protected override IList<XamlType> LookupPositionalParameters(int parameterCount)
            {
                return LookupPositionalParametersResult.Or(base.LookupPositionalParameters, parameterCount);
            }

            public Optional<EventHandler<XamlSetMarkupExtensionEventArgs>> LookupSetMarkupExtensionHandlerResult { get; set;}
            protected override EventHandler<XamlSetMarkupExtensionEventArgs> LookupSetMarkupExtensionHandler()
            {
                return LookupSetMarkupExtensionHandlerResult.Or(base.LookupSetMarkupExtensionHandler);
            }

            public Optional<EventHandler<XamlSetTypeConverterEventArgs>> LookupSetTypeConverterHandlerResult { get; set; }
            protected override EventHandler<XamlSetTypeConverterEventArgs> LookupSetTypeConverterHandler()
            {
                return LookupSetTypeConverterHandlerResult.Or(base.LookupSetTypeConverterHandler);
            }

            public Optional<bool> LookupTrimSurroundingWhitespaceResult { get; set; }
            protected override bool LookupTrimSurroundingWhitespace()
            {
                return LookupTrimSurroundingWhitespaceResult.Or(base.LookupTrimSurroundingWhitespace);
            }

            public Optional<XamlValueConverter<TypeConverter>> LookupTypeConverterResult { get; set; }
            protected override XamlValueConverter<TypeConverter> LookupTypeConverter()
            {
                return LookupTypeConverterResult.Or(base.LookupTypeConverter);
            }

            public Optional<Type> LookupUnderlyingTypeResult { get; set; }
            protected override Type LookupUnderlyingType()
            {
                return LookupUnderlyingTypeResult.Or(base.LookupUnderlyingType);
            }

            public Optional<bool> LookupUsableDuringInitializationResult { get; set; }
            protected override bool LookupUsableDuringInitialization()
            {
                return LookupUsableDuringInitializationResult.Or(base.LookupUsableDuringInitialization);
            }

            public Optional<XamlValueConverter<ValueSerializer>> LookupValueSerializerResult { get; set; }
            protected override XamlValueConverter<ValueSerializer> LookupValueSerializer()
            {
                return LookupValueSerializerResult.Or(base.LookupValueSerializer);
            }

            public Optional<IList<string>> GetXamlNamespacesResult { get; set; }
            public override IList<string> GetXamlNamespaces()
            {
                return GetXamlNamespacesResult.Or(base.GetXamlNamespaces);
            }
        }

        private class CustomType : TypeDelegator
        {
            public CustomType(Type delegatingType) : base(delegatingType)
            {
            }

            public Optional<Assembly> AssemblyResult { get; set; }
            public override Assembly Assembly => AssemblyResult.Or(base.Assembly);

            public Optional<IList<CustomAttributeData>> GetCustomAttributesDataResult { get; set; }
            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                return GetCustomAttributesDataResult.Or(typeImpl.GetCustomAttributesData);
            }

            public Optional<Type> DeclaringTypeResult { get; set; }
            public override Type DeclaringType => DeclaringTypeResult.Or(typeImpl.DeclaringType);

            public Optional<EventInfo[]> GetEventsResult { get; set; }
            public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            {
                return GetEventsResult.Or(typeImpl.GetEvents, bindingAttr);
            }

            public Optional<Type[]> GetGenericParameterConstraintsResult { get; set; }
            public override Type[] GetGenericParameterConstraints()
            {
                return GetGenericParameterConstraintsResult.Or(typeImpl.GetGenericParameterConstraints);
            }

            public Optional<Type[]> GetInterfacesResult { get; set; }
            public override Type[] GetInterfaces()
            {
                return GetInterfacesResult.Or(typeImpl.GetInterfaces);
            }

            public Optional<PropertyInfo[]> GetPropertiesResult { get; set; }
            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            {
                return GetPropertiesResult.Or(typeImpl.GetProperties, bindingAttr);
            }

            public Optional<bool> IsGenericParameterResult { get; set; }
            public override bool IsGenericParameter => IsGenericParameterResult.Or(typeImpl.IsGenericParameter);
        }

        private class ReflectionOnlyType : CustomType
        {
            public ReflectionOnlyType(Type delegatingType) : base(delegatingType)
            {
                AssemblyResult = new CustomAssembly(base.Assembly)
                {
                    ReflectionOnlyResult = true
                };
            }

            public CustomAssembly AssemblyDelegator
            {
                get => AssemblyResult.HasValue ? AssemblyResult.Value as CustomAssembly : null;
                set => AssemblyResult = value;
            }
        }

        private class ReflectionOnlyCustomAttributeDataType : CustomType
        {
            public ReflectionOnlyCustomAttributeDataType(Type delegatingType) : base(delegatingType)
            {
            }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                IList<CustomAttributeData> baseData = typeImpl.GetCustomAttributesData();
                return baseData.Select(c =>
                {
                    return new SubCustomAttributeData
                    {
                        ConstructorResult = new CustomConstructorInfo(c.Constructor)
                        {
                            DeclaringTypeResult = new ReflectionOnlyType(c.Constructor.DeclaringType)
                        },
                        ConstructorArgumentsResult = c.ConstructorArguments
                    };
                }).ToArray();
            }
        }

        private class ThrowsCustomAttributeFormatExceptionDelegator : TypeDelegator
        {
            public ThrowsCustomAttributeFormatExceptionDelegator(Type delegatingType) : base(delegatingType)
            {
            }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                throw new CustomAttributeFormatException();
            }
        }
    }
}
