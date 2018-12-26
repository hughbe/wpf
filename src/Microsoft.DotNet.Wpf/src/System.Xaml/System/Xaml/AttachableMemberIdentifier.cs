// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    public class AttachableMemberIdentifier : IEquatable<AttachableMemberIdentifier>
    {
        public AttachableMemberIdentifier(Type declaringType, string memberName)
        {
            DeclaringType = declaringType;
            MemberName = memberName;
        }

        public Type DeclaringType { get; }

        public string MemberName { get; }

        public static bool operator !=(AttachableMemberIdentifier left, AttachableMemberIdentifier right)
        {
            return !(left == right);
        }

        public static bool operator ==(AttachableMemberIdentifier left, AttachableMemberIdentifier right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
    
            return left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AttachableMemberIdentifier);
        }

        public bool Equals(AttachableMemberIdentifier other)
        {
            if (other == null)
            {
                return false;
            }

            return DeclaringType == other.DeclaringType && MemberName == other.MemberName;
        }

        public override int GetHashCode()
        {
            int a = DeclaringType == null ? 0 : DeclaringType.GetHashCode();
            int b = MemberName == null ? 0 : MemberName.GetHashCode();
            return ((a << 5) + a) ^ b;
        }

        public override string ToString()
        {
            if (DeclaringType == null)
            {
                return MemberName;
            }

            return DeclaringType.ToString() + "." + MemberName;
        }
    }
}
