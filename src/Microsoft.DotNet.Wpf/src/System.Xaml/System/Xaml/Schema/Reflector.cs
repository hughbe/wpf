﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Markup;

namespace System.Xaml.Schema
{
    internal abstract class Reflector
    {
        // If _attributeProvider is set, we will use it for all attribute lookups.
        // Otherwise, we will populate _attributeData with Member.GetCustomAttributesData.
        protected NullableReference<ICustomAttributeProvider> _attributeProvider;
        protected IList<CustomAttributeData> _attributeData;

        internal ICustomAttributeProvider CustomAttributeProvider
        {
            get => _attributeProvider.Value;
            set => _attributeProvider.Value = value;
        }

        internal void SetCustomAttributeProviderVolatile(ICustomAttributeProvider value)
        {
            _attributeProvider.SetVolatile(value);
        }

        internal bool CustomAttributeProviderIsSet => _attributeProvider.IsSet;

        internal bool CustomAttributeProviderIsSetVolatile => _attributeProvider.IsSetVolatile;

        protected abstract MemberInfo Member { get; }

        public bool IsAttributePresent(Type attributeType)
        {
            if (CustomAttributeProvider != null)
            {
                return CustomAttributeProvider.IsDefined(attributeType, false);
            }
            try
            {
                CustomAttributeData cad = GetAttribute(attributeType);
                return (cad != null);
            }
            catch (CustomAttributeFormatException)
            {
                CustomAttributeProvider = Member;
                return IsAttributePresent(attributeType);
            }
        }

        // Returns null if attribute wasn't found, string.Empty if attribute string was null or empty
        public string GetAttributeString(Type attributeType, out bool checkedInherited)
        {
            if (CustomAttributeProvider != null)
            {
                // Passes inherit=true for reasons explained in comment on XamlType.TryGetAttributeString
                checkedInherited = true;

                object[] attributes = CustomAttributeProvider.GetCustomAttributes(attributeType, true /*inherit*/);
                if (attributes.Length == 0)
                {
                    return null;
                }

                if (attributeType == typeof(ContentPropertyAttribute))
                {
                    return ((ContentPropertyAttribute)attributes[0]).Name;
                }
                else if (attributeType == typeof(RuntimeNamePropertyAttribute))
                {
                    return ((RuntimeNamePropertyAttribute)attributes[0]).Name;
                }
                else if (attributeType == typeof(DictionaryKeyPropertyAttribute))
                {
                    return ((DictionaryKeyPropertyAttribute)attributes[0]).Name;
                }
                else if (attributeType == typeof(XamlSetMarkupExtensionAttribute))
                {
                    return ((XamlSetMarkupExtensionAttribute)attributes[0]).XamlSetMarkupExtensionHandler;
                }
                else if (attributeType == typeof(XamlSetTypeConverterAttribute))
                {
                    return ((XamlSetTypeConverterAttribute)attributes[0]).XamlSetTypeConverterHandler;
                }
                else if (attributeType == typeof(UidPropertyAttribute))
                {
                    return ((UidPropertyAttribute)attributes[0]).Name;
                }
                else if (attributeType == typeof(XmlLangPropertyAttribute))
                {
                    return ((XmlLangPropertyAttribute)attributes[0]).Name;
                }
                else
                {
                    Debug.Assert(attributeType == typeof(ConstructorArgumentAttribute), "Unexpected attribute type requested: " + attributeType.Name);
                    return ((ConstructorArgumentAttribute)attributes[0]).ArgumentName;
                }
            }
            try
            {
                // CustomAttributeData doesn't have an inherit=true option
                checkedInherited = false;

                CustomAttributeData cad = GetAttribute(attributeType);
                if (cad == null)
                {
                    return null;
                }
                return Extract<string>(cad) ?? string.Empty;
            }
            catch (CustomAttributeFormatException)
            {
                CustomAttributeProvider = Member;
                return GetAttributeString(attributeType, out checkedInherited);
            }
        }

        public IReadOnlyDictionary<char,char> GetBracketCharacterAttributes()
        {
            if (CustomAttributeProvider != null)
            {
                object[] attributes = CustomAttributeProvider.GetCustomAttributes(typeof(MarkupExtensionBracketCharactersAttribute), false);
                if (attributes.Length == 0)
                {
                    return null;
                }

                Dictionary<char, char> bracketCharacterAttributeList = new Dictionary<char, char>();
                foreach (object attribute in attributes)
                {
                    MarkupExtensionBracketCharactersAttribute bracketCharactersAttribute = (MarkupExtensionBracketCharactersAttribute)attribute;
                    bracketCharacterAttributeList.Add(bracketCharactersAttribute.OpeningBracket, bracketCharactersAttribute.ClosingBracket);
                }

                return new ReadOnlyDictionary<char, char>(bracketCharacterAttributeList);
            }

            return TokenizeBracketCharacters();
        }

        public T? GetAttributeValue<T>(Type attributeType) where T : struct
        {
            if (CustomAttributeProvider != null)
            {
                object[] attributes = CustomAttributeProvider.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0)
                {
                    return null;
                }

                if (attributeType == typeof(DesignerSerializationVisibilityAttribute))
                {
                    DesignerSerializationVisibility result = ((DesignerSerializationVisibilityAttribute)attributes[0]).Visibility;
                    return (T)(object)result;
                }
                else
                {
                    Debug.Assert(attributeType == typeof(UsableDuringInitializationAttribute), "Unexpected attribute type requested: " + attributeType.Name);
                    bool result = ((UsableDuringInitializationAttribute)attributes[0]).Usable;
                    return (T)(object)result;
                }
            }
            try
            {
                CustomAttributeData cad = GetAttribute(attributeType);
                if (cad == null)
                {
                    return null;
                }
                return Extract<T>(cad);
            }
            catch (CustomAttributeFormatException)
            {
                CustomAttributeProvider = Member;
                return GetAttributeValue<T>(attributeType);
            }
        }

        public Type GetAttributeType(Type attributeType)
        {
            if (CustomAttributeProvider != null)
            {
                object[] attributes = CustomAttributeProvider.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0)
                {
                    return null;
                }

                if (attributeType == typeof(TypeConverterAttribute))
                {
                    string typeName = ((TypeConverterAttribute)attributes[0]).ConverterTypeName;
                    return Type.GetType(typeName);
                }
                else if (attributeType == typeof(MarkupExtensionReturnTypeAttribute))
                {
                    return ((MarkupExtensionReturnTypeAttribute)attributes[0]).ReturnType;
                }
                else
                {
                    Debug.Assert(attributeType == typeof(ValueSerializerAttribute), "Unexpected attribute type requested: " + attributeType.Name);
                    return ((ValueSerializerAttribute)attributes[0]).ValueSerializerType;
                }
            }
            try
            {
                CustomAttributeData cad = GetAttribute(attributeType);
                if (cad == null)
                {
                    return null;
                }
                return ExtractType(cad);
            }
            catch (CustomAttributeFormatException)
            {
                CustomAttributeProvider = Member;
                return GetAttributeType(attributeType);
            }
        }

        public Type[] GetAttributeTypes(Type attributeType, int count)
        {
            if (CustomAttributeProvider != null)
            {
                object[] attributes = CustomAttributeProvider.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0)
                {
                    return null;
                }
                Debug.Assert(attributeType == typeof(XamlDeferLoadAttribute));
                Debug.Assert(count == 2);
                XamlDeferLoadAttribute tca = (XamlDeferLoadAttribute)attributes[0];
                Type converterType = Type.GetType(tca.LoaderTypeName);
                Type contentType = Type.GetType(tca.ContentTypeName);
                return new Type[] { converterType, contentType };
            }
            try
            {
                CustomAttributeData cad = GetAttribute(attributeType);
                if (cad == null)
                {
                    return null;
                }
                return ExtractTypes(cad, count);
            }
            catch (CustomAttributeFormatException)
            {
                CustomAttributeProvider = Member;
                return GetAttributeTypes(attributeType, count);
            }
        }

        public List<T> GetAllAttributeContents<T>(Type attributeType)
        {
            if (CustomAttributeProvider != null)
            {
                object[] attributes = CustomAttributeProvider.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0)
                {
                    return null;
                }
                List<T> result = new List<T>();

                if (attributeType == typeof(ContentWrapperAttribute))
                {
                    foreach (ContentWrapperAttribute attribute in attributes)
                    {
                        result.Add((T)(object)attribute.ContentWrapper);
                    }
                    return result;
                }
                else
                {
                    Debug.Assert(attributeType == typeof(DependsOnAttribute), "Unexpected attribute type requested: " + attributeType.Name);
                    foreach (DependsOnAttribute attribute in attributes)
                    {
                        result.Add((T)(object)attribute.Name);
                    }
                    return result;
                }
            }
            try
            {
                List<CustomAttributeData> cads = new List<CustomAttributeData>();
                GetAttributes(attributeType, cads);
                if (cads.Count == 0)
                {
                    return null;
                }
                List<T> types = new List<T>();
                foreach (CustomAttributeData cad in cads)
                {
                    T content = Extract<T>(cad);
                    types.Add((T)(object)content);
                }
                return types;
            }
            catch (CustomAttributeFormatException)
            {
                CustomAttributeProvider = Member;
                return GetAllAttributeContents<T>(attributeType);
            }
        }

        // This operates on a bitmask where:
        // - The lower 16 bits represent boolean values
        // - The upper 16 bits are valid bits
        // If the valid (high) bit is set, this returns the value of the low bit. If the valid bit
        // is not set, this returns null.
        protected static bool? GetFlag(int bitMask, int bitToCheck)
        {
            int validBit = GetValidMask(bitToCheck);
            if ((bitMask & validBit) != 0)
            {
                return (bitMask & bitToCheck) != 0;
            }
            return null;
        }

        protected static int GetValidMask(int flagMask)
        {
            // Make sure we're only using the low 16 bits for flags)
            Debug.Assert((flagMask & 0xFFFF) == flagMask, "flagMask should only use lower 16 bits of int");
            return flagMask << 16;
        }

        // Same expected bitmask layout as GetFlag.
        // This sets a low bit to the specified value, and sets the corresponding valid (high) bit to true.
        protected static void SetFlag(ref int bitMask, int bitToSet, bool value)
        {
            // This method cannot be used to clear a flag that has already been set
            Debug.Assert(value || (bitMask & bitToSet) == 0);

            int validMask = GetValidMask(bitToSet);
            int bitsToSet = validMask + (value ? bitToSet : 0);
            SetBit(ref bitMask, bitsToSet);
        }

        protected static void SetBit(ref int flags, int mask)
        {
            int oldValue;
            int newValue;
            bool updated;
            do
            {
                oldValue = flags;
                newValue = oldValue | mask;
                updated = oldValue == Interlocked.CompareExchange(ref flags, newValue, oldValue);
            }
            while (!updated);
        }

        private static bool TypesAreEqual(Type userType, Type builtInType)
        {
            if (userType.Assembly.ReflectionOnly)
            {
                return LooseTypeExtensions.AssemblyQualifiedNameEquals(userType, builtInType);
            }
            else
            {
                return userType == builtInType;
            }
        }

        private ReadOnlyDictionary<char, char> TokenizeBracketCharacters()
        {
            IList<CustomAttributeData> attrDataList = new List<CustomAttributeData>();
            GetAttributes(typeof(MarkupExtensionBracketCharactersAttribute), attrDataList);

            Dictionary<char, char> bracketCharacterList = new Dictionary<char, char>();
            foreach (CustomAttributeData attributeData in attrDataList)
            {
                char openingBracket = (char)(attributeData.ConstructorArguments[0].Value);
                char closingBracket = (char)(attributeData.ConstructorArguments[1].Value);
                bracketCharacterList.Add(openingBracket, closingBracket);
            }

            return new ReadOnlyDictionary<char, char>(bracketCharacterList);
        }

        private Type ExtractType(CustomAttributeData cad)
        {
            Type result = null;
            if (cad.ConstructorArguments.Count == 1)
            {
                result = ExtractType(cad.ConstructorArguments[0]);
            }
            if (result == null)
            {
                throw GetInvalidMetadataException(cad, 1, typeof(Type));
            }
            return result;
        }

        private Type[] ExtractTypes(CustomAttributeData cad, int count)
        {
            if (cad.ConstructorArguments.Count != count)
            {
                throw GetInvalidMetadataException(cad, count, typeof(Type));
            }
            Type[] result = new Type[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = ExtractType(cad.ConstructorArguments[i]);
                if (result[i] == null)
                {
                    throw GetInvalidMetadataException(cad, count, typeof(Type));
                }
            }
            return result;
        }

        private Type ExtractType(CustomAttributeTypedArgument arg)
        {
            if (arg.ArgumentType == typeof(Type))
            {
                return (Type)arg.Value;
            }
            else if (arg.ArgumentType == typeof(string))
            {
                string typeName = (string)arg.Value;
                return Type.GetType(typeName);
            }
            return null;
        }

        private T Extract<T>(CustomAttributeData cad)
        {
            if (cad.ConstructorArguments.Count == 0)
            {
                return default(T);
            }

            if (cad.ConstructorArguments.Count > 1 ||
                !TypesAreEqual(cad.ConstructorArguments[0].ArgumentType, typeof(T)))
            {
                throw GetInvalidMetadataException(cad, 1, typeof(T));
            }
            return (T)cad.ConstructorArguments[0].Value;
        }

        protected void EnsureAttributeData()
        {
            if (_attributeData == null)
            {
                _attributeData = CustomAttributeData.GetCustomAttributes(Member);
            }
        }

        private CustomAttributeData GetAttribute(Type attributeType)
        {
            EnsureAttributeData();
            for (int i = 0; i < _attributeData.Count; i++)
            {
                if (TypesAreEqual(_attributeData[i].Constructor.DeclaringType, attributeType))
                {
                    return _attributeData[i];
                }
            }
            return null;
        }

        private void GetAttributes(Type attributeType, IList<CustomAttributeData> cads)
        {
            EnsureAttributeData();
            for (int i = 0; i < _attributeData.Count; i++)
            {
                if (TypesAreEqual(_attributeData[i].Constructor.DeclaringType, attributeType))
                {
                    cads.Add(_attributeData[i]);
                }
            }
        }

        protected Exception GetInvalidMetadataException(CustomAttributeData cad, int expectedCount, Type expectedType)
        {
            return new XamlSchemaException(SR.Get(SRID.UnexpectedConstructorArg,
                cad.Constructor.DeclaringType, Member, expectedCount, expectedType));
        }
    }
}
