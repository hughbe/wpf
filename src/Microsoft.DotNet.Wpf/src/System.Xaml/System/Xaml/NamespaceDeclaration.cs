// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Xaml
{
    [DebuggerDisplay("Prefix={Prefix} Namespace={Namespace}")]
    public class NamespaceDeclaration
    {
        public NamespaceDeclaration(string ns, string prefix)
        {
            Namespace = ns;
            Prefix = prefix;
        }

        public string Namespace { get; }

        public string Prefix { get; }
    }
}
