// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml.Tests.Common
{
    public class CustomRootObjectProvider : IRootObjectProvider
    {
        public object RootObjectResult { get; set; }

        public object RootObject => RootObjectResult;
    }
}
