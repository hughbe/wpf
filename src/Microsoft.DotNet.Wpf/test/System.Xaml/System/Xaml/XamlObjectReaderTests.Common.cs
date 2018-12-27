// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Windows.Markup;

namespace System.Xaml.Tests
{
    public class MarkupExtensionBase : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => null;
    }

    public class InstanceDescriptorTypeConverterBase : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }
    }

    public class UnknownMemberInfo : MemberInfo
    {
        public override MemberTypes MemberType => throw new NotImplementedException();

        public override string Name => throw new NotImplementedException();

        public override Type DeclaringType => throw new NotImplementedException();

        public override Type ReflectedType => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
