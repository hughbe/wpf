// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    public abstract class XamlReader: IDisposable
    {
        public abstract bool Read();
        public abstract XamlNodeType NodeType { get; }
        public abstract bool IsEof { get; }

        public abstract NamespaceDeclaration Namespace { get; }
        public abstract XamlType Type { get; }
        public abstract object Value { get; }
        public abstract XamlMember Member { get; }

        public abstract XamlSchemaContext SchemaContext { get; }

        public virtual void Skip()
        {
            switch (NodeType)
            {
            case XamlNodeType.StartObject:
                SkipFromTo(XamlNodeType.StartObject, XamlNodeType.EndObject);
                break;
            case XamlNodeType.StartMember:
                SkipFromTo(XamlNodeType.StartMember, XamlNodeType.EndMember);
                break;
            }

            Read();
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        public void Close() => ((IDisposable)this).Dispose();

        public virtual XamlReader ReadSubtree() => new XamlSubreader(this);

        private void SkipFromTo(XamlNodeType startNodeType, XamlNodeType endNodeType)
        {
#if DEBUG
            if (NodeType != startNodeType)
            {
                throw new XamlInternalException("SkipFromTo() called incorrectly");
            }
#endif
            int depth = 1;
            while (depth > 0)
            {
                Read();
                XamlNodeType nodeType = NodeType;
                if (nodeType == startNodeType)
                {
                    depth += 1;
                }
                else if (nodeType == endNodeType)
                {
                    depth -= 1;
                }
            }
        }
    }
}
