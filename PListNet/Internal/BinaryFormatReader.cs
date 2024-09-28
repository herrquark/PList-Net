using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PListNet.Extensions;
using PListNet.Nodes;

namespace PListNet.Internal;

/// <summary>
/// A class, used to read binary formated <see cref="T:PListNet.PNode"/> from a stream
/// </summary>
internal class BinaryFormatReader
{
    /// <summary>
    /// Reads a binary formated <see cref="T:PListNet.PNode"/> from the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>The <see cref="T:PListNet.PNode"/>, read from the specified stream</returns>
    public PNode Read(Stream stream)
    {
        // reference material:
        // - https://medium.com/@karaiskc/understanding-apples-binary-property-list-format-281e6da00dbd

        // read in file header and verify expected bits are found
        ValidatePListFileHeader(stream);

        // read in file trailer
        var trailer = ReadTrailer(stream);

        // read in node offsets
        var nodeOffsets = ReadNodeOffsets(stream, trailer);

        var readerState = new ReaderState(stream, nodeOffsets, trailer.OffsetIntSize, trailer.ObjectRefSize);

        return ReadInternal(readerState, trailer.TopObject);
    }

    private void ValidatePListFileHeader(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);

        var buffer = new byte[8];
        if (stream.Read(buffer, 0, buffer.Length) != buffer.Length) throw new PListFormatException("Invalid plist file: must start with 8-byte header.");

        // get first 6 bytes and match to expected text, "bplist"
        var text = Encoding.UTF8.GetString(buffer, 0, 6);
        if (text != "bplist") throw new PListFormatException("Invalid plist file: must start with string \"bplist\".");

