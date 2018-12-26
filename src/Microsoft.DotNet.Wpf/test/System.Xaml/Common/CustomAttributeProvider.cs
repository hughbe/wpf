// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomAttributeProvider : ICustomAttributeProvider
    {
        public object[] GetCustomAttributesResult { get; set; } = new object[0];

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return GetCustomAttributesResult;
        }

        public object[] GetCustomAttributes(bool inherit) => null;

        public bool IsDefinedResult { get; set; }

        public bool IsDefined(Type attributeType, bool inherit) => IsDefinedResult;
    }
}
