// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Xaml.Tests.Common
{
    public class SubCustomAttributeData : CustomAttributeData
    {
        public SubCustomAttributeData() : base()
        {
        }

        public ConstructorInfo ConstructorResult { get; set; }
        public override ConstructorInfo Constructor => ConstructorResult;

        public IList<CustomAttributeTypedArgument> ConstructorArgumentsResult { get; set; }
        public override IList<CustomAttributeTypedArgument> ConstructorArguments => ConstructorArgumentsResult;
    }
}