        // TODO: get version (ASCII numbers in bytes 7 and 8) and pass back to the parser
    }

    private static PListTrailer ReadTrailer(Stream stream)
    {
        // trailer is 32 bytes long, at the end of the file
        var buffer = new byte[32];
        stream.Seek(-32, SeekOrigin.End);

        if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
            throw new PListFormatException("Invalid plist file: unable to read trailer.");

        // all data in a binary plist file is big-endian
        var trailer = new PListTrailer
        {
            Unused = new byte[5],
            SortVersionl = buffer[5],
            OffsetIntSize = buffer[6],
            ObjectRefSize = buffer[7],
            NumObjects = buffer.ToUInt64(8),
            TopObject = buffer.ToUInt64(16),
            OffsetTableOffset = buffer.ToUInt64(24)
        };

        return trailer;
    }

    /// <summary>
    ///		Read in offsets. Converting to Int32 because .NET Stream.Read method takes Int32s.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="trailer"></param>
    /// <returns></returns>
    private static int[] ReadNodeOffsets(Stream stream, PListTrailer trailer)
    {
        // the bitconverter library we use only knows how to deal with integer offsets
        if (trailer.NumObjects > int.MaxValue) throw new PListFormatException($"Offset table contains too many entries: {trailer.NumObjects}.");

        // position the stream at the start of the offset table
        if (stream.Seek((long) trailer.OffsetTableOffset, SeekOrigin.Begin) != (long) trailer.OffsetTableOffset)
            throw new PListFormatException("Invalid plist file: unable to seek to start of the offset table.");

        var offsetSize = trailer.OffsetIntSize;
        var buffer = new byte[offsetSize];
        var nodeOffsets = new int[trailer.NumObjects];

        for (ulong i = 0; i < trailer.NumObjects; i++)
        {
            if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new PListFormatException($"Invalid plist file: unable to read value {i} in the offset table.");

            nodeOffsets[i] = ReadNumber(buffer);
        }

        return nodeOffsets;
    }

    private static int ReadNumber(byte[] buffer)
        => buffer.Length switch
        {
            1 => buffer[0],
            2 => buffer.ToUInt16(),
            4 => (int)buffer.ToUInt32(),
            8 => (int)buffer.ToUInt64(),
            _ => throw new PListFormatException($"Unexpected offset int size: {buffer.Length}."),
        };

    /// <summary>
    /// Reads the <see cref="T:PListNet.PNode"/> at the specified idx.
    /// </summary>
    /// <param name="readerState">Reader state.</param>
    /// <param name="elemIdx">The elem idx.</param>
    /// <returns>The <see cref="T:PListNet.PNode"/> at the specified idx.</returns>
    private PNode ReadInternal(ReaderState readerState, ulong elemIdx)
    {
        readerState.Stream.Seek(readerState.NodeOffsets[elemIdx], SeekOrigin.Begin);
        return ReadInternal(readerState);
    }

    /// <summary>
    /// Reads the <see cref="T:PListNet.PNode"/> at the current stream position.
    /// </summary>
    /// <param name="readerState">Reader state.</param>
    /// <returns>The <see cref="T:PListNet.PNode"/> at the current stream position.</returns>
    private PNode ReadInternal(ReaderState readerState)
    {
        var tagAndLength = GetObjectLengthAndTag(readerState.Stream);

        var tag = tagAndLength.Tag;
        var objectLength = tagAndLength.Length;

        var node = NodeFactory.Create(tag, objectLength);

        // array and dictionary are special-cased here
        // while primitives handle their own loading
        if (node is ArrayNode arrayNode)
        {
            ReadInArray(arrayNode, objectLength, readerState);
            return node;
        }

        if (node is DictionaryNode dictionaryNode)
        {
            ReadInDictionary(dictionaryNode, objectLength, readerState);
            return node;
        }

        node.ReadBinary(readerState.Stream, objectLength);

        return node;
    }

    private static NodeTagAndLength GetObjectLengthAndTag(Stream stream)
    {
        // read the marker byte
        // left 4 bits represent the tag, which indicates the node type
        // right 4 bits indicate the length
        //  - if size fits in 4 bits, the number is the length
        //  - if the bit value is 1111, the following byte will contain information needed to decode length as follows:
        //      - 4 left bits are 0001
        //      - 4 right bits is the power of 2 required to represent the length
        //      - the following pow(2, x) bytes give us the length (big-endian)
        var buf = new byte[1];

        if (stream.Read(buf, 0, buf.Length) != buf.Length)
            throw new PListFormatException("Couldn't read node tag byte.");

        byte tag = (byte) ((buf[0] >> 4) & 0x0F);
        var length = buf[0] & 0x0F;

        // length fits in 4 bits, return
        if (length != 0xF)
            return new NodeTagAndLength(tag, length);

        // read next byte to determine the length (in bytes) of actual length value
        if (stream.Read(buf, 0, buf.Length) != buf.Length)
            throw new PListFormatException("Couldn't read node length byte.");

        // verify that leftmost bits are 0001
        if (((buf[0] >> 4) & 0x0F) != 0x1)
            throw new PListFormatException("Invalid node length byte header.");

        // get the rightmost bits, giving us the number of bytes (power of 2) that we need
        var byteCount = (int) Math.Pow(2, buf[0] & 0x0F);

        // now get the length
        var lengthBuffer = new byte[byteCount];
        if (stream.Read(lengthBuffer, 0, lengthBuffer.Length) != lengthBuffer.Length)
            throw new PListFormatException("Couldn't read node length byte(s).");

        length = ReadNumber(lengthBuffer);

        return new NodeTagAndLength(tag, length);
    }

    private void ReadInArray(ICollection<PNode> node, int nodeLength, ReaderState readerState)
    {
        var buf = new byte[nodeLength * readerState.ObjectRefSize];

        if (readerState.Stream.Read(buf, 0, buf.Length) != buf.Length)
            throw new PListFormatException();

        for (var i = 0; i < nodeLength; i++)
        {
            var topNode = GetNodeOffset(readerState, buf, i);
            node.Add(ReadInternal(readerState, topNode));
        }
    }

    private void ReadInDictionary(IDictionary<string, PNode> node, int nodeLength, ReaderState readerState)
    {
        var bufKeys = new byte[nodeLength * readerState.ObjectRefSize];
        var bufVals = new byte[nodeLength * readerState.ObjectRefSize];

        if (readerState.Stream.Read(bufKeys, 0, bufKeys.Length) != bufKeys.Length)
            throw new PListFormatException();

        if (readerState.Stream.Read(bufVals, 0, bufVals.Length) != bufVals.Length)
            throw new PListFormatException();

        for (var i = 0; i < nodeLength; i++)
        {
            var topNode = GetNodeOffset(readerState, bufKeys, i);
            var plKey = ReadInternal(readerState, topNode);

            if (plKey is not StringNode stringKey)
                throw new PListFormatException("Key is not a string");

            topNode = GetNodeOffset(readerState, bufVals, i);
            var plVal = ReadInternal(readerState, topNode);

            node.Add(stringKey.Value, plVal);
        }
    }

    private static ulong GetNodeOffset(ReaderState readerState, byte[] bufKeys, int index)
        => readerState.ObjectRefSize switch
        {
            1 => bufKeys[index],
            2 => BinaryPrimitives.ReadUInt16BigEndian(bufKeys.AsSpan(readerState.ObjectRefSize * index)),
            4 => BinaryPrimitives.ReadUInt32BigEndian(bufKeys.AsSpan(readerState.ObjectRefSize * index)),
            8 => BinaryPrimitives.ReadUInt64BigEndian(bufKeys.AsSpan(readerState.ObjectRefSize * index)),
            _ => throw new PListFormatException("$Unexpected index size: {readerState.IndexSize}."),
        };

    public class ReaderState(Stream stream, int[] nodeOffsets, int indexSize, int objectRefSize)
    {
        public Stream Stream { get; } = stream;
        public int[] NodeOffsets { get; } = nodeOffsets;
        public int OffsetIntSize { get; } = indexSize;
        public int ObjectRefSize { get; } = objectRefSize;
    }
}
