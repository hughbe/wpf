// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Xaml
{
    /// </summary>
    //// Provides a place to Write a list of Xaml nodes
    /// and Read them back without exposing the 'XamlNode' type.
    /// Single Writer, multiple reader.
    /// Must complete writing and Close, before reading.
    /// </summary>
    public class XamlNodeList
    {
        private List<XamlNode> _nodeList;
        private bool _readMode = false;
        private readonly XamlWriter _writer;
        private bool _hasLineInfo;

        public XamlNodeList(XamlSchemaContext schemaContext)
        {
            if (schemaContext == null)
            {
                throw new ArgumentNullException(nameof(schemaContext));
            }

            _nodeList = new List<XamlNode>();
            _writer = new WriterDelegate(Add, AddLineInfo, schemaContext);
        }

        public XamlNodeList(XamlSchemaContext schemaContext, int size)
        {
            if (schemaContext == null)
            {
                throw new ArgumentNullException(nameof(schemaContext));
            }

            _nodeList = size == 0 ? new List<XamlNode>() : new List<XamlNode>(size);
            _writer = new WriterDelegate(Add, AddLineInfo, schemaContext);
        }

        public XamlWriter Writer => _writer;

        public XamlReader GetReader()
        {
            if (!_readMode)
            {
                throw new XamlException(SR.Get(SRID.CloseXamlWriterBeforeReading));
            }

            Debug.Assert(_writer.SchemaContext != null, "SchemaContext is validated in the constructor.");
            return new ReaderMultiIndexDelegate(_writer.SchemaContext, Index, _nodeList.Count, _hasLineInfo);
        }

        private void Add(XamlNodeType nodeType, object data)
        {
            if (!_readMode)
            {
                if (nodeType != XamlNodeType.None)
                {
                    XamlNode node = new XamlNode(nodeType, data);
                    _nodeList.Add(node);
                    return;
                }
                Debug.Assert(XamlNode.IsEof_Helper(nodeType, data));
                _readMode = true;
            }
            else
            {
                throw new XamlException(SR.Get(SRID.CannotWriteClosedWriter));
            }
        }

        private void AddLineInfo(int lineNumber, int linePosition)
        {
            if (_readMode)
            {
                throw new XamlException(SR.Get(SRID.CannotWriteClosedWriter));
            }

            XamlNode node = new XamlNode(new LineInfo(lineNumber, linePosition));
            _nodeList.Add(node);
            if (!_hasLineInfo)
            {
                _hasLineInfo = true;
            }
        }

        private XamlNode Index(int idx)
        {
            if (!_readMode)
            {
                throw new XamlException(SR.Get(SRID.CloseXamlWriterBeforeReading));
            }

            return _nodeList[idx];
        }
              
        public void Clear()
        {
            _nodeList.Clear();
            _readMode = false;      // go back to write mode.
        }

        public int Count => _nodeList.Count;
    }
}
