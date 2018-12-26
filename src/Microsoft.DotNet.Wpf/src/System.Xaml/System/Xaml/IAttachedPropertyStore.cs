// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Xaml
{
    public interface IAttachedPropertyStore
    {
        /// <summary>
        /// The number of properties currently attached to this instance
        /// </summary>
        int PropertyCount { get; }

        /// <summary>
        /// Retrieve the set of attached properties for this instance. This is
        /// a copy of the current set of properties.
        /// </summary>
        void CopyPropertiesTo(KeyValuePair<AttachableMemberIdentifier, object>[] array, int index);

        /// <summary>
        /// Remove the property 'name' from this instance. If the property doesn't
        /// currently exist this returns false.
        /// </summary>
        bool RemoveProperty(AttachableMemberIdentifier attachableMemberIdentifier);

        /// <summary>
        /// Set the property 'name' to 'value' for this instance. If the property
        /// doesn't currently exist on this instance it will be created.
        /// </summary>
        void SetProperty(AttachableMemberIdentifier attachableMemberIdentifier, object value);
        
        /// <summary>
        /// Retrieve the value of the attached property 'name' for this instance.
        /// If there is not an attached property defined for this instance with
        /// this 'name' then returns false. If the value of the attached property
        /// for this instance with this 'name' cannot be cast to T then returns
        /// false.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1007")]
        bool TryGetProperty(AttachableMemberIdentifier attachableMemberIdentifier, out object value);
    }
}
