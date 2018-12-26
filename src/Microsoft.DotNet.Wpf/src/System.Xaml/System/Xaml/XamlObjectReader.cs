// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Xaml.MS.Impl;
using System.Xaml.Schema;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Markup;
using MS.Internal.Xaml.Runtime;

namespace System.Xaml
{
    public class XamlObjectReader : XamlReader
    {
        private readonly XamlObjectReaderSettings _settings;
        private readonly XamlSchemaContext _schemaContext;
        private XamlNode _currentXamlNode;
        private object _currentInstance;
        private readonly Stack<MarkupInfo> _nodes;

        public XamlObjectReader(object instance)
            : this(instance, (XamlObjectReaderSettings)null)
        {
        }

        public XamlObjectReader(object instance, XamlObjectReaderSettings settings)
            : this(instance, new XamlSchemaContext(), settings)
        {
        }

        public XamlObjectReader(object instance, XamlSchemaContext schemaContext)
            : this(instance, schemaContext, null)
        {
        }

        public XamlObjectReader(object instance, XamlSchemaContext schemaContext, XamlObjectReaderSettings settings)
        {
            _schemaContext = schemaContext ?? throw new ArgumentNullException(nameof(schemaContext));
            _settings = settings ?? new XamlObjectReaderSettings();
            _nodes = new Stack<MarkupInfo>();
            _currentXamlNode = new XamlNode(XamlNode.InternalNodeType.StartOfStream);

            var context = new SerializerContext(schemaContext, _settings) { RootType = instance?.GetType() };

            ObjectMarkupInfo rootObject = ObjectMarkupInfo.ForObject(instance, context, null, true);

            // While we still have name scopes to do, do them. We postpone name scopes so that we can appropriately
            // resolve references from inside a namescope to objects outside of the name scope (which may also occur
            // later in the XAML).
            while (context.PendingNameScopes.Count > 0)
            {
                var recordInfo = context.PendingNameScopes.Dequeue();
                recordInfo.Resume(context);
            }

            // Now make another pass and make sure that all the records that have references to them also have
            // names. This will also call lookup prefix while recursing to ensure that all the namespaces are
            // written at the top of the document.
            //
            // While we recurse assigning names we will also check that there aren't duplicate names visible within
            // a given name scope.
            //
            Stack<HashSet<string>> namesInCurrentScope = new Stack<HashSet<string>>();
            namesInCurrentScope.Push(new HashSet<string>());
            rootObject.EnsureNoDuplicateNames(namesInCurrentScope);

            // identify all namespaces, recursively
            rootObject.FindNamespace(context);

            _nodes.Push(rootObject);
            // elevate all namespaces to the top of the stack
            List<XamlNode> namespaceNodes = context.GetSortedNamespaceNodes();
            foreach (var node in namespaceNodes)
            {
                _nodes.Push(new NamespaceMarkupInfo() { XamlNode = node });
            }
        }

        public override bool Read()
        {
            if (_nodes.Count == 0)
            {
                if (_currentXamlNode.NodeType != XamlNodeType.None)
                {
                    _currentXamlNode = new XamlNode(XamlNode.InternalNodeType.EndOfStream);
                }
                return false;
            }

            MarkupInfo node = _nodes.Pop();
            _currentXamlNode = node.XamlNode;

            ObjectMarkupInfo objectNode = node as ObjectMarkupInfo;
            _currentInstance = objectNode != null ? objectNode.Object : null;

            List<MarkupInfo> subNodes = node.Decompose();
            if (subNodes != null)
            {
                // we want to push the nodes onto the stack in reverse order
                subNodes.Reverse();
                foreach (var subNode in subNodes)
                {
                    _nodes.Push(subNode);
                }
            }

            return true;
        }

        public override XamlNodeType NodeType => _currentXamlNode.NodeType;

        public override NamespaceDeclaration Namespace => _currentXamlNode.NamespaceDeclaration;

        public override XamlType Type => _currentXamlNode.XamlType;

        public override XamlMember Member => _currentXamlNode.Member;

        public override object Value => _currentXamlNode.Value;

        public override XamlSchemaContext SchemaContext => _schemaContext;

        public override bool IsEof => _currentXamlNode.IsEof;

        public virtual object Instance
        {
            get => _currentXamlNode.NodeType == XamlNodeType.StartObject ? this._currentInstance : null;
        }

        internal static DesignerSerializationVisibility GetSerializationVisibility(XamlMember member)
        {
            XamlMember result = XamlMemberExtensions.GetNearestMember(member,
                XamlMemberExtensions.GetNearestBaseMemberCriterion.HasSerializationVisibility);
            return result.SerializationVisibility;
        }

        internal static string GetConstructorArgument(XamlMember member)
        {
            XamlMember result = XamlMemberExtensions.GetNearestMember(member,
                XamlMemberExtensions.GetNearestBaseMemberCriterion.HasConstructorArgument);
            return result.ConstructorArgument;
        }

        internal static bool GetDefaultValue(XamlMember member, out object value)
        {
            XamlMember result = XamlMemberExtensions.GetNearestMember(member,
                XamlMemberExtensions.GetNearestBaseMemberCriterion.HasDefaultValue);
            if (result.HasDefaultValue)
            {
                value = result.DefaultValue;
                return true;
            }
            value = null;
            return false;
        }

        private class NameScopeMarkupInfo : ObjectMarkupInfo
        {
            public ReferenceTable ParentTable { get; set; }
            public object SourceObject { get; set; }

            public void Resume(SerializerContext context)
            {
                context.ReferenceTable = new ReferenceTable(ParentTable);
                AddRecordMembers(SourceObject, context);
            }

            public override void EnsureNoDuplicateNames(Stack<HashSet<string>> namesInCurrentScope)
            {
                namesInCurrentScope.Push(new HashSet<string>());
                base.EnsureNoDuplicateNames(namesInCurrentScope);
                namesInCurrentScope.Pop();
            }
        }

        private class ObjectReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);

