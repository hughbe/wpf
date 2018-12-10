// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xaml
{
    internal class XamlSubreader : XamlReader, IXamlLineInfo
    {
        private XamlReader _reader;
        private IXamlLineInfo _lineInfoReader;
        private bool _done;
        private bool _firstRead;
        private bool _rootIsStartMember;
        private int _depth;

        public XamlSubreader(XamlReader reader)
        {
            _reader = reader;
            _lineInfoReader = reader as IXamlLineInfo;
            _done = false;
            _depth = 0;
            _firstRead = true;
            _rootIsStartMember = (reader.NodeType == XamlNodeType.StartMember);
        }

        public override bool Read()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(XamlReader));  // can't say "XamlSubreader" it's an internal class.
            }
            if (!_firstRead)
            {
                return LimitedRead();
            }
            _firstRead = false;
            return true;
        }

        private bool IsEmpty => _done || _firstRead;

        public override XamlNodeType NodeType
        {
            get => IsEmpty ? XamlNodeType.None : _reader.NodeType;
        }

        public override bool IsEof => IsEmpty || _reader.IsEof;

        public override NamespaceDeclaration Namespace
        {
            get => IsEmpty ? null : _reader.Namespace;
        }

        public override XamlType Type => IsEmpty ? null : _reader.Type;

        public override object Value => IsEmpty ? null : _reader.Value;

        public override XamlMember Member => IsEmpty ? null : _reader.Member;

        public override XamlSchemaContext SchemaContext => _reader.SchemaContext;

        public bool HasLineInfo => _lineInfoReader?.HasLineInfo ?? false;

        public int LineNumber => _lineInfoReader?.LineNumber ?? 0;

        public int LinePosition => _lineInfoReader?.LinePosition ?? 0;

        private bool LimitedRead()
        {
            if (IsEof)
            {
                return false;
            }

            XamlNodeType nodeType = _reader.NodeType;

            if (_rootIsStartMember)
            {
                if (nodeType == XamlNodeType.StartMember)
                {
                    _depth += 1;
                }
                else if (nodeType == XamlNodeType.EndMember)
                {
                    _depth -= 1;
                }
            }
            else
            {
                if (nodeType == XamlNodeType.StartObject
                    || nodeType == XamlNodeType.GetObject)
                {
                    _depth += 1;
                }
                else if (nodeType == XamlNodeType.EndObject)
                {
                    _depth -= 1;
                }
            }

            if (_depth == 0)
            {
                _done = true;
            }
            _reader.Read();
            return !IsEof;
        }
    }
}
