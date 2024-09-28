using System;
using System.Globalization;
using System.IO;
using PListNet.Extensions;

namespace PListNet.Nodes;

/// <summary>
/// Represents an integer Value from a PList
/// </summary>
public class IntegerNode : PNode<long>
{
    /// <summary>
    /// Gets the Xml tag of this element.
    /// </summary>
    /// <value>The Xml tag of this element.</value>
    internal override string XmlTag => "integer";

    /// <summary>
    /// Gets the binary typecode of this element.
    /// </summary>
    /// <value>The binary typecode of this element.</value>
    internal override byte BinaryTag => 1;

    /// <summary>
    /// Gets the length of this PList element.
    /// </summary>
    /// <returns>The length of this PList element.</returns>
    /// <remarks>Provided for internal use only.</remarks>
    internal override int BinaryLength
    => Value switch
        {
            >= byte.MinValue and <= byte.MaxValue => 0,
            >= short.MinValue and <= short.MaxValue => 1,
            >= int.MinValue and <= int.MaxValue => 2,
            >= long.MinValue and <= long.MaxValue => 3,
        };

    /// <summary>
    /// Gets or sets the value of this element.
    /// </summary>
    /// <value>The value of this element.</value>
    public override long Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerNode"/> class.
    /// </summary>
    public IntegerNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerNode"/> class.
    /// </summary>
    /// <param name="value">The value of this element.</param>
    public IntegerNode(long value)
        => Value = value;

    /// <summary>
    /// Parses the specified value from a given string, read from Xml.
    /// </summary>
    /// <param name="data">The string whis is parsed.</param>
    internal override void Parse(string data)
        => Value = long.Parse(data, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the XML string representation of the Value.
    /// </summary>
    /// <returns>
    /// The XML string representation of the Value.
    /// </returns>
    internal override string ToXmlString()
        => Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads the binary stream.
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="nodeLength">Node length.</param>
    internal override void ReadBinary(Stream stream, int nodeLength)
    {
        var buf = new byte[1 << nodeLength];

        if (stream.Read(buf, 0, buf.Length) != buf.Length)
            throw new PListFormatException();

        Value = nodeLength switch
        {
            0 => buf[0],
            1 => buf.ToInt16(),
            2 => buf.ToInt32(),
            3 => buf.ToInt64(),
            _ => throw new PListFormatException("Int > 64Bit"),
        };
    }

    /// <summary>
    /// Writes this element binary to the writer.
    /// </summary>
    internal override void WriteBinary(Stream stream)
    {
        byte[] buf = BinaryLength switch
        {
            0 => [(byte)Value],
            1 => ((short)Value).GetBytes(),
            2 => ((int)Value).GetBytes(),
            3 => Value.GetBytes(),
            _ => throw new Exception($"Unexpected length: {BinaryLength}."),
        };

        stream.Write(buf, 0, buf.Length);
    }
}