            public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
        }

        private class ValueMarkupInfo : ObjectOrValueMarkupInfo
        {
        }

        private class MemberMarkupInfo : MarkupInfo
        {
            private readonly List<MarkupInfo> _children = new List<MarkupInfo>();

            public bool IsContent { get; set; }
            public bool IsFactoryMethod { get; set; }
            public List<MarkupInfo> Children => _children;

            public override List<MarkupInfo> Decompose()
            {
                _children.Add(EndMemberMarkupInfo.Instance);
                return _children;
            }

            public bool IsAtomic => _children.Count == 1 && _children[0] is ValueMarkupInfo;

            public bool IsAttributableMarkupExtension
            {
                get
                {
                    if (_children.Count != 1)
                    {
                        return false;
                    }

                    return _children[0] is ObjectMarkupInfo r && r.IsAttributableMarkupExtension;
                }
            }

            public bool IsAttributable
            {
                get
                {
                    // Constructor arguments property
                    if (XamlNode.Member == XamlLanguage.PositionalParameters)
                    {
                        foreach (MarkupInfo child in _children)
                        {
                            Debug.Assert(!(child is ObjectMarkupInfo objectInfo && !objectInfo.IsAttributableMarkupExtension));
                        }
                        return true;
                    }

                    // Non-empty Collections are not attributable
                    if (Children.Count > 1)
                    {
                        return false;
                    }

                    // Empty collections and atoms are attributable
                    if (Children.Count == 0 || Children[0] is ValueMarkupInfo)
                    {
                        return true;
                    }

                    if (!(Children[0] is ObjectMarkupInfo r))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.ExpectedObjectMarkupInfo));
                    }

                    return r.IsAttributableMarkupExtension;
                }
            }

            public override void FindNamespace(SerializerContext context)
            {
                XamlMember member = XamlNode.Member;
                if (MemberRequiresNamespaceHoisting(member))
                {
                    context.FindPrefix(member.PreferredXamlNamespace);
                }

                foreach (MarkupInfo ov in Children)
                {
                    ov.FindNamespace(context);
                }
            }

            private bool MemberRequiresNamespaceHoisting(XamlMember member)
            {
                return (member.IsAttachable || (member.IsDirective && !XamlXmlWriter.IsImplicit(member)))
                    && (member.PreferredXamlNamespace != XamlLanguage.Xml1998Namespace);
            }

            public static XamlTemplateMarkupInfo ConvertToXamlReader(
                object propertyValue, XamlValueConverter<XamlDeferringLoader> deferringLoader, SerializerContext context)
            {
                XamlDeferringLoader converter = deferringLoader.ConverterInstance;
                if (converter == null)
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.DeferringLoaderInstanceNull, deferringLoader));
                }

                context.Instance = propertyValue;
                XamlReader reader = context.Runtime.DeferredSave(context.TypeDescriptorContext, deferringLoader, propertyValue);
                context.Instance = null;
                using (reader)
                {
                    return new XamlTemplateMarkupInfo(reader, context);
                }
            }

            public static MemberMarkupInfo ForAttachedProperty
                (object source, XamlMember attachedProperty, object value, SerializerContext context)
            {
                if (GetSerializationVisibility(attachedProperty) == DesignerSerializationVisibility.Hidden)
                {
                    return null;
                }

                if (ShouldWriteProperty(source, attachedProperty, context))
                {
                    if (context.IsPropertyWriteVisible(attachedProperty))
                    {
                        return new MemberMarkupInfo
                        {
                            XamlNode = new XamlNode(XamlNodeType.StartMember, attachedProperty),
                            Children = { GetPropertyValueInfo(value, attachedProperty, context) }
                        };
                    }

                    if (attachedProperty.Type.IsDictionary)
                    {
                        return ForDictionary(value, attachedProperty, context, true);
                    }

                    if (attachedProperty.Type.IsCollection)
                    {
                        return ForSequence(value, attachedProperty, context, true);
                    }
                }
                return null;
            }

            // These take either the source object or the value, depending on whether this is actually a property
            // or is an implied content property for some list somewhere. The 'sourceOrValue' parameter is the source
            // when the 'property' parameter is non-null, and the value otherwise.
            public static MemberMarkupInfo ForDictionaryItems(
                object sourceOrValue, XamlMember property, XamlType propertyType, SerializerContext context)
            {
                object propertyValue;

                if (property != null)
                {
                    propertyValue = context.Runtime.GetValue(sourceOrValue, property);
                    if (propertyValue == null)
                    {
                        return null;
                    }
                }
                else
                {
                    propertyValue = sourceOrValue;
                }

                XamlType keyType = propertyType.KeyType;
                MemberMarkupInfo itemsInfo = new MemberMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items) };
                foreach (var entry in context.Runtime.GetDictionaryItems(propertyValue, propertyType))
                {
                    ObjectMarkupInfo objInfo = ObjectMarkupInfo.ForObject(entry.Value, context);

                    ObjectOrValueMarkupInfo keyValue;
                    XamlType actualKeyType = null;
                    if (entry.Key != null)
                    {
                        actualKeyType = context.GetXamlType(entry.Key.GetType());
                    }

                    if (entry.Key != null && actualKeyType != keyType)
                    {
                        TypeConverter tc = TypeConverterExtensions.GetConverterInstance(actualKeyType.TypeConverter);
                        keyValue = ObjectMarkupInfo.ForObject(entry.Key, context, tc);
                    }
                    else
                    {
                        ValueSerializer vs = TypeConverterExtensions.GetConverterInstance(keyType.ValueSerializer);
                        TypeConverter tc = TypeConverterExtensions.GetConverterInstance(keyType.TypeConverter);
                        keyValue = GetPropertyValueInfoInternal(entry.Key, vs, tc, false, null, context);
                    }

                    if (!ShouldOmitKey(entry, context))
                    {
                        objInfo.Properties.Insert(0, new MemberMarkupInfo()
                        {
                            XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Key),
                            Children = { keyValue }
                        });
                    }

                    itemsInfo.Children.Add(objInfo);
                }

                return itemsInfo;
            }

            public static bool ShouldOmitKey(DictionaryEntry entry, SerializerContext context)
            {
                if (entry.Value != null)
                {
                    XamlType typeOfValue = context.GetXamlType(entry.Value.GetType());
                    XamlMember dkp = typeOfValue.GetAliasedProperty(XamlLanguage.Key);

                    if (dkp != null)
                    {
                        if (ObjectMarkupInfo.CanPropertyXamlRoundtrip(dkp, context))
                        {
                            object dkpObject = context.Runtime.GetValue(entry.Value, dkp);

                            if (dkpObject == null)
                            {
                                return entry.Key == null;
                            }
                            else if (dkpObject.Equals(entry.Key))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            public static MemberMarkupInfo ForProperty(
                object source, XamlMember property, SerializerContext context)
            {
                if (ShouldWriteProperty(source, property, context))
                {
                    if (context.IsPropertyWriteVisible(property))
                    {
                        return ForReadWriteProperty(source, property, context);
                    }

                    if (property.Type.IsXData)
                    {
                        return ForXmlSerializable(source, property, context);
                    }

                    // @NOTE: Order is important here because dictionaries can are also seen as
                    //  as collections by Xasl, but we would prefer to serialize them as dictionaries.
                    if (property.Type.IsDictionary)
                    {
                        return ForDictionary(source, property, context, false);
                    }

                    if (property.Type.IsCollection)
                    {
                        return ForSequence(source, property, context, false);
                    }
                }

                return null;
            }

            static MemberMarkupInfo ForSequence(object source, XamlMember property, SerializerContext context, bool isAttachable)
            {
                MemberMarkupInfo itemsInfo = ForSequenceItems(source, isAttachable ? null : property, property.Type, context, allowReadOnly: false);
                if (itemsInfo != null && itemsInfo.Children.Count != 0)
                {
                    return new MemberMarkupInfo
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, property),
                        Children =
                        {
                            new ObjectMarkupInfo
                            {
                                XamlNode = new XamlNode(XamlNodeType.GetObject),
                                //Scope = context.ReferenceTable,
                                Properties =
                                {
                                   itemsInfo
                                }
                            }
                        }
                    };
                }
                else
                {
                    return null;
                }
            }

            static MemberMarkupInfo ForDictionary(object source, XamlMember property, SerializerContext context, bool isAttachable)
            {
                var itemsInfo = ForDictionaryItems(source, isAttachable ? null : property, property.Type, context);
                if (itemsInfo != null && itemsInfo.Children.Count != 0)
                {
                    return new MemberMarkupInfo
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, property),
                        Children =
                        {
                            new ObjectMarkupInfo
                            {
                                XamlNode = new XamlNode(XamlNodeType.GetObject),
                                Properties =
                                {
                                   itemsInfo
                                }
                            }
                        }
                    };
                }
                else
                {
                    return null;
                }
            }

            static MemberMarkupInfo ForXmlSerializable(object source, XamlMember property, SerializerContext context)
            {
                var serializer = (IXmlSerializable)context.Runtime.GetValue(source, property);
                if (serializer == null) { return null; }

                var sb = new StringBuilder();
                var writerSettings = new XmlWriterSettings
                {
                    ConformanceLevel = ConformanceLevel.Auto,
                    Indent = true,
                    OmitXmlDeclaration = true,
                };
                using (XmlWriter writer = XmlWriter.Create(sb, writerSettings))
                {
                    //writer.WriteStartElement(property.Type.Name, property.Type.PreferredXamlNamespace);
                    serializer.WriteXml(writer);
                    //writer.WriteEndElement();
                }

                if (sb.Length > 0)
                {
                    return new MemberMarkupInfo()
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, property),
                        Children =
                        {
                            new ObjectMarkupInfo()
                            {
                                XamlNode = new XamlNode(XamlNodeType.StartObject, XamlLanguage.XData),
                                Properties =
                                {
                                    new MemberMarkupInfo()
                                    {
                                        XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.XData.GetMember("Text")),
                                        Children =
                                        {
                                            new ValueMarkupInfo()
                                            {
                                                XamlNode = new XamlNode(XamlNodeType.Value, sb.ToString())
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };
                }

                return null;
            }

            static MemberMarkupInfo ForReadWriteProperty(
                object source, XamlMember xamlProperty, SerializerContext context)
            {
                var propertyValue = context.Runtime.GetValue(source, xamlProperty);
                XamlType declaringType = xamlProperty.DeclaringType;

                MemberMarkupInfo memberInfo;
                if((xamlProperty == declaringType.GetAliasedProperty(XamlLanguage.Lang)) && (propertyValue is string))
                {
                    memberInfo = new MemberMarkupInfo()
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Lang),
                        Children = { GetPropertyValueInfo(propertyValue, xamlProperty, context) }
                    };
                }
                else
                {
                    memberInfo = new MemberMarkupInfo()
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, xamlProperty),
                        Children = { GetPropertyValueInfo(propertyValue, xamlProperty, context) }
                    };
                }

                RemoveObjectNodesForCollectionOrDictionary(memberInfo);

                return memberInfo;
            }

            static void RemoveObjectNodesForCollectionOrDictionary(MemberMarkupInfo memberInfo)
            {
                var memberType = memberInfo.XamlNode.Member.Type;
                if (memberType.IsCollection || memberType.IsDictionary)
                {
                    if (memberInfo.Children.Count == 1)
                    {
                        var objectInfo = memberInfo.Children[0] as ObjectMarkupInfo;
                        if (objectInfo != null && objectInfo.Properties.Count == 1 && memberType == objectInfo.XamlNode.XamlType)
                        {

                            if (objectInfo.Properties[0].XamlNode.Member == XamlLanguage.Items)
                            {
                                var itemsMemberInfo = objectInfo.Properties[0] as MemberMarkupInfo;
                                if(itemsMemberInfo != null && itemsMemberInfo.Children.Count > 0)
                                {
                                    //Check if the first element of the collection/dictionary is a ME and replace the SO with GO only if it is not an ME.
                                    //This is to handle cases where the first element is, say, null. If we remove the SO, then there is no way to
                                    //know if the collection is null or the first element is null.
                                    var itemInfo = itemsMemberInfo.Children[0] as ObjectMarkupInfo;
                                    if(itemInfo == null || itemInfo.XamlNode.XamlType == null || !itemInfo.XamlNode.XamlType.IsMarkupExtension)
                                    {
                                        // change the member to GetObject
                                        objectInfo.XamlNode = new XamlNode(XamlNodeType.GetObject);
                                    }
                                }

                            }
                        }
                    }
                }
            }

            /// <summary>
            /// These take either the source object or the value, depending on whether this is actually a property
            /// or is an implied content property for some list somewhere. The 'sourceOrValue' parameter is the source
            /// when the 'property' parameter is non-null, and the value otherwise.
            /// </summary>
            public static MemberMarkupInfo ForSequenceItems(object sourceOrValue, XamlMember property, XamlType xamlType, SerializerContext context, bool allowReadOnly)
            {
                object propertyValue;

                if (property != null)
                {
                    propertyValue = context.Runtime.GetValue(sourceOrValue, property);
                    if (propertyValue == null)
                    {
                        return null;
                    }
                }
                else
                {
                    propertyValue = sourceOrValue;
                }

                if (!allowReadOnly && xamlType.IsReadOnlyMethod != null)
                {
                    bool isReadOnly = (bool)xamlType.IsReadOnlyMethod.Invoke(propertyValue, null);
                    if (isReadOnly)
                    {
                        return null;
                    }
                }

                var itemsInfo = new MemberMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items) };

                bool isPreviousItemValue = false;
                IList<object> itemsList = context.Runtime.GetCollectionItems(propertyValue, xamlType);
                for(int i = 0; i < itemsList.Count; i++)
                {
                    object itemValue = itemsList[i];
                    ObjectMarkupInfo itemInfo = ObjectMarkupInfo.ForObject(itemValue, context);

                    ObjectOrValueMarkupInfo unwrappedItemInfo = null;

                    if (xamlType.ContentWrappers != null)
                    {
                        if (itemInfo.Properties.Count == 1)
                        {
                            MemberMarkupInfo memberInfo = (MemberMarkupInfo)itemInfo.Properties[0];
                            if (memberInfo.XamlNode.Member == itemInfo.XamlNode.XamlType.ContentProperty)
                            {
                                foreach (XamlType contentWrapperType in xamlType.ContentWrappers)
                                {
                                    if (contentWrapperType == itemInfo.XamlNode.XamlType)
                                    {
                                        if (memberInfo.Children.Count == 1)
                                        {
                                            ObjectOrValueMarkupInfo childOvInfo = (ObjectOrValueMarkupInfo)memberInfo.Children[0];
                                            if (childOvInfo is ValueMarkupInfo)
                                            {
                                                // if the unwrapped object is a string, we need to make sure
                                                // that its previous cousin is not a string and the string does
                                                // not have significant whitespaces before unwrapping

                                                bool isFirstElementOfCollection = (i == 0);
                                                bool isLastElementOfCollection = (i == (itemsList.Count - 1));
                                                if (!isPreviousItemValue && !ShouldUnwrapDueToWhitespace((string)childOvInfo.XamlNode.Value, xamlType, isFirstElementOfCollection, isLastElementOfCollection))
                                                {
                                                    unwrappedItemInfo = childOvInfo;
                                                    isPreviousItemValue = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                unwrappedItemInfo = childOvInfo;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!(unwrappedItemInfo is ValueMarkupInfo))
                    {
                        isPreviousItemValue = false;
                    }
                    itemsInfo.Children.Add(unwrappedItemInfo ?? itemInfo);
                }

                return itemsInfo;
            }

            // We not should unwrap if the value has significant whitespace and it is not in a WhitespaceSignificant Collection
            // If it is in a WhitespaceSignificant Collection we should not unwrap only if
            // 1. It has 2 consecutive spaces or non space whitespace (tabs, new line, etc)
            // 2. Last Element has trailing whitespace
            // 3. First element has leading whitespace
            private static bool ShouldUnwrapDueToWhitespace(string value, XamlType xamlType, bool isFirstElementOfCollection, bool isLastElementOfCollection)
            {
                if(XamlXmlWriter.HasSignificantWhitespace(value))
                {
                    if(xamlType.IsWhitespaceSignificantCollection)
                    {
                        if(XamlXmlWriter.ContainsConsecutiveInnerSpaces(value) ||
                           XamlXmlWriter.ContainsWhitespaceThatIsNotSpace(value))
                        {
                            return true;
                        }
                        else if(XamlXmlWriter.ContainsTrailingSpace(value) && isLastElementOfCollection)
                        {
                            return true;
                        }
                        else if(XamlXmlWriter.ContainsLeadingSpace(value) && isFirstElementOfCollection)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            private static ObjectOrValueMarkupInfo GetPropertyValueInfo(
                object propertyValue, XamlMember xamlProperty, SerializerContext context)
            {
                return GetPropertyValueInfoInternal(propertyValue,
                    TypeConverterExtensions.GetConverterInstance(xamlProperty.ValueSerializer),
                    TypeConverterExtensions.GetConverterInstance(xamlProperty.TypeConverter),
                    xamlProperty != null && xamlProperty.DeferringLoader != null,
                    xamlProperty,
                    context);
            }

            private static ObjectOrValueMarkupInfo GetPropertyValueInfoInternal(
                object propertyValue, ValueSerializer propertyValueSerializer, TypeConverter propertyConverter, bool isXamlTemplate, XamlMember xamlProperty, SerializerContext context)
            {
                ObjectOrValueMarkupInfo valueInfo = null;

                context.Instance = propertyValue;
                if (isXamlTemplate && propertyValue != null)
                {
                    valueInfo = ConvertToXamlReader(propertyValue, xamlProperty.DeferringLoader, context);
                }
                else
                {
                    if (context.TryValueSerializeToString(propertyValueSerializer, propertyConverter, context, ref propertyValue))
                    {
                        ThrowIfPropertiesAreAttached(context.Instance, xamlProperty, context);
                        context.Instance = null;

                        valueInfo = new ValueMarkupInfo() { XamlNode = new XamlNode(XamlNodeType.Value, propertyValue) };
                    }
                    else if (propertyConverter != null && context.TryConvertToMarkupExtension(propertyConverter, ref propertyValue))
                    {
                        context.Instance = null;
                        valueInfo = ObjectMarkupInfo.ForObject(propertyValue, context);
                    }
                    else if (propertyConverter != null && context.TryTypeConvertToString(propertyConverter, ref propertyValue))
                    {
                        ThrowIfPropertiesAreAttached(context.Instance, xamlProperty, context);
                        context.Instance = null;

                        // duplicate 3.0 behavior by replacing null value with String.Empty
                        valueInfo = new ValueMarkupInfo() { XamlNode = new XamlNode(XamlNodeType.Value, propertyValue ?? string.Empty) };
                    }
                    else if (propertyValue is string)
                    {
                        ThrowIfPropertiesAreAttached(propertyValue, xamlProperty, context);
                        context.Instance = null;

                        valueInfo = new ValueMarkupInfo() { XamlNode = new XamlNode(XamlNodeType.Value, propertyValue) };
                    }
                    else
                    {
                        context.Instance = null;
                        valueInfo = ObjectMarkupInfo.ForObject(propertyValue, context, propertyConverter);
                    }
                }
                return valueInfo;
            }

            private static void ThrowIfPropertiesAreAttached(object value, XamlMember property, SerializerContext context)
            {
                var props = context.Runtime.GetAttachedProperties(value);
                if (props != null)
                {
                    if (property != null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.AttachedPropertyOnTypeConvertedOrStringProperty, property.Name, value.ToString(), props[0].Key.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Get(SRID.AttachedPropertyOnDictionaryKey, value.ToString(), props[0].Key.ToString()));
                    }
                }
            }

            // Reproduce the logic of System.ComponentModel.ReflectPropertyDescriptor.ShouldSerializeValue
            private static bool ShouldWriteProperty(object source, XamlMember property, SerializerContext context)
            {
                bool isReadOnly = !context.IsPropertyWriteVisible(property);

                if (!isReadOnly)
                {
                    object defaultValue;
                    if (GetDefaultValue(property, out defaultValue))
                    {
                        object actualValue = context.Runtime.GetValue(source, property);
                        return !Equals(defaultValue, actualValue);
                    }
                }

                ShouldSerializeResult shouldSerialize = context.Runtime.ShouldSerialize(property, source);
                if (shouldSerialize != ShouldSerializeResult.Default)
                {
                    return shouldSerialize == ShouldSerializeResult.True ? true : false;
                }

                if (!isReadOnly)
                {
                    // Read-write properties are serialized by default
                    return true;
                }
                else
                {
                    // One departure from the behavior of ReflectPropertyDescriptor: By default it requires
                    // that read-only properties must be attributed with DesignerSerializationVisibility.Content
                    // to visible. Unless RequireExplicitContentVisibility is set to true, we don't require that
                    // for readonly collection/dictionary/xdata.
                    return !context.Settings.RequireExplicitContentVisibility || 
                           GetSerializationVisibility(property) == DesignerSerializationVisibility.Content;
                }
            }
        }

        private abstract class MarkupInfo
        {
            public XamlNode XamlNode { get; set; }
            public virtual void FindNamespace(SerializerContext context) { }
            public virtual List<MarkupInfo> Decompose() => null;
        }

        private class EndObjectMarkupInfo : MarkupInfo
        {
            private static EndObjectMarkupInfo s_instance = new EndObjectMarkupInfo();

            private EndObjectMarkupInfo()
            {
                XamlNode = new XamlNode(XamlNodeType.EndObject);
            }

            public static EndObjectMarkupInfo Instance  => s_instance;
        }

        private class EndMemberMarkupInfo : MarkupInfo
        {
            private static EndMemberMarkupInfo s_instance = new EndMemberMarkupInfo();

            private EndMemberMarkupInfo()
            {
                XamlNode = new XamlNode(XamlNodeType.EndMember);
            }

            public static EndMemberMarkupInfo Instance => s_instance;
        }

        private class NamespaceMarkupInfo : MarkupInfo
        {
        }

        private abstract class ObjectOrValueMarkupInfo : MarkupInfo
        {
            public virtual void EnsureNoDuplicateNames(Stack<HashSet<string>> namesInCurrentScope) { }
        }

        private class ObjectMarkupInfo : ObjectOrValueMarkupInfo
        {
            private readonly List<MarkupInfo> _properties = new List<MarkupInfo>();
            private bool? isAttributableMarkupExtension = null;

            public List<MarkupInfo> Properties => _properties;
            public string Name { get; set; }
            public object Object { get; set; }

            public override List<MarkupInfo> Decompose()
            {
                SortProperties();
                Properties.Add(EndObjectMarkupInfo.Instance);
                return _properties;
            }

            private void SortProperties()
            {
                if (IsAttributableMarkupExtension)
                {
                    Properties.Sort(PropertySorterForCurlySyntax.Instance);
                }
                else
                {
                    Properties.Sort(PropertySorterForXmlSyntax.Instance);
                }

                ReorderPropertiesWithDO();
            }

            private void ReorderPropertiesWithDO()
            {
                List<MarkupInfo> removedProperties;
                SelectAndRemovePropertiesWithDO(out removedProperties);

                if (removedProperties != null)
                {
                    InsertPropertiesWithDO(removedProperties);
                }
            }

            private void InsertPropertiesWithDO(List<MarkupInfo> propertiesWithDO)
            {
                int posOfFirstNonAttributableProperty;
                HashSet<string> namesOfAttributableProperties = FindAllAttributableProperties(out posOfFirstNonAttributableProperty);

                foreach (MemberMarkupInfo memberInfo in propertiesWithDO)
                {
                    if (IsMemberOnlyDependentOnAttributableMembers(memberInfo.XamlNode.Member, namesOfAttributableProperties))
                    {
                        if (memberInfo.IsAtomic || memberInfo.IsAttributableMarkupExtension)
                        {
                            _properties.Insert(posOfFirstNonAttributableProperty, memberInfo);
                            namesOfAttributableProperties.Add(memberInfo.XamlNode.Member.Name);
                            posOfFirstNonAttributableProperty++;
                            continue;
                        }
                    }

                    Properties.Add(memberInfo);
                }
            }

            private bool IsMemberOnlyDependentOnAttributableMembers(XamlMember member, HashSet<string> namesOfAttributableProperties)
            {
                foreach (XamlMember dependingProperty in member.DependsOn)
                {
                    if (!namesOfAttributableProperties.Contains(dependingProperty.Name))
                    {
                        return false;
                    }
                }
                return true;
            }

            private HashSet<string> FindAllAttributableProperties(out int posOfFirstNonAttributableProperty)
            {
                int i;
                HashSet<string> namesOfAttributableProperties = new HashSet<string>();
                for (i = 0; i < Properties.Count; i++)
                {
                    MemberMarkupInfo memberInfo = (MemberMarkupInfo)Properties[i];
                    if (!memberInfo.IsAtomic && !memberInfo.IsAttributableMarkupExtension)
                    {
                        break;
                    }
                    namesOfAttributableProperties.Add(memberInfo.XamlNode.Member.Name);
                }
                posOfFirstNonAttributableProperty = i;
                return namesOfAttributableProperties;
            }

            private void SelectAndRemovePropertiesWithDO(out List<MarkupInfo> removedProperties)
            {
                removedProperties = null;
                PartiallyOrderedList<string, MarkupInfo> propertiesWithDO = null;

                for (int i = 0; i < _properties.Count; )
                {
                    MarkupInfo property = _properties[i];
                    if (property.XamlNode.Member.DependsOn.Count > 0)
                    {
                        if (propertiesWithDO == null)
                        {
                            propertiesWithDO = new PartiallyOrderedList<string, MarkupInfo>();
                        }
                        string dependentPropertyName = property.XamlNode.Member.Name;

                        propertiesWithDO.Add(dependentPropertyName, property);
                        foreach (XamlMember dependingProperty in property.XamlNode.Member.DependsOn)
                        {
                            propertiesWithDO.SetOrder(dependingProperty.Name, dependentPropertyName);
                        }

                        _properties.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                if (propertiesWithDO == null)
                {
                    return;
                }

                removedProperties = new List<MarkupInfo>(propertiesWithDO);
                return;

            }

            public virtual bool IsAttributableMarkupExtension
            {
                get
                {
                    if (isAttributableMarkupExtension.HasValue)
                    {
                        return isAttributableMarkupExtension.Value;
                    }

                    if ((XamlNode.NodeType == XamlNodeType.StartObject && !XamlNode.XamlType.IsMarkupExtension)
                        || XamlNode.NodeType == XamlNodeType.GetObject)
                    {
                        isAttributableMarkupExtension = false;
                        return false;
                    }

                    //check if every property is attributable
                    foreach (var property in Properties)
                    {
                        if (!((MemberMarkupInfo)property).IsAttributable)
                        {
                            isAttributableMarkupExtension = false;
                            return false;
                        }
                    }

                    isAttributableMarkupExtension = true;
                    return true;
                }
            }

            public override void  FindNamespace(SerializerContext context)
            {
                if (XamlNode.NodeType == XamlNodeType.StartObject)
                {
                    context.FindPrefix(this.XamlNode.XamlType.PreferredXamlNamespace);

                    var type = XamlNode.XamlType;
                    if (type.IsGeneric)
                    {
                        context.FindPrefix(XamlLanguage.TypeArguments.PreferredXamlNamespace);
                        FindNamespaceForTypeArguments(type.TypeArguments, context);
                    }
                }

                foreach (var property in Properties)
                {
                    property.FindNamespace(context);
                }
            }

            private void FindNamespaceForTypeArguments(IList<XamlType> types, SerializerContext context)
            {
                if (types == null || types.Count == 0)
                {
                    return;
                }

                foreach (var type in types)
                {
                    context.FindPrefix(type.PreferredXamlNamespace);
                    FindNamespaceForTypeArguments(type.TypeArguments, context);
                }
            }

            private void AddItemsProperty(object value, SerializerContext context, XamlType xamlType)
            {
                MemberMarkupInfo propertyInfo = null;

                if (xamlType.IsDictionary)
                {
                    propertyInfo = MemberMarkupInfo.ForDictionaryItems(value, null, xamlType, context);
                }
                else if (xamlType.IsCollection)
                {
                    propertyInfo = MemberMarkupInfo.ForSequenceItems(value, null, xamlType, context, allowReadOnly: true);
                }

                if (propertyInfo != null && propertyInfo.Children.Count != 0)
                {
                    _properties.Add(propertyInfo);
                }
            }

            private ParameterInfo[] GetMethodParams(MemberInfo memberInfo)
            {
                if (!(memberInfo is MethodBase method))
                {
                    return null;
                }

                return method.GetParameters();
            }

            private void AddFactoryMethodAndValidateArguments(Type valueType, MemberInfo memberInfo, ICollection arguments, SerializerContext context, out ParameterInfo[] methodParams)
            {
                methodParams = null;

                if (memberInfo == null)
                {
                    // default ctor
                    methodParams = new ParameterInfo[0];
                }
                else if (memberInfo is ConstructorInfo ctor)
                {
                    methodParams = ctor.GetParameters();
                }
                else if (memberInfo is MethodInfo mi)
                {
                    methodParams = mi.GetParameters();

                    var methodName = memberInfo.Name;
                    var declaringType = memberInfo.DeclaringType;
                    if (declaringType != valueType)
                    {
                        methodName = ConvertTypeAndMethodToString(declaringType, methodName, context);
                    }

                    // add factory method if there is one
                    Properties.Add(new MemberMarkupInfo
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.FactoryMethod),
                        IsFactoryMethod = true,
                        Children = { new ValueMarkupInfo() { XamlNode = new XamlNode(XamlNodeType.Value, methodName) } }
                    });
                }
                else if (valueType.IsValueType)
                {
                    if (arguments == null || arguments.Count == 0)
                    {
                        return;
                    }
                    
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderInstanceDescriptorIncompatibleArguments));
                }
                else
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderInstanceDescriptorInvalidMethod));
                }

                if (arguments != null)
                {
                    if (arguments.Count != methodParams.Length)
                    {
                        throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderInstanceDescriptorIncompatibleArguments));
                    }

                    int argPos = 0;
                    foreach (var argument in arguments)
                    {
                        var parameterInfo = methodParams[argPos++];
                        if (argument == null)
                        {
                            if (parameterInfo.ParameterType.IsValueType)
                            {
                                if (!(parameterInfo.ParameterType.IsGenericType &&
                                    parameterInfo.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                                {
                                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderInstanceDescriptorIncompatibleArgumentTypes, "null", parameterInfo.ParameterType));
                                }
                            }
                        }
                        else if (!parameterInfo.ParameterType.IsAssignableFrom(argument.GetType()))
                        {
                            throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderInstanceDescriptorIncompatibleArgumentTypes, argument.GetType(), parameterInfo.ParameterType));
                        }
                    }
                }
            }

            private void AddArgumentsMembers(ICollection arguments, SerializerContext context)
            {
                if (arguments == null || arguments.Count == 0)
                {
                    return;
                }

                var itemsProperty = new MemberMarkupInfo
                {
                    XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items)
                };
                var argumentsProperty = new MemberMarkupInfo()
                {
                    XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Arguments)
                };

                foreach (var argument in arguments)
                {
                    argumentsProperty.Children.Add(ForObject(argument, context));
                }

                Properties.Add(argumentsProperty);
            }

            private bool TryAddPositionalParameters(XamlType xamlType, MemberInfo member, ICollection arguments, SerializerContext context)
            {
                if (arguments == null || arguments.Count == 0)
                {
                    return false;
                }

                ParameterInfo[] cstrParams = GetMethodParams(member);
                var positionalParametersProperty = new MemberMarkupInfo
                {
                    XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters)
                };

                int i = 0;
                foreach (object argument in arguments)
                {
                    XamlType paramXamlType = context.GetXamlType(cstrParams[i++].ParameterType);

                    ValueSerializer valueSerializer = TypeConverterExtensions.GetConverterInstance(paramXamlType.ValueSerializer);
                    TypeConverter converter = TypeConverterExtensions.GetConverterInstance(paramXamlType.TypeConverter);

                    ObjectMarkupInfo objectInfo = null;
                    object meObject = argument;

                    context.Instance = argument;
                    if (converter != null && valueSerializer != null && context.CanRoundtripUsingValueSerializer(valueSerializer, converter, argument))
                    {
                        //ValueSerializer is always preferred over TypeConverters
                        string stringValue = context.ConvertToString(valueSerializer, argument);
                        context.Instance = null;
                        positionalParametersProperty.Children.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.Value, stringValue) });
                    }
                    else if ((converter != null && context.TryConvertToMarkupExtension(converter, ref meObject)) || meObject is MarkupExtension)
                    {
                        context.Instance = null;
                        objectInfo = ForObject(meObject, context);
                        if (!objectInfo.IsAttributableMarkupExtension)
                        {
                            return false;
                        }
                        positionalParametersProperty.Children.Add(objectInfo);
                    }
                    else if (converter != null && context.CanRoundTripString(converter))
                    {
                        var stringValue = context.ConvertTo<string>(converter, argument);
                        context.Instance = null;
                        positionalParametersProperty.Children.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.Value, stringValue) });
                    }
                    else if (argument is string)
                    {
                        context.Instance = null;
                        positionalParametersProperty.Children.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.Value, argument) });
                    }
                    else
                    {
                        context.Instance = null;
                        return false;
                    }
                }

                Properties.Add(positionalParametersProperty);
                return true;
            }

            protected void AddRecordMembers(object value, SerializerContext context)
            {
                AddRecordMembers(value, context, null);
            }

            private bool TryGetInstanceDescriptorInfo(object value, SerializerContext context, TypeConverter converter, out MemberInfo member, out ICollection arguments, out bool isComplete)
            {
                context.Instance = value;
                if (converter != null && context.CanConvertTo(converter, typeof(InstanceDescriptor)))
                {
                    ConvertToInstanceDescriptor(context, value, converter, out member, out arguments, out isComplete);
                    return true;
                }

                member = null;
                arguments = null;
                isComplete = false;
                return false;
            }

            /// <SecurityNote>
            /// Critical: Accesses members on InstanceDescriptor, which has a LinkDemand
            /// Safe: Protected with a Full Demand
            ///
            /// Note: there's not really anything on InstanceDescriptor itself (other than perhaps
            /// the Invoke method, which we don't use) that needs to be protected; the sensitive
            /// work is actually done in converters that potentially access non-public members to
            /// return the instance data.
            /// However, rather than relying on converters to do the right thing, this is enforced
            /// via a LinkDemand on InstanceDescriptor. So we have no choice but to demand here.
            /// </SecurityNote>
            [SecuritySafeCritical]
            [PermissionSet(SecurityAction.Demand, Unrestricted=true)]
            private void ConvertToInstanceDescriptor(SerializerContext context, object instance, TypeConverter converter,
                out MemberInfo member, out ICollection arguments, out bool isComplete)
            {
                InstanceDescriptor descriptor = context.ConvertTo<InstanceDescriptor>(converter, instance);
                context.Instance = null;

                member = descriptor.MemberInfo;
                arguments = descriptor.Arguments;
                isComplete = descriptor.IsComplete;
            }

            private bool TryGetDefaultConstructorInfo(XamlType type, out MemberInfo member, out ICollection arguments, out bool isComplete)
            {
                arguments = null;
                isComplete = false;
                member = null;
                return type.IsConstructible && !type.ConstructionRequiresArguments;
            }

            protected void AddRecordMembers(object value, SerializerContext context, TypeConverter converter)
            {
                Type valueType = value.GetType();
                XamlType valueXamlType = context.GetXamlType(valueType);

                context.Instance = value;
                if (converter == null || !context.CanConvertTo(converter, typeof(InstanceDescriptor)))
                {
                    context.Instance = null;
                    converter = TypeConverterExtensions.GetConverterInstance(valueXamlType.TypeConverter);
                }

                // Add the appropriate x:Arguments/PositionalParameters/FactoryMethod, if any
                bool isComplete;
                ParameterInfo[] methodParams;
                AddRecordConstructionMembers(value, valueXamlType, context, converter, out isComplete, out methodParams);

                // If the instanceDescriptor or constructor args aren't a complete description of
                // the object then we need to enumerate the properties, but even if it is complete
                // we will need to enumerate the properties if we have a runtimeNamePropertyName
                if (!isComplete ||
                    valueXamlType.GetAliasedProperty(XamlLanguage.Name) != null ||
                    context.Runtime.AttachedPropertyCount(value) > 0)
                {
                    AddRecordMembers(value, context, methodParams, valueXamlType);
                }
            }

            private void AddRecordMembers(object value, SerializerContext context, ParameterInfo[] methodParameters, XamlType xamlType)
            {
                List<XamlMember> propertyList = GetXamlSerializableProperties(xamlType, context);
                foreach (XamlMember property in propertyList)
                {
                    // If this property is explicitly hidden, we're done with it
                    if (GetSerializationVisibility(property) == DesignerSerializationVisibility.Hidden)
                    {
                        continue;
                    }

                    // If this property was already used in the creation method signature, we're done with it
                    if (PropertyUsedInMethodSignature(property, methodParameters))
                    {
                        continue;
                    }

                    // Attempt to create a MemberMarkupInfo for the property, being unable to create
                    // one is indicative that we're done with it
                    var propertyInfo = MemberMarkupInfo.ForProperty(value, property, context);
                    if (propertyInfo == null)
                    {
                        continue;
                    }

                    // If this is the runtime-name property then we store the name in Name
                    // so that we can find it during the EnsureNoDuplicateNames phase
                    if (property == xamlType.GetAliasedProperty(XamlLanguage.Name))
                    {
                        // We have special handling of RuntimeNameProperty where we skip it if it is Null
                        if (IsNull(propertyInfo) || IsEmptyString(propertyInfo))
                        {
                            continue;
                        }

                        Name = ValidateNamePropertyAndFindName(propertyInfo);
                    }

                    propertyInfo.IsContent = IsPropertyContent(propertyInfo, xamlType);
                    Properties.Add(propertyInfo);
                }

                AddItemsProperty(value, context, xamlType);
                AddAttachedProperties(value, this, context);
            }

            private void AddRecordConstructionMembers(object value, XamlType valueXamlType, SerializerContext context,
                TypeConverter converter, out bool isComplete, out ParameterInfo[] methodParams)
            {
                MemberInfo member = null;
                ICollection arguments = null;
                isComplete = false;

                if (valueXamlType.IsMarkupExtension)
                {
                    if (!TryGetInstanceDescriptorInfo(value, context, converter, out member, out arguments, out isComplete))
                    {
                        // for MarkupExtensions, if there is no instance descriptor, we prefer
                        // 1. default constructor
                        // 2. non-default constructor with matching constructor arguments as positional parameters
                        // 3. non-default constructor with matching constructor arguments as x:arguments
                        if (!TryGetDefaultConstructorInfo(valueXamlType, out member, out arguments, out isComplete))
                        {
                            GetConstructorInfo(value, valueXamlType, context, out member, out arguments, out isComplete);

                            if (!TryAddPositionalParameters(valueXamlType, member, arguments, context))
                            {
                                AddArgumentsMembers(arguments, context);
                            }
                        }
                    }
                    else
                    {
                        // for MarkupExtensions, if there is an instance descriptor, we prefer
                        // 1. instance descriptor as positional parameters
                        // (which might not be possible since not all arguments could be represented as positional parameters)
                        // 2. default constructor
                        // 3. instance descriptor as x:arguments
                        if (!TryAddPositionalParameters(valueXamlType, member, arguments, context))
                        {
                            // save the member, arguments and isComplete from instance descriptor
                            // since we might need them later.
                            MemberInfo instanceDescriptorMember = member;
                            ICollection instanceDescriptorArguments = arguments;
                            bool instanceDescriptorIsComplete = isComplete;

                            // adding positional parameters might fail because the parameter types are not convertible to string
                            if (!TryGetDefaultConstructorInfo(valueXamlType, out member, out arguments, out isComplete))
                            {
                                member = instanceDescriptorMember;
                                arguments = instanceDescriptorArguments;
                                isComplete = instanceDescriptorIsComplete;

                                AddArgumentsMembers(arguments, context);
                            }
                        }
                    }
                }
                else
                {
                    // for non-MarkupExtensions, we prefer
                    // 1. default constructor
                    // 2. instance descriptor
                    // 3. non-default constructor with matching constructor arguments
                    if (!TryGetDefaultConstructorInfo(valueXamlType, out member, out arguments, out isComplete))
                    {
                        if (!TryGetInstanceDescriptorInfo(value, context, converter, out member, out arguments, out isComplete))
                        {
                            GetConstructorInfo(value, valueXamlType, context, out member, out arguments, out isComplete);
                        }
                        AddArgumentsMembers(arguments, context);
                    }
                }

                // Note: if this is the default ctor, member will be null, because we key off of
                // XamlType.IsConstructible/ConstructionRequiresArguments, rather than lookup up the
                // ConstructorInfo ourselves.
                AddFactoryMethodAndValidateArguments(value.GetType(), member, arguments, context, out methodParams);
            }

            private bool IsPropertyContent(MemberMarkupInfo propertyInfo, XamlType containingType)
            {
                var property = propertyInfo.XamlNode.Member;
                if (property != containingType.ContentProperty)
                {
                    return false;
                }
                if (propertyInfo.IsAtomic)
                {
                    // we do not want type-converted properties as content properties
                    // to conform with 3.0
                    return XamlLanguage.String.CanAssignTo(property.Type);
                }
                return true;
            }

            private void GetConstructorInfo(object value, XamlType valueXamlType, SerializerContext context, out MemberInfo member, out ICollection arguments, out bool isComplete)
            {
                // Walk the constructors of the type and find the ones with signatures that match the types of
                // the properties we found above and whose param names match the names we found above
                //
                member = null;
                arguments = null;
                isComplete = false;

                // Pull out all the properties that are attributed with ConstructorArgumentAttribute
                ICollection<XamlMember> properties = valueXamlType.GetAllMembers();
                ICollection<XamlMember> readOnlyProperties = valueXamlType.GetAllExcludedReadOnlyMembers();
                var ctorArgProps = new List<XamlMember>();

                foreach (XamlMember p in properties)
                {
                    if (context.IsPropertyReadVisible(p) && !string.IsNullOrEmpty(GetConstructorArgument(p)))
                    {
                        ctorArgProps.Add(p);
                    }
                }
                foreach (XamlMember p in readOnlyProperties)
                {
                    if (context.IsPropertyReadVisible(p) && !string.IsNullOrEmpty(GetConstructorArgument(p)))
                    {
                        ctorArgProps.Add(p);
                    }
                }

                foreach (var constructor in valueXamlType.GetConstructors())
                {
                    ParameterInfo[] constructorParameters = constructor.GetParameters();

                    // If there aren't the same number of parameters as there are properties with
                    // ConstructorArgumentAttribute then we know this one won't match...
                    if (constructorParameters.Length != ctorArgProps.Count)
                    {
                        continue;
                    }

                    IList constructorArguments = new List<object>(constructorParameters.Length);
                    for (int i = 0; i < constructorParameters.Length; i++)
                    {
                        ParameterInfo paraminfo = constructorParameters[i];

                        XamlMember matchingProperty = null;
                        foreach (var potentialProperty in ctorArgProps)
                        {
                            if ((potentialProperty.Type.UnderlyingType == paraminfo.ParameterType) &&
                                (GetConstructorArgument(potentialProperty) == paraminfo.Name))
                            {
                                matchingProperty = potentialProperty;
                                break;
                            }
                        }

                        // At least one of the arguments has no matching property... break out of the loop and
                        // we'll move on to the next constructor.
                        if (matchingProperty == null)
                        {
                            break;
                        }

                        constructorArguments.Add(context.Runtime.GetValue(value, matchingProperty));
                    }

                    // If we found a match for all the properties annotated with ConstructorArgumentAttribute,
                    // then we have found an acceptable constructor. Hooray!
                    if (constructorArguments.Count == ctorArgProps.Count)
                    {
                        member = constructor;
                        arguments = constructorArguments;

                        // In addition, if we found a constructor argument for *each* property, then we know the
                        // instance is complete and we don't need to worry about setting any additional
                        // properties.
                        if (constructorArguments.Count == properties.Count)
                        {
                            if (!valueXamlType.IsCollection && !valueXamlType.IsDictionary) // always need to serialize items of a collection or dictionary
                            {
                                isComplete = true;
                            }
                        }

                        break;
                    }
                }

                if (member == null && !valueXamlType.UnderlyingType.IsValueType)
                {
                    if (ctorArgProps.Count == 0)
                    {
                        throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderNoDefaultConstructor, value.GetType()));
                    }
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderNoMatchingConstructor, value.GetType()));
                }
            }

            private static void CheckTypeCanRoundtrip(ObjectMarkupInfo objInfo)
            {
                var xamlType = objInfo.XamlNode.XamlType;
                if (!xamlType.IsConstructible)
                {
                    foreach (var property in objInfo.Properties)
                    {
                        if (((MemberMarkupInfo)property).IsFactoryMethod && !xamlType.UnderlyingType.IsNested)
                        {
                            // this is the case when the class has no public constructor we can use but contains a factory method
                            // and the class is not nested

                            return;
                        }
                    }

                    if (xamlType.UnderlyingType.IsNested)
                    {
                        throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderTypeIsNested, xamlType.Name));
                    }
                    else
                    {
                        throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderTypeCannotRoundtrip, xamlType.Name));
                    }
                }
            }

            public void AssignName(SerializerContext context)
            {
                if (Name == null)
                {
                    Name = context.AllocateIdentifier();
                    AddNameProperty(context);
                }
            }

            public void AssignName(string name, SerializerContext context)
            {
                if (Name == null)
                {
                    Name = name;

                    if (name.StartsWith(KnownStrings.ReferenceName, StringComparison.Ordinal))
                    {
                        AddNameProperty(context);
                    }
                }
            }

            public void AddNameProperty(SerializerContext context)
            {
                Properties.Add(new MemberMarkupInfo()
                {
                    XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Name),
                    Children = { new ValueMarkupInfo() { XamlNode = new XamlNode(XamlNodeType.Value, Name) } }
                });

                // some collection or dictionary's object node might have been removed
                // i.e. XamlNode is now changed to GetObject.  since we are now naming
                // the object, we need to add the object node back
                if (XamlNode.NodeType == XamlNodeType.GetObject)
                {
                    Debug.Assert(Object != null);
                    XamlType xamlType = context.LocalAssemblyAwareGetXamlType(Object.GetType());
                    XamlNode = new XamlNode(XamlNodeType.StartObject, xamlType);
                }
            }

            public override void EnsureNoDuplicateNames(Stack<HashSet<string>> namesInCurrentScope)
            {
                if (!String.IsNullOrEmpty(Name) && !namesInCurrentScope.Peek().Add(Name))
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderXamlNamedElementAlreadyRegistered, Name));
                }

                // Recurse through all of the values I have.
                foreach (var property in Properties)
                {
                    var propertyInfo = (MemberMarkupInfo)property;
                    foreach (var ov in propertyInfo.Children)
                    {
                        ((ObjectOrValueMarkupInfo)ov).EnsureNoDuplicateNames(namesInCurrentScope);
                    }
                }
            }

            private static string ConvertTypeAndMethodToString(Type type, string methodName, SerializerContext context)
            {
                string typeString = context.ConvertXamlTypeToString(context.LocalAssemblyAwareGetXamlType(type));
                return typeString + "." + methodName;
            }

            private static ObjectMarkupInfo ForArray(Array value, SerializerContext context)
            {
                if (value.Rank > 1)
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderMultidimensionalArrayNotSupported));
                }

                XamlType type = context.LocalAssemblyAwareGetXamlType(value.GetType());
                XamlType elementType = type.ItemType;
                var items = new MemberMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Items) };
                foreach (object item in value)
                {
                    items.Children.Add(ForObject(item, context));
                }

                var objectInfo = new ObjectMarkupInfo()
                {
                    XamlNode = new XamlNode(XamlNodeType.StartObject, XamlLanguage.Array),
                    Object = value,
                    Properties =
                    {
                        new MemberMarkupInfo()
                        {
                            XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Array.GetMember("Type")), //x:ArrayExtension.Type
                            Children =
                            {
                                new ValueMarkupInfo()
                                {
                                    XamlNode = new XamlNode(XamlNodeType.Value, context.ConvertXamlTypeToString(elementType))
                                }
                            }
                        }
                    }
                };

                if (items.Children.Count != 0)
                {
                    var iListInfo = new ObjectMarkupInfo
                    {
                        XamlNode = new XamlNode(XamlNodeType.GetObject),
                        Properties =
                        {
                            items
                        }
                    };

                    var arrayItemsInfo = new MemberMarkupInfo()
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Array.ContentProperty),
                        Children =
                        {
                            iListInfo
                        }
                    };

                    objectInfo.Properties.Add(arrayItemsInfo);
                }

                AddAttachedProperties(value, objectInfo, context);
                return objectInfo;
            }

            private static void AddAttachedProperties(object value, ObjectMarkupInfo objectInfo, SerializerContext context)
            {
                var props = context.Runtime.GetAttachedProperties(value);
                if (props != null)
                {
                    foreach (var ap in props)
                    {
                        XamlType owningType = context.GetXamlType(ap.Key.DeclaringType);
                        if(!owningType.IsVisibleTo(context.LocalAssembly))
                        {
                            continue;
                        }
                        XamlMember attachedProperty = owningType.GetAttachableMember(ap.Key.MemberName);

                        if (attachedProperty == null)
                        {
                            throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderAttachedPropertyNotFound, owningType, ap.Key.MemberName));
                        }

                        if (!CanPropertyXamlRoundtrip(attachedProperty, context))
                        {
                            continue;
                        }

                        var propertyInfo = MemberMarkupInfo.ForAttachedProperty(value, attachedProperty, ap.Value, context);
                        if (propertyInfo != null)
                        {
                            objectInfo.Properties.Add(propertyInfo);
                        }
                    }
                }
            }

            private static ObjectMarkupInfo ForNull()
            {
                return new ObjectMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartObject, XamlLanguage.Null) };
            }

            public static ObjectMarkupInfo ForObject(object value, SerializerContext context, TypeConverter instanceConverter = null, bool isRoot = false)
            {
                if (value == null)
                {
                    return ForNull();
                }

                ObjectMarkupInfo existingInfo = context.ReferenceTable.Find(value);
                if (existingInfo != null)
                {
                    existingInfo.AssignName(context);
                    return new ReferenceMarkupInfo(existingInfo);
                }

                context.IsRoot = isRoot;
                if (value is Array valueAsArray)
                {
                    return ForArray(valueAsArray, context);
                }

                XamlType valueType = context.GetXamlType(value.GetType());

                ValueSerializer valueSerializer = null;
                TypeConverter converter = null;

                if (valueType.ContentProperty == null ||
                    (valueType.ContentProperty.TypeConverter != BuiltInValueConverter.String &&
                     valueType.ContentProperty.TypeConverter != BuiltInValueConverter.Object))
                {
                    valueSerializer = TypeConverterExtensions.GetConverterInstance(valueType.ValueSerializer);
                    converter = TypeConverterExtensions.GetConverterInstance(valueType.TypeConverter);
                }

                context.Instance = value;
                ObjectMarkupInfo objectInfo;
                if (valueType.DeferringLoader != null)
                {
                    objectInfo = MemberMarkupInfo.ConvertToXamlReader(value, valueType.DeferringLoader, context);
                }
                else if (converter != null && valueSerializer != null && context.CanRoundtripUsingValueSerializer(valueSerializer, converter, value))
                {
                    if (isRoot)
                    {
                        context.ReserveDefaultPrefixForRootObject(value);
                    }
                    //ValueSerializer is always preferred over TypeConverters
                    string stringValue = context.ConvertToString(valueSerializer, value);
                    context.Instance = null;
                    objectInfo = ForTypeConverted((string)stringValue, value, context);
                }
                else if (converter != null && context.TryConvertToMarkupExtension(converter, ref value))
                {
                    context.Instance = null;

                    if (isRoot)
                    {
                        context.ReserveDefaultPrefixForRootObject(value);
                    }

                    objectInfo = ForObject(value, context);
                }
                else if (value is Type type)
                {
                    context.Instance = null;
                    objectInfo = ForObject(new TypeExtension(type), context);
                }
                else if (converter != null && context.CanRoundTripString(converter))
                {
                    if (isRoot)
                    {
                        context.ReserveDefaultPrefixForRootObject(value);
                    }
                    var stringValue = context.ConvertTo<string>(converter, value);
                    context.Instance = null;
                    objectInfo = ForTypeConverted((string)stringValue, value, context);
                }
                else if (value is string)
                {
                    context.Instance = null;
                    objectInfo = ForTypeConverted((string)value, value, context);
                }
                else
                {
                    if (isRoot)
                    {
                        context.ReserveDefaultPrefixForRootObject(value);
                    }
                    context.Instance = null;
                    objectInfo = ForObjectInternal(value, context, instanceConverter);
                }

                // check if any type converter has asked for the name of the current object
                string assignedName = context.ReferenceTable.FindInServiceProviderTable(value);
                if (assignedName != null)
                {
                    objectInfo.AssignName(assignedName, context);
                }

                CheckTypeCanRoundtrip(objectInfo);

                return objectInfo;
            }

            private static ObjectMarkupInfo ForObjectInternal(object value, SerializerContext context, TypeConverter converter)
            {
                ObjectMarkupInfo recordInfo;
                XamlType xamlType = context.LocalAssemblyAwareGetXamlType(value.GetType());

                if (value is INameScope)
                {
                    // This is a name scope; add it to the queue and we'll deal with it later.
                    var nameScopeInfo = new NameScopeMarkupInfo
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartObject, xamlType),
                        Object = value,
                        SourceObject = value,
                        ParentTable = context.ReferenceTable
                    };

                    context.PendingNameScopes.Enqueue(nameScopeInfo);
                    AddReference(value, nameScopeInfo, context);
                    recordInfo = nameScopeInfo;
                }
                else
                {
                    recordInfo = new ObjectMarkupInfo
                    {
                        XamlNode = new XamlNode(XamlNodeType.StartObject, xamlType),
                        Object = value
                    };
                    // It is important to add the MarkupInfo to the reference table before adding its members
                    // in order to prevent problems with recursive references.
                    AddReference(value, recordInfo, context);
                    recordInfo.AddRecordMembers(value, context, converter);
                }

                return recordInfo;
            }

            private static void AddReference(object value, ObjectMarkupInfo objectInfo, SerializerContext context)
            {
                context.ReferenceTable.Add(value, objectInfo);
            }

            private static ObjectMarkupInfo ForTypeConverted(string value, object originalValue, SerializerContext context)
            {
                var xamlType = context.LocalAssemblyAwareGetXamlType(originalValue.GetType());
                var objectInfo = new ObjectMarkupInfo
                {
                    XamlNode = new XamlNode(XamlNodeType.StartObject, xamlType),
                    Object = originalValue
                    //Scope = context.ReferenceTable
                };

                // we want to treat all null values returned by TCs as String.Empty
                value = value ?? string.Empty;

                objectInfo.Properties.Add(new MemberMarkupInfo()
                {
                    XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.Initialization),
                    Children = { new ValueMarkupInfo() { XamlNode = new XamlNode(XamlNodeType.Value, value) } }
                });

                AddAttachedProperties(originalValue, objectInfo, context);

                return objectInfo;
            }

            private static bool IsEmptyString(MemberMarkupInfo propertyInfo)
            {
                if (propertyInfo.Children.Count == 1)
                {
                    if (propertyInfo.Children[0] is ValueMarkupInfo valueInfo)
                    {
                        return Equals(valueInfo.XamlNode.Value, string.Empty);
                    }
                }

                return false;
            }

            private static bool IsNull(MemberMarkupInfo propertyInfo)
            {
                return propertyInfo.Children.Count == 1 && 
                       propertyInfo.Children[0] is ObjectMarkupInfo objectInfo &&
                       objectInfo.XamlNode.XamlType == XamlLanguage.Null;
            }

            private static bool PropertyUsedInMethodSignature(XamlMember property, ParameterInfo[] methodParameters)
            {
                if (methodParameters != null)
                {
                    if (!string.IsNullOrEmpty(GetConstructorArgument(property)))
                    {
                        return methodParameters.Any(parameter => 
                            parameter.Name == GetConstructorArgument(property) && 
                            parameter.ParameterType == property.Type.UnderlyingType);
                    }
                }

                return false;
            }

            private static string ValidateNamePropertyAndFindName(MemberMarkupInfo propertyInfo)
            {
                // The name property needs to be a single value that is an atomic string. If it isn't, then it's not
                // a valid name.
                if (propertyInfo.Children.Count == 1)
                {
                    var valueInfo = propertyInfo.Children[0] as ValueMarkupInfo;
                    if (valueInfo?.XamlNode.Value is string name)
                    {
                        return name;
                    }
                }
                XamlMember property = propertyInfo.XamlNode.Member;
                throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderXamlNamePropertyMustBeString, property.Name, property.DeclaringType));
            }

            private class PropertySorterForXmlSyntax : IComparer<MarkupInfo>
            {
                private const int Equal = 0;
                private const int XFirst = -1;
                private const int YFirst = 1;

                public static readonly PropertySorterForXmlSyntax Instance = new PropertySorterForXmlSyntax();

                public int Compare(MarkupInfo x, MarkupInfo y)
                {
                    // The order is as follows:
                    //    - factory method directive
                    //    - attributable markup extensions
                    //    - atoms if they aren't the content property
                    //    - initialization value or x:args before any other directives
                    //    - x: directive before non-directive
                    //    - non-content property in alphabetical order
                    //    - content property or items directive

                    var xInfo = (MemberMarkupInfo)x;
                    var yInfo = (MemberMarkupInfo)y;

                    var xProperty = x.XamlNode.Member;
                    var yProperty = y.XamlNode.Member;

                    bool xIsFactoryMethod = xInfo.IsFactoryMethod;
                    bool yIsFactoryMethod = yInfo.IsFactoryMethod;

                    if (xIsFactoryMethod && !yIsFactoryMethod)
                    {
                        return XFirst;
                    }
                    if (yIsFactoryMethod && !xIsFactoryMethod)
                    {
                        return YFirst;
                    }

                    bool xIsContentOrItemsProperty = xInfo.IsContent || (xProperty == XamlLanguage.Items);
                    bool yIsContentOrItemsProperty = yInfo.IsContent || (yProperty == XamlLanguage.Items);

                    if (xIsContentOrItemsProperty && !yIsContentOrItemsProperty)
                    {
                        return YFirst;
                    }
                    if (yIsContentOrItemsProperty && !xIsContentOrItemsProperty)
                    {
                        return XFirst;
                    }

                    bool xIsAttributableMarkupExtension = xInfo.IsAttributableMarkupExtension;
                    bool yIsAttributableMarkupExtension = yInfo.IsAttributableMarkupExtension;

                    if (xIsAttributableMarkupExtension && !yIsAttributableMarkupExtension)
                    {
                        return XFirst;
                    }
                    if (yIsAttributableMarkupExtension && !xIsAttributableMarkupExtension)
                    {
                        return YFirst;
                    }

                    bool xIsAtomic = xInfo.IsAtomic;
                    bool yIsAtomic = yInfo.IsAtomic;

                    bool xIsInitialization = (xProperty == XamlLanguage.Initialization);
                    bool yIsInitialization = (yProperty == XamlLanguage.Initialization);

                    bool xIsAtomicAndNotInitialization = xIsAtomic && !xIsInitialization;
                    bool yIsAtomicAndNotInitialization = yIsAtomic && !yIsInitialization;

                    if (xIsAtomicAndNotInitialization && !yIsAtomicAndNotInitialization)
                    {
                        return XFirst;
                    }
                    if (yIsAtomicAndNotInitialization && !xIsAtomicAndNotInitialization)
                    {
                        return YFirst;
                    }

                    if (xIsAtomic && !yIsAtomic)
                    {
                        return XFirst;
                    }
                    if (yIsAtomic && !xIsAtomic)
                    {
                        return YFirst;
                    }

                    if (xIsInitialization && !yIsInitialization)
                    {
                        return XFirst; }
                    if (yIsInitialization && !xIsInitialization)
                    {
                        return YFirst; }

                    bool xIsArgumentsDirective = (xProperty == XamlLanguage.Arguments);
                    bool yIsArgumentsDirective = (yProperty == XamlLanguage.Arguments);

                    if (xIsArgumentsDirective && !yIsArgumentsDirective)
                    {
                        return XFirst;
                    }
                    if (yIsArgumentsDirective && !xIsArgumentsDirective)
                    {
                        return YFirst;
                    }

                    bool xIsDirective = xProperty.IsDirective;
                    bool yIsDirective = yProperty.IsDirective;

                    if (xIsDirective && !yIsDirective)
                    {
                        return XFirst;
                    }
                    if (yIsDirective && !xIsDirective)
                    {
                        return YFirst;
                    }

                    return string.CompareOrdinal(xProperty.Name, yProperty.Name);
                }
            }

            private class PropertySorterForCurlySyntax : IComparer<MarkupInfo>
            {
                private const int Equal = 0;
                private const int XFirst = -1;
                private const int YFirst = 1;

                public static readonly PropertySorterForCurlySyntax Instance = new PropertySorterForCurlySyntax();

                public int Compare(MarkupInfo x, MarkupInfo y)
                {
                    // The order is as follows:
                    //    - positional parameters
                    //    - factory method directive
                    //    - attributable markup extensions
                    //    - atoms if they aren't the content property
                    //    - x: directive before non-directive
                    //    - property in alphabetical order

                    var xInfo = (MemberMarkupInfo)x;
                    var yInfo = (MemberMarkupInfo)y;

                    var xProperty = x.XamlNode.Member;
                    var yProperty = y.XamlNode.Member;

                    bool xIsPositionalParameterProperty = (xProperty == XamlLanguage.PositionalParameters);
                    bool yIsPositionalParameterProperty = (yProperty == XamlLanguage.PositionalParameters);

                    if (xIsPositionalParameterProperty && !yIsPositionalParameterProperty)
                    {
                        return XFirst;
                    }
                    if (yIsPositionalParameterProperty && !xIsPositionalParameterProperty)
                    {
                        return YFirst;
                    }

                    bool xIsFactoryMethod = xInfo.IsFactoryMethod;
                    bool yIsFactoryMethod = yInfo.IsFactoryMethod;

                    if (xIsFactoryMethod && !yIsFactoryMethod)
                    {
                        return XFirst;
                    }
                    if (yIsFactoryMethod && !xIsFactoryMethod)
                    {
                        return YFirst;
                    }

                    bool xIsAttributableMarkupExtension = xInfo.IsAttributableMarkupExtension;

                    bool yIsAttributableMarkupExtension = yInfo.IsAttributableMarkupExtension;

                    if (xIsAttributableMarkupExtension && !yIsAttributableMarkupExtension)
                    {
                        return XFirst;
                    }
                    if (yIsAttributableMarkupExtension && !xIsAttributableMarkupExtension)
                    {
                        return YFirst;
                    }

                    bool xIsAtomic = xInfo.IsAtomic;
                    bool yIsAtomic = yInfo.IsAtomic;

                    if (xIsAtomic && !yIsAtomic)
                    {
                        return XFirst;
                    }
                    if (yIsAtomic && !xIsAtomic)
                    {
                        return YFirst;
                    }

                    bool xIsDirective = xProperty.IsDirective;
                    bool yIsDirective = yProperty.IsDirective;

                    if (xIsDirective && !yIsDirective)
                    {
                        return XFirst;
                    }
                    if (yIsDirective && !yIsDirective)
                    {
                        return YFirst;
                    }

                    return string.CompareOrdinal(xProperty.Name, yProperty.Name);
                }
            }

            internal static bool CanPropertyXamlRoundtrip(XamlMember property, SerializerContext context)
            {
                return !property.IsEvent && context.IsPropertyReadVisible(property) &&
                    (context.IsPropertyWriteVisible(property) || property.Type.IsUsableAsReadOnly);
            }

            static List<XamlMember> GetXamlSerializableProperties(XamlType type, SerializerContext context)
            {
                List<XamlMember> propertyList = new List<XamlMember>();
                foreach (XamlMember property in type.GetAllMembers())
                {
                    if (CanPropertyXamlRoundtrip(property, context))
                    {
                        propertyList.Add(property);
                    }
                }
                return propertyList;
            }
        }

        private sealed class ReferenceMarkupInfo : ObjectMarkupInfo
        {
            public ObjectMarkupInfo Target { get; set; }

            private MemberMarkupInfo _nameProperty;

            public ReferenceMarkupInfo(ObjectMarkupInfo target)
            {
                XamlNode = new XamlNode(XamlNodeType.StartObject, XamlLanguage.Reference);
                _nameProperty = new MemberMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartMember, XamlLanguage.PositionalParameters) };
                Properties.Add(_nameProperty);
                Target = target;
                Object = target.Object;
            }

            public override List<MarkupInfo> Decompose()
            {
                _nameProperty.Children.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.Value, Target.Name) });
                return base.Decompose();
            }
        }

        private class ReferenceTable
        {
            private readonly ReferenceTable _parent;

            // table that remembers all objects seen in the current namescope
            private readonly Dictionary<object, ObjectMarkupInfo> _objectGraphTable;

            // table that remembers all the objects whose names are requested by TCs, MEs,
            // and the names assigned to them.
            private Dictionary<object, string> _serviceProviderTable;

            public ReferenceTable(ReferenceTable parent)
            {
                _parent = parent;
                _objectGraphTable = new Dictionary<object, ObjectMarkupInfo>(new ObjectReferenceEqualityComparer());
            }

            public void Add(object value, ObjectMarkupInfo info)
            {
                _objectGraphTable.Add(value, info);
            }

            public void AddToServiceProviderTable(object value, string name)
            {
                if (_serviceProviderTable == null)
                {
                    _serviceProviderTable = new Dictionary<object, string>(new ObjectReferenceEqualityComparer());
                }
                _serviceProviderTable.Add(value, name);
            }

            public ObjectMarkupInfo Find(object value)
            {
                ObjectMarkupInfo result;
                if (!_objectGraphTable.TryGetValue(value, out result))
                {
                    // this is a recursive search, because objects in the parent namescopes are still visible
                    if (_parent != null)
                    {
                        return _parent.Find(value);
                    }
                }
                return result;
            }

            public string FindInServiceProviderTable(object value)
            {
                string result = null;

                if (_serviceProviderTable != null)
                {
                    _serviceProviderTable.TryGetValue(value, out result);
                }

                // this search is not recursive, because only names requested in the current
                // namescope are meaningful references.
                return result;
            }
        }

        private class SerializerContext
        {
            private int _lastIdentifier;
            private readonly Queue<NameScopeMarkupInfo> _pendingNameScopes;
            private readonly ITypeDescriptorContext _typeDescriptorContext;
            private readonly IValueSerializerContext _valueSerializerContext;
            private Dictionary<string, string> _namespaceToPrefixMap;
            private Dictionary<string, string> _prefixToNamespaceMap;
            private readonly XamlSchemaContext _schemaContext;
            private readonly ClrObjectRuntime _runtime;
            private readonly XamlObjectReaderSettings _settings;

            public SerializerContext(XamlSchemaContext schemaContext, XamlObjectReaderSettings settings)
            {
                _pendingNameScopes = new Queue<NameScopeMarkupInfo>();
                var typeDescriptorContext = new TypeDescriptorAndValueSerializerContext(this);
                _typeDescriptorContext = typeDescriptorContext;
                _valueSerializerContext = typeDescriptorContext;
                _namespaceToPrefixMap = new Dictionary<string, string>();
                _prefixToNamespaceMap = new Dictionary<string, string>();
                ReferenceTable = new ReferenceTable(null);
                _schemaContext = schemaContext;
                _runtime = new ClrObjectRuntime(null, isWriter: false);
                _settings = settings;
            }

            public XamlObjectReaderSettings Settings => _settings;

            public object Instance { get; set; }

            public void ReserveDefaultPrefixForRootObject(object obj)
            {
                string ns = GetXamlType(obj.GetType()).PreferredXamlNamespace;
                if (ns != XamlLanguage.Xaml2006Namespace)
                {
                    _namespaceToPrefixMap.Add(ns, string.Empty);
                    _prefixToNamespaceMap.Add(string.Empty, ns);
                }
            }

            public ClrObjectRuntime Runtime => _runtime;

            public Queue<NameScopeMarkupInfo> PendingNameScopes => _pendingNameScopes;

            public ReferenceTable ReferenceTable { get; set; }

            public IValueSerializerContext ValueSerializerContext => _valueSerializerContext;

            public ITypeDescriptorContext TypeDescriptorContext => _typeDescriptorContext;

            public XamlSchemaContext SchemaContext => _schemaContext;

            public List<XamlNode> GetSortedNamespaceNodes()
            {
                var namespaceMapList = new List<KeyValuePair<string, string>>();
                foreach (var pair in _namespaceToPrefixMap)
                {
                    namespaceMapList.Add(pair);
                }

                namespaceMapList.Sort(CompareByValue);
                return namespaceMapList.ConvertAll<XamlNode>(pair => new XamlNode(XamlNodeType.NamespaceDeclaration,
                                                                       new NamespaceDeclaration(pair.Key, pair.Value)));
            }

            public Assembly LocalAssembly => Settings.LocalAssembly;

            public bool IsRoot{ get; set; }

            public Type RootType { get; set; }

            static int CompareByValue(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                // we sort the list in reverse alphabetical order of prefixes
                return string.Compare(y.Value, x.Value, false, TypeConverterHelper.InvariantEnglishUS);
            }

            public string AllocateIdentifier()
            {
                return KnownStrings.ReferenceName + (_lastIdentifier++);
            }

            public bool TryHoistNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
            {
                string ns;
                if (_prefixToNamespaceMap.TryGetValue(namespaceDeclaration.Prefix, out ns))
                {
                    return ns == namespaceDeclaration.Namespace;
                }

                _namespaceToPrefixMap.Add(namespaceDeclaration.Namespace, namespaceDeclaration.Prefix);
                _prefixToNamespaceMap.Add(namespaceDeclaration.Prefix, namespaceDeclaration.Namespace);
                return true;
            }

            public string FindPrefix(string ns)
            {
                string prefix = null;

                if (_namespaceToPrefixMap.TryGetValue(ns, out prefix))
                {
                    return prefix;
                }

                string basePrefix = SchemaContext.GetPreferredPrefix(ns);

                if (basePrefix != XamlLanguage.PreferredPrefix && !_namespaceToPrefixMap.ContainsValue(string.Empty))
                {
                    prefix = string.Empty;
                }

                if (prefix == null)
                {
                    prefix = basePrefix;

                    int index = 0;

                    while (_namespaceToPrefixMap.ContainsValue(prefix))
                    {
                        index += 1;
                        prefix = basePrefix + index.ToString(TypeConverterHelper.InvariantEnglishUS);
                    }

                    if (prefix != string.Empty)
                    {
                        XmlConvert.VerifyNCName(prefix);
                    }
                }

                _namespaceToPrefixMap.Add(ns, prefix);
                _prefixToNamespaceMap.Add(prefix, ns);
                return prefix;
            }

            public XamlType GetXamlType(Type clrType)
            {
                XamlType result = _schemaContext.GetXamlType(clrType);
                if (result == null)
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReaderTypeNotAllowed, _schemaContext.GetType(), clrType));
                }
                return result;
            }

            public XamlType LocalAssemblyAwareGetXamlType(Type clrType)
            {
                XamlType result = GetXamlType(clrType);
                if(!result.IsVisibleTo(LocalAssembly) && !typeof(Type).IsAssignableFrom(clrType))
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.ObjectReader_TypeNotVisible, clrType.FullName));
                }
                return result;
            }

            public bool CanConvertTo(TypeConverter converter, Type type)
            {
                return Runtime.CanConvertTo(TypeDescriptorContext, converter, type);
            }

            public bool CanRoundTripString(TypeConverter converter)
            {
                // ReferenceConverter lies.
                if (converter is ReferenceConverter)
                {
                    return false;
                }

                return Runtime.CanConvertFrom<string>(TypeDescriptorContext, converter) &&
                    Runtime.CanConvertTo(TypeDescriptorContext, converter, typeof(string));
            }

            public bool CanRoundtripUsingValueSerializer(ValueSerializer valueSerializer, TypeConverter typeConverter, object value)
            {
                // ValueSerializer must know how to convert to string and the
                // TypeConverter must know how to convert from string
                return (valueSerializer != null &&
                    typeConverter != null &&
                    Runtime.CanConvertToString(ValueSerializerContext, valueSerializer, value) &&
                    Runtime.CanConvertFrom<string>(TypeDescriptorContext, typeConverter));
            }

            public string ConvertToString(ValueSerializer valueSerializer, object value)
            {
                return Runtime.ConvertToString(_valueSerializerContext, valueSerializer, value);
            }

            public T ConvertTo<T>(TypeConverter converter, object value)
            {
                return Runtime.ConvertToValue<T>(TypeDescriptorContext, converter, value);
            }

            public bool TryValueSerializeToString(ValueSerializer valueSerializer, TypeConverter propertyConverter, SerializerContext context, ref object value)
            {
                if (value == null)
                {
                    return false;
                }

                // skip using ValueSerializer if value is string
                if (value is string)
                {
                    return true;
                }

                // Test if value roundtrips using either the type converter on the
                // property or the type of the actual instance.
                XamlType valueXamlType = context.GetXamlType(value.GetType());
                TypeConverter actualTypeConverter = TypeConverterExtensions.GetConverterInstance(valueXamlType.TypeConverter);
                if (!CanRoundtripUsingValueSerializer(valueSerializer, propertyConverter, value) &&
                    !CanRoundtripUsingValueSerializer(valueSerializer, actualTypeConverter, value))
                {
                    return false;
                }

                value = Runtime.ConvertToString(ValueSerializerContext, valueSerializer, value);
                return true;
            }

            public bool TryTypeConvertToString(TypeConverter converter, ref object value)
            {
                if (value == null)
                {
                    return false;
                }

                // Regardless of what the type converter says, if the value of the property is now a string, then
                // we can just write the string straight in.
                if (value is string)
                {
                    return true;
                }
                if (!CanRoundTripString(converter))
                {
                    return false;
                }

                value = ConvertTo<string>(converter, value);
                return true;
            }

            public bool TryConvertToMarkupExtension(TypeConverter converter, ref object value)
            {
                if (value == null)
                {
                    return false;
                }
                if (!Runtime.CanConvertTo(TypeDescriptorContext, converter, typeof(MarkupExtension)))
                {
                    return false;
                }

                value = ConvertTo<MarkupExtension>(converter, value);
                return true;
            }

            public string ConvertXamlTypeToString(XamlType type)
            {
                var typeName = new XamlTypeName(type);
                return typeName.ConvertToStringInternal(FindPrefix);
            }

            public string GetName(object objectToName)
            {
                string runtimeName = null;
                XamlType type = GetXamlType(objectToName.GetType());
                XamlMember runtimeNameProperty = type.GetAliasedProperty(XamlLanguage.Name);

                if (runtimeNameProperty != null)
                {
                    runtimeName = Runtime.GetValue(objectToName, runtimeNameProperty) as string;
                }

                if (runtimeName != null)
                {
                    return runtimeName;
                }

                // Is the object already being referenced?
                ObjectMarkupInfo existingInfo = ReferenceTable.Find(objectToName);
                if (existingInfo != null)
                {
                    existingInfo.AssignName(this);
                    return existingInfo.Name;
                }

                // Has the object been looked up via TCs, MEs, already?
                string allocatedName = null;
                allocatedName = ReferenceTable.FindInServiceProviderTable(objectToName);

                if (allocatedName == null)
                {
                    // the object has not been seen at the current stage due to either
                    // 1. the object is present in other parts of the object graph yet to be constructed
                    // 2. the object is not part of the object graph, or not visible to the current namescope
                    // in both cases, we give it a name, and store it in the current namescope's
                    // table, to be looked up later

                    allocatedName = AllocateIdentifier();
                    ReferenceTable.AddToServiceProviderTable(objectToName, allocatedName);
                }
                return allocatedName;
            }

            public bool IsPropertyReadVisible(XamlMember property)
            {
                Type allowProtectedMemberOnType = null;
                if (Settings.AllowProtectedMembersOnRoot && IsRoot)
                {
                    allowProtectedMemberOnType = RootType;
                }
                return property.IsReadVisibleTo(LocalAssembly, allowProtectedMemberOnType);
            }

            public bool IsPropertyWriteVisible(XamlMember property)
            {
                Type allowProtectedMemberOnType = null;
                if (Settings.AllowProtectedMembersOnRoot && IsRoot)
                {
                    allowProtectedMemberOnType = RootType;
                }
                return property.IsWriteVisibleTo(LocalAssembly, allowProtectedMemberOnType);
            }
        }

        private class TypeDescriptorAndValueSerializerContext : IValueSerializerContext, INamespacePrefixLookup, IXamlSchemaContextProvider, IXamlNameProvider
        {
            private readonly SerializerContext _context;

            public TypeDescriptorAndValueSerializerContext(SerializerContext context)
            {
                _context = context;
            }

            public IContainer Container => null;

            public object Instance => _context.Instance;

            public PropertyDescriptor PropertyDescriptor => null;

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IValueSerializerContext))
                {
                    return this;
                }
                if (serviceType == typeof(ITypeDescriptorContext))
                {
                    return this;
                }
                if (serviceType == typeof(INamespacePrefixLookup))
                {
                    return this;
                }
                if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return this;
                }
                if (serviceType == typeof(IXamlNameProvider))
                {
                    return this;
                }

                return null;
            }

            public void OnComponentChanged()
            {
            }

            public bool OnComponentChanging() => false;

            public string LookupPrefix(string ns) => _context.FindPrefix(ns);

            public ValueSerializer GetValueSerializerFor(PropertyDescriptor propertyDescriptor)
            {
                return ValueSerializer.GetSerializerFor(propertyDescriptor);
            }

            public ValueSerializer GetValueSerializerFor(Type type)
            {
                return ValueSerializer.GetSerializerFor(type);
            }

            public XamlSchemaContext SchemaContext => _context.SchemaContext;

            public string GetName(object value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                return _context.GetName(value);
            }
        }

        private sealed class XamlTemplateMarkupInfo : ObjectMarkupInfo
        {
            private readonly List<MarkupInfo> _nodes = new List<MarkupInfo>();
            private int _objectPosition;

            /// <summary>
            /// This is kinda fancy-- XamlFactories are converted to XamlReaders, and then the XAML from those
            /// readers is supposed to be inserted into the XAML verbatim. The problem is that we are supposed to
            /// be a ObjectMarkupInfo, and that implies certain things, like the ability to add a key or
            /// a name.
            ///
            /// So! We shred the XAML into a record; for each member that the XamlReader reports, we create a new
            /// MemberMarkupInfo, and then for each subtree under the member we construct either an
            /// ValueMarkupInfo or a XamlNodeMarkupInfo to contain the subtree. (The reason we bother
            /// to check atom-ness is to make the XAML that we write prettier.)
            /// </summary>
            public XamlTemplateMarkupInfo(XamlReader reader, SerializerContext context)
            {
                // we hoist all the namespace declarations we can hoist to the root of the document
                // without conflicting any previously defined namespace declarations.
                while (reader.Read() && reader.NodeType != XamlNodeType.StartObject)
                {
                    if (reader.NodeType != XamlNodeType.NamespaceDeclaration)
                    {
                        throw new XamlObjectReaderException(SR.Get(SRID.XamlFactoryInvalidXamlNode, reader.NodeType));
                    }

                    if (!context.TryHoistNamespaceDeclaration(reader.Namespace))
                    {
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.NamespaceDeclaration, reader.Namespace) });
                    }
                }

                if (reader.NodeType != XamlNodeType.StartObject)
                {
                    throw new XamlObjectReaderException(SR.Get(SRID.XamlFactoryInvalidXamlNode, reader.NodeType));
                }

                _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartObject, reader.Type) });
                _objectPosition = _nodes.Count;

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                    case XamlNodeType.NamespaceDeclaration:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.NamespaceDeclaration, reader.Namespace) });
                        break;

                    case XamlNodeType.StartObject:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartObject, reader.Type) });
                        break;

                    case XamlNodeType.GetObject:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.GetObject) });
                        break;

                    case XamlNodeType.EndObject:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.EndObject) });
                        break;

                    case XamlNodeType.StartMember:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.StartMember, reader.Member) });
                        break;

                    case XamlNodeType.EndMember:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.EndMember) });
                        break;

                    case XamlNodeType.Value:
                        _nodes.Add(new ValueMarkupInfo { XamlNode = new XamlNode(XamlNodeType.Value, reader.Value) });
                        break;

                    default:
                        throw new InvalidOperationException(SR.Get(SRID.XamlFactoryInvalidXamlNode, reader.NodeType));
                    }
                }

                XamlNode = ((ValueMarkupInfo)_nodes[0]).XamlNode;
                _nodes.RemoveAt(0);
            }

            public override List<MarkupInfo> Decompose()
            {
                foreach (MarkupInfo property in Properties)
                {
                    _nodes.Insert(_objectPosition, property);
                }
                return _nodes;
            }

            public override void FindNamespace(SerializerContext context)
            {
                foreach (MarkupInfo property in Properties)
                {
                    property.FindNamespace(context);
                }
            }
        }

        // need to implement our own Set class to alleviate ties to System.Core.dll
        // HashSet<T> lives in System.Core.dll
        private class HashSet<T>
        {
            private Dictionary<T, bool> dictionary;

            public HashSet()
            {
                dictionary = new Dictionary<T, bool>();
            }

            public HashSet(IEqualityComparer<T> comparer)
            {
                dictionary = new Dictionary<T, bool>(comparer);
            }

            public bool Contains(T member) => dictionary.ContainsKey(member);

            public bool Add(T member)
            {
                if (Contains(member))
                {
                    return false;
                }
                else
                {
                    dictionary.Add(member, true);
                    return true;
                }
            }
        }

        private class PartiallyOrderedList<TKey, TValue> : IEnumerable<TValue>
    where TValue : class
        {
            /// <summary>
            /// Private class to hold the key and the value.
            /// </summary>
            private class Entry
            {
                public readonly TKey Key;
                public readonly TValue Value;
                public List<int> Predecessors;
                public int Link;    // -1 - unseen;  <-1 - in DFS;  >=0 - next index in top. order
                public const int UNSEEN = -1;
                public const int INDFS = -2;

                public Entry(TKey key, TValue value)
                {
                    Key = key;
                    Value = value;
                    Predecessors = null;
                    Link = 0;
                }

                public override bool Equals(object obj)
                {
                    Entry other = obj as Entry;
                    return other != null && other.Key.Equals(Key);
                }

                public override int GetHashCode()
                {
                    return Key.GetHashCode();
                }
            }

            /// <summary>
            /// Add a value to the list. The order in the list
            /// is controled by the key and calls to SetOrder.
            /// </summary>
            /// <param name="key">The ordering key</param>
            /// <param name="value">The value</param>
            public void Add(TKey key, TValue value)
            {
                Entry entry = new Entry(key, value);
                int existingIndex = _entries.IndexOf(entry);
                // If an entry already exists use it. This happens
                // when two values of the same key are added or when
                // SetOrder refering to a key that hasn't had a value
                // added for it yet. A null value is used as a place
                // holder.
                if (existingIndex >= 0)
                {
                    entry.Predecessors = _entries[existingIndex].Predecessors;
                    _entries[existingIndex] = entry;
                }
                else
                {
                    _entries.Add(entry);
                }
            }

            private int GetEntryIndex(TKey key)
            {
                Entry entry = new Entry(key, null);
                int result = _entries.IndexOf(entry);
                if (result < 0)
                {
                    result = _entries.Count;
                    _entries.Add(entry);
                }
                return result;
            }

            public void SetOrder(TKey predecessor, TKey key)
            {
                // Find where these keys are in the list
                // If they don't exist, a null value place holder is
                // added to the end of the list.
                int predIndex = GetEntryIndex(predecessor);
                Entry predEntry = _entries[predIndex];
                int keyIndex = GetEntryIndex(key);
                Entry keyEntry = _entries[keyIndex];

                // add the constraint
                if (keyEntry.Predecessors == null)
                {
                    keyEntry.Predecessors = new List<int>();
                }
                keyEntry.Predecessors.Add(predIndex);

                // mark the list to force a sort before the next
                // enumeration
                _firstIndex = Entry.UNSEEN;
            }

            // compute a linear order consistent with the constraints.
            // This is the classic Topological Sort problem, which is
            // solved in linear time by doing a depth-first search of the
            // "reverse" directed graph (edges go from a node to its
            // predecessors), and enumerating the nodes in postorder.
            // If there are no cycles, each node is enumerated after any
            // nodes that directly or indirectly precede it.
            private void TopologicalSort()
            {
                // initialize
                _firstIndex = Entry.UNSEEN;
                _lastIndex = Entry.UNSEEN;
                for (int i = 0; i < _entries.Count; ++i)
                {
                    _entries[i].Link = Entry.UNSEEN;
                }

                // start a DFS at each entry
                for (int i = 0; i < _entries.Count; ++i)
                {
                    DepthFirstSearch(i);
                }
            }

            // depth-first-search of predecessors of entry at given index
            private void DepthFirstSearch(int index)
            {
                // do a search, unless we've already seen this entry
                if (_entries[index].Link == Entry.UNSEEN)
                {
                    // mark entry as 'in progress'
                    _entries[index].Link = Entry.INDFS;

                    // search the predecessors
                    if (_entries[index].Predecessors != null)
                    {
                        foreach (int predIndex in _entries[index].Predecessors)
                        {
                            DepthFirstSearch(predIndex);
                        }
                    }

                    // Add the current entry to the postorder list.  We do this
                    // by linking the previous (in postorder) entry to this one
                    if (_lastIndex == -1)
                    {
                        _firstIndex = index;    // special case for head of list
                    }
                    else
                    {
                        _entries[_lastIndex].Link = index;
                    }

                    _lastIndex = index;
                }
                /* Note: if it is desired to detect cycles, this is the
                   place to do it.
                else if (_entries[index].Link == Entry.INDFS)
                {
                    // DFS has returned to an entry that is currently being
                    // searched.  This happens if and only if there is a cycle.
                    // Report the cycle.
                }
                */
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                // if new constraints have arrived, sort the list
                if (_firstIndex < 0)
                {
                    TopologicalSort();
                }

                // Enumerate the values according to the topological order.
                // We skip null values that
                // are just place holders for keys for which the order
                // was set but the value wasn't provided.
                int index = _firstIndex;
                while (index >= 0)
                {
                    Entry entry = _entries[index];
                    if (entry.Value != null)
                        yield return entry.Value;

                    index = entry.Link;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                // Non-generic version of IEnumerator
                foreach (TValue value in this)
                    yield return value;
            }

            private List<Entry> _entries = new List<Entry>();
            private int _firstIndex = Entry.UNSEEN; // head of linear order
            private int _lastIndex;                 // index of most recently assigned entry
        }

        internal static class TypeConverterExtensions
        {
            public static TConverter GetConverterInstance<TConverter>(XamlValueConverter<TConverter> converter) where TConverter : class
            {
                return converter?.ConverterInstance;
            }
        }

        private static class XamlMemberExtensions
        {
            // For compat with XamlWriter.Save, which used PropertyDescriptor, we need to look at
            // attributes not just on this property, but also on any base properties that it overrides.
            // Returns the nearest member up the hierarchy that fits the specified criterion.
            // If the no hits, just returns the original member.
            internal static XamlMember GetNearestMember(XamlMember member, GetNearestBaseMemberCriterion criterion)
            {
                if (member.IsAttachable || member.IsDirective || MeetsCriterion(member, criterion))
                {
                    return member;
                }
                MethodInfo accessor = member.Getter ?? member.Setter;
                if (accessor == null || !accessor.IsVirtual)
                {
                    return member;
                }
                Type baseDeclaringType = accessor.GetBaseDefinition().DeclaringType;
                if (member.DeclaringType.UnderlyingType == baseDeclaringType)
                {
                    return member;
                }

                XamlType baseType = member.DeclaringType.BaseType;
                while (baseType != null && baseType != XamlLanguage.Object)
                {
                    XamlMember baseMember = baseType.GetMember(member.Name);
                    if (baseMember == null)
                    {
                        baseMember = GetExcludedReadOnlyMember(baseType, member.Name);
                        if (baseMember == null)
                        {
                            break;
                        }
                    }
                    if (MeetsCriterion(baseMember, criterion))
                    {
                        return baseMember;
                    }
                    if (baseType.UnderlyingType == baseDeclaringType)
                    {
                        break;
                    }
                    baseType = baseMember.DeclaringType.BaseType;
                }
                return member;
            }

            private static XamlMember GetExcludedReadOnlyMember(XamlType type, string name)
            {
                foreach (XamlMember member in type.GetAllExcludedReadOnlyMembers())
                {
                    if (member.Name == name)
                    {
                        return member;
                    }
                }
                return null;
            }

            private static bool MeetsCriterion(XamlMember member, GetNearestBaseMemberCriterion criterion)
            {
                switch (criterion)
                {
                    case GetNearestBaseMemberCriterion.HasConstructorArgument:
                        return member.ConstructorArgument != null;
                    case GetNearestBaseMemberCriterion.HasDefaultValue:
                        return member.HasDefaultValue;
                    case GetNearestBaseMemberCriterion.HasSerializationVisibility:
                        return member.HasSerializationVisibility;
                    default:
                        Debug.Fail("Invalid enum value");
                        return false;
                }
            }

            internal enum GetNearestBaseMemberCriterion
            {
                HasSerializationVisibility,
                HasDefaultValue,
                HasConstructorArgument
            }
        }
    }
}
