// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

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
#if DEBUG
            switch (nodeType)
            {
            case XamlNodeType.EndObject:
            case XamlNodeType.EndMember:
            case XamlNodeType.GetObject:
                break;

            default:
                Debug.Assert(false, "XamlNode Ctor missing data argument");
                break;
            }
#endif
            _nodeType = nodeType;
            _internalNodeType = InternalNodeType.None;
            _data = null;
        }

        public XamlNode(XamlNodeType nodeType, object data)
        {
#if DEBUG
            switch(nodeType)
            {
            case XamlNodeType.StartObject:
                Debug.Assert(data is XamlType, "XamlNode ctor, StartObject data is not a XamlType");
                break;

            case XamlNodeType.StartMember:
                Debug.Assert(data is XamlMember, "XamlNode ctor, StartMember data is not a XamlMember");
                break;

            case XamlNodeType.NamespaceDeclaration:
                Debug.Assert(data is NamespaceDeclaration, "XamlNode ctor, NamespaceDeclaration data is not a NamespaceDeclaration");
                break;

            case XamlNodeType.Value:
                // can be anything;
                break;

            case XamlNodeType.EndObject:
            case XamlNodeType.EndMember:
            case XamlNodeType.GetObject:
                Debug.Assert(data == null, "XamlNode ctor, Internal XamlNode data must be null for this Node type");
                break;

            default:
                Debug.Assert(false, "XamlNode ctor, incorrect ctor called.");
                break;
            }
#endif
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
