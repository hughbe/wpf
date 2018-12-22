// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xaml
{
    internal delegate void XamlNodeAddDelegate(XamlNodeType nodeType, object data);
    internal delegate void XamlLineInfoAddDelegate(int lineNumber, int linePosition);
    internal delegate XamlNode XamlNodeNextDelegate();     
    internal delegate XamlNode XamlNodeIndexDelegate(int idx);

    [DebuggerDisplay("{ToString()}")]
    internal struct XamlNode
    {
        internal enum InternalNodeType:byte { None, StartOfStream, EndOfStream, EndOfAttributes, LineInfo }

        private XamlNodeType _nodeType;
        private InternalNodeType _internalNodeType;
        private object _data;

        public XamlNodeType NodeType => _nodeType;

        public XamlNode(XamlNodeType nodeType)
        {
            Debug.Assert(nodeType == XamlNodeType.EndObject ||
                            nodeType == XamlNodeType.EndMember ||
                            nodeType == XamlNodeType.GetObject, "XamlNode ctor: Illegal node type");

            _nodeType = nodeType;
            _internalNodeType = InternalNodeType.None;
            _data = null;
        }

        public XamlNode(XamlNodeType nodeType, object data)
        {
            _nodeType = nodeType;
            _internalNodeType = InternalNodeType.None;
            _data = data;
        }

        public XamlNode(InternalNodeType internalNodeType)
        {
            Debug.Assert(internalNodeType == InternalNodeType.EndOfAttributes ||
                            internalNodeType == InternalNodeType.StartOfStream ||
                            internalNodeType == InternalNodeType.EndOfStream, "XamlNode ctor: Illegal Internal node type");
            _nodeType = XamlNodeType.None;
            _internalNodeType = internalNodeType;
            _data = null;
        }

        public XamlNode(LineInfo lineInfo)
        {
            _nodeType = XamlNodeType.None;
            _internalNodeType = InternalNodeType.LineInfo;
            _data = lineInfo;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            string str = String.Format(TypeConverterHelper.InvariantEnglishUS, "{0}: ", this.NodeType);
            switch(NodeType)
            {
            case XamlNodeType.StartObject:
                str += XamlType.Name;
                break;

            case XamlNodeType.StartMember:
                str += Member.Name;
                break;

            case XamlNodeType.Value:
                str += Value.ToString();
                break;

            case XamlNodeType.NamespaceDeclaration:
                str += NamespaceDeclaration.ToString();
                break;

            case XamlNodeType.None:
                switch(_internalNodeType)
                {
                case InternalNodeType.EndOfAttributes:
                    str += "End Of Attributes";
                    break;

                    case InternalNodeType.StartOfStream:
                    str += "Start Of Stream";
                    break;

                case InternalNodeType.EndOfStream:
                    str += "End Of Stream";
                    break;

                case InternalNodeType.LineInfo:
                    str += "LineInfo: " + LineInfo.ToString();
                    break;
                }
                break;
            }
            return str;
        }

        public NamespaceDeclaration NamespaceDeclaration
        {
            get
            {
                if (NodeType == XamlNodeType.NamespaceDeclaration)
                {
                    return (NamespaceDeclaration)_data;
                }
                return null;
            }
        }

        public XamlType XamlType
        {
            get => NodeType == XamlNodeType.StartObject ? (XamlType)_data : null;
        }

        public object Value
        {
            get => NodeType == XamlNodeType.Value ? _data : null;
        }

        public XamlMember Member
        {
            get => NodeType == XamlNodeType.StartMember ? (XamlMember)_data : null;
        }

        public LineInfo LineInfo
        {
            get => NodeType == XamlNodeType.None ? _data as LineInfo : null;
        }

        internal bool IsEof
        {
            get => NodeType == XamlNodeType.None && _internalNodeType == InternalNodeType.EndOfStream;
        }

        internal bool IsEndOfAttributes
        {
            get => NodeType == XamlNodeType.None && _internalNodeType == InternalNodeType.EndOfAttributes;
        }

        internal bool IsLineInfo
        {
            get => NodeType == XamlNodeType.None && _internalNodeType == InternalNodeType.LineInfo;
        }

        [ExcludeFromCodeCoverage]
        internal static bool IsEof_Helper(XamlNodeType nodeType, object data)
        {
            if (nodeType != XamlNodeType.None)
            {
                return false;
            }
            if (data is InternalNodeType internalNodeType)
            {
                if (internalNodeType == InternalNodeType.EndOfStream)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
