// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests.Common
{
    public class CustomXamlSchemaContext : XamlSchemaContext
    {
        public Optional<Type> ExpectedGetXamlType { get; set; }
        public XamlType GetXamlTypeResult { get; set; }

        public override XamlType GetXamlType(Type type)
        {
            if (ExpectedGetXamlType.HasValue)
            {
                Assert.Equal(ExpectedGetXamlType, type);
            }

            return GetXamlTypeResult;
        }
    }
}
