﻿using System.Buffers.Binary;
using PListNet.Extensions;
using PListNet.Nodes;

namespace PListNet.Internal;

/// <summary>
/// A class, used to write a <see cref="T:PListNet.PNode"/>  binary formated to a stream
/// </summary>
public class BinaryFormatWriter
{
    /// <summary>
    /// The Header (bplist00)
    /// </summary>
    private static readonly byte[] _header = [ 0x62, 0x70, 0x6c, 0x69, 0x73, 0x74, 0x30, 0x30 ];

    private readonly Dictionary<byte, Dictionary<PNode, int>> _uniqueElements = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryFormatWriter"/> class.
    /// </summary>
    internal BinaryFormatWriter()
    {
    }

    /// <summary>
    /// Writers a <see cref="T:PListNet.PNode"/> to the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="node">The PList node.</param>
    public void Write(Stream stream, PNode node)
    {
        stream.Write(_header, 0, _header.Length);

        var offsets = new List<int>();
        int nodeCount = GetNodeCount(node);

        byte nodeIndexSize;
        if (nodeCount <= byte.MaxValue) nodeIndexSize = sizeof(byte);
        else if (nodeCount <= short.MaxValue) nodeIndexSize = sizeof(short);
        else nodeIndexSize = sizeof(int);

        int topOffestIdx = WriteInternal(stream, nodeIndexSize, offsets, node);
        nodeCount = offsets.Count;

        int offsetTableOffset = (int) stream.Position;


        byte offsetSize;
        if (offsetTableOffset <= byte.MaxValue)
            offsetSize = sizeof(byte);
        else if (offsetTableOffset <= short.MaxValue)
            offsetSize = sizeof(short);
        else
            offsetSize = sizeof(int);

        for (int i = 0; i < offsets.Count; i++)
        {
            byte[] buf = offsetSize switch
            {
                1 => [ (byte) offsets[i] ],
                2 => ((short) offsets[i]).GetBytes(),
                4 => offsets[i].GetBytes(),
                _ => null
            };

            stream.Write(buf, 0, buf.Length);
        }

        var header = new byte[32];
        header[6] = offsetSize;
        header[7] = nodeIndexSize;

        nodeCount.GetBytes().CopyTo(header, 12);
        topOffestIdx.GetBytes().CopyTo(header, 20);
        offsetTableOffset.GetBytes().CopyTo(header, 28);

        stream.Write(header, 0, header.Length);
    }

    /// <summary>
    /// Writers a <see cref="T:PListNet.PNode"/> to the current stream position
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="nodeIndexSize">The node index size.</param>
    /// <param name="offsets">Node offsets.</param>
    /// <param name="node">The PList node.</param>
    /// <returns>The Idx of the written node</returns>
    internal int WriteInternal(Stream stream, byte nodeIndexSize, List<int> offsets, PNode node)
    {
        int elementIdx = offsets.Count;
        if (node.IsBinaryUnique && node is IEquatable<PNode>)
        {
            if (!_uniqueElements.ContainsKey(node.BinaryTag))
                _uniqueElements.Add(node.BinaryTag, []);

            if (_uniqueElements[node.BinaryTag].ContainsKey(node))
            {
                if (node is BooleanNode)
                    elementIdx = _uniqueElements[node.BinaryTag][node];
                else
                    return _uniqueElements[node.BinaryTag][node];
            }
            else
            {
                _uniqueElements[node.BinaryTag][node] = elementIdx;
            }
        }

        int offset = (int) stream.Position;
        offsets.Add(offset);
        int len = node.BinaryLength;
        var typeCode = (byte) (node.BinaryTag << 4 | (len < 0x0F ? len : 0x0F));
        stream.WriteByte(typeCode);
        if (len >= 0x0F)
        {
            var extLen = NodeFactory.CreateLengthElement(len);
            var binaryTag = (byte) (extLen.BinaryTag << 4 | extLen.BinaryLength);
            stream.WriteByte(binaryTag);
            extLen.WriteBinary(stream);
        }

        if (node is ArrayNode arrayNode)
        {
            WriteInternal(stream, nodeIndexSize, offsets, arrayNode);
            return elementIdx;
        }

        if (node is DictionaryNode dictionaryNode)
        {
            WriteInternal(stream, nodeIndexSize, offsets, dictionaryNode);
            return elementIdx;
        }

        node.WriteBinary(stream);
        return elementIdx;
    }

    private void WriteInternal(Stream stream, byte nodeIndexSize, List<int> offsets, ArrayNode array)
    {
        var nodes = new byte[nodeIndexSize * array.Count];
        var streamPos = stream.Position;

        stream.Write(nodes, 0, nodes.Length);
        for (int i = 0; i < array.Count; i++)
        {
            int elementIdx = WriteInternal(stream, nodeIndexSize, offsets, array[i]);
            FormatIdx(elementIdx, nodeIndexSize).CopyTo(nodes, nodeIndexSize * i);
        }

        stream.Seek(streamPos, SeekOrigin.Begin);
        stream.Write(nodes, 0, nodes.Length);
        stream.Seek(0, SeekOrigin.End);
    }

    private void WriteInternal(Stream stream, byte nodeIndexSize, List<int> offsets, DictionaryNode dictionary)
    {
        var keys = new byte[nodeIndexSize * dictionary.Count];
        var values = new byte[nodeIndexSize * dictionary.Count];
        long streamPos = stream.Position;
        stream.Write(keys, 0, keys.Length);
        stream.Write(values, 0, values.Length);

        KeyValuePair<string, PNode>[] elems = dictionary.ToArray();

        for (int i = 0; i < dictionary.Count; i++)
        {
            int elementIdx = WriteInternal(stream, nodeIndexSize, offsets, NodeFactory.CreateKeyElement(elems[i].Key));
            FormatIdx(elementIdx, nodeIndexSize).CopyTo(keys, nodeIndexSize * i);
        }
        for (int i = 0; i < dictionary.Count; i++)
        {
            int elementIdx = WriteInternal(stream, nodeIndexSize, offsets, elems[i].Value);
            FormatIdx(elementIdx, nodeIndexSize).CopyTo(values, nodeIndexSize * i);
        }

        stream.Seek(streamPos, SeekOrigin.Begin);
        stream.Write(keys, 0, keys.Length);
        stream.Write(values, 0, values.Length);
        stream.Seek(0, SeekOrigin.End);
    }

    private static int GetNodeCount(PNode node)
    {
        if (node == null)
            throw new ArgumentNullException("node");

        // special case: array
        if (node is ArrayNode array)
        {
            var count = 1;

            foreach (var subNode in array)
                count += GetNodeCount(subNode);

            return count;
        }

        // special case: dictionary
        if (node is DictionaryNode dictionary)
        {
            var count = 1;

            foreach (var subNode in dictionary.Values)
                count += GetNodeCount(subNode);

            count += dictionary.Keys.Count;
            return count;
        }

        // normal case
        return 1;
    }

    /// <summary>
    /// Formats a node index based on the size.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="nodeIndexSize">The node index size.</param>
    /// <returns>The formated idx.</returns>
    private static byte[] FormatIdx(int index, byte nodeIndexSize)
    {
        var buf = new byte[nodeIndexSize];

        switch (nodeIndexSize)
        {
            case 1:
                buf[0] = (byte) index;
                break;
            case 2:
                BinaryPrimitives.WriteInt16BigEndian(buf, (short) index);
                break;
            case 4:
                BinaryPrimitives.WriteInt32BigEndian(buf, index);
                break;
            default:
                throw new PListFormatException("Invalid node index size");
        }

        return buf;
    }
}
