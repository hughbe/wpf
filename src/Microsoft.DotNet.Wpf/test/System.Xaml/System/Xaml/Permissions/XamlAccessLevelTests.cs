// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Xaml.Permissions.Tests
{
    public class XamlAccessLevelTests
    {
        [Fact]
        public void AssemblyAccessTo_Assembly_ReturnsExpected()
        {
            XamlAccessLevel access = XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly);
            Assert.Equal(typeof(int).Assembly.FullName, access.AssemblyAccessToAssemblyName.FullName);
            Assert.Null(access.PrivateAccessToTypeName);
        }

        [Fact]
        public void AssemblyAccessTo_NullAssembly_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("assembly", () => XamlAccessLevel.AssemblyAccessTo((Assembly)null));
        }

        [Fact]
        public void AssemblyAccessTo_AssemblyName_ReturnsExpected()
        {
            var assemblyName = new AssemblyName(typeof(int).Assembly.FullName);
            XamlAccessLevel access = XamlAccessLevel.AssemblyAccessTo(assemblyName);
            Assert.Equal(assemblyName.FullName, access.AssemblyAccessToAssemblyName.FullName);
            Assert.Null(access.PrivateAccessToTypeName);
        }

        [Fact]
        public void AssemblyAccessTo_NullAssemblyName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("assemblyName", () => XamlAccessLevel.AssemblyAccessTo((AssemblyName)null));
        }

        public static IEnumerable<object[]> InvalidAssemblyName_TestData()
        {
            AssemblyName SetPublicKeyToken(AssemblyName name)
            {
                name.SetPublicKeyToken(new byte[] { 1, 2, 3 });
                return name;
            }

            yield return new object[] { new AssemblyName() };
            yield return new object[]
            {
                SetPublicKeyToken(new AssemblyName
                {
                    Name = null,
                    Version = new Version(1, 2),
                    CultureInfo = CultureInfo.InvariantCulture,
                })
            };
            yield return new object[]
            {
                SetPublicKeyToken(new AssemblyName
                {
                    Name = "name",
                    Version = null,
                    CultureInfo = CultureInfo.InvariantCulture,
                })
            };
            yield return new object[]
            {
                SetPublicKeyToken(new AssemblyName
                {
                    Name = "name",
                    Version = new Version(1, 2),
                    CultureInfo = null,
                })
            };
            yield return new object[]
            {
                new AssemblyName
                {
                    Name = "name",
                    Version = new Version(1, 2),
                    CultureInfo = CultureInfo.InvariantCulture,
                }
            };
        }

        [Theory]
        [MemberData(nameof(InvalidAssemblyName_TestData))]
        public void AssemblyAccessTo_InvalidAssemblyName_ThrowsArgumentException(AssemblyName assemblyName)
        {
            Assert.Throws<ArgumentException>("assemblyName", () => XamlAccessLevel.AssemblyAccessTo(assemblyName));
        }

        [Fact]
        public void PrivateAccessTo_Type_ReturnsExpected()
        {
            XamlAccessLevel access = XamlAccessLevel.PrivateAccessTo(typeof(int));
            Assert.Equal(typeof(int).Assembly.FullName, access.AssemblyAccessToAssemblyName.FullName);
            Assert.Equal(typeof(int).FullName, access.PrivateAccessToTypeName);
        }

        [Fact]
        public void PrivateAccessTo_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("type", () => XamlAccessLevel.PrivateAccessTo((Type)null));
        }

        [Fact]
        public void PrivateAccessTo_String_ReturnsExpected()
        {
            XamlAccessLevel access = XamlAccessLevel.PrivateAccessTo(typeof(int).AssemblyQualifiedName);
            Assert.Equal(typeof(int).Assembly.FullName, access.AssemblyAccessToAssemblyName.FullName);
            Assert.Equal(typeof(int).FullName, access.PrivateAccessToTypeName);
        }

        [Fact]
        public void PrivateAccessTo_NullAssemblyQualifiedTypeName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("assemblyQualifiedTypeName", () => XamlAccessLevel.PrivateAccessTo((string)null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("assemblyQualifiedTypeName")]
        public void PrivateAccessTo_InvalidAssemblyQualfiiedTypeName_ThrowsArgumentException(string assemblyQualifiedName)

        {
            Assert.Throws<ArgumentException>("assemblyQualifiedTypeName", () => XamlAccessLevel.PrivateAccessTo(assemblyQualifiedName));
        }
    }
}