﻿using System.Globalization;
using PListNet.Extensions;

namespace PListNet.Nodes;

/// <summary>
/// Represents a DateTime Value from a PList
/// </summary>
public sealed class DateNode : PNode<DateTime>
{
    /// <summary>
    /// Gets the Xml tag of this element.
    /// </summary>
    /// <value>The Xml tag of this element.</value>
    internal override string XmlTag => "date";

    /// <summary>
    /// Gets the binary typecode of this element.
    /// </summary>
    /// <value>The binary typecode of this element.</value>
    internal override byte BinaryTag => 3;

    internal override int BinaryLength => 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateNode"/> class.
    /// </summary>
    public DateNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateNode"/> class.
    /// </summary>
    /// <param name="value">The value of this element.</param>
    public DateNode(DateTime value)
        => Value = value;

    /// <summary>
    /// Parses the specified value from a given string, read from Xml.
    /// </summary>
    /// <param name="data">The string whis is parsed.</param>
    internal override void Parse(string data)
        => Value = DateTime.Parse(data, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the XML string representation of the Value.
    /// </summary>
    /// <returns>
    /// The XML string representation of the Value.
    /// </returns>
    internal override string ToXmlString()
        => Value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffZ");

    /// <summary>
    /// Reads this element binary from the reader.
    /// </summary>
    internal override void ReadBinary(Stream stream, int nodeLength)
    {
        var buf = new byte[1 << nodeLength];
        if (stream.Read(buf, 0, buf.Length) != buf.Length)
            throw new PListFormatException();

        var ticks = nodeLength switch
        {
            < 2 => throw new PListFormatException("Date < 32Bit"),
            2 => (double)buf.ToSingle(),
            3 => buf.ToDouble(),
            _ => throw new PListFormatException("Date > 64Bit"),
        };

        Value = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ticks);
    }

    /// <summary>
    /// Writes this element binary to the writer.
    /// </summary>
    internal override void WriteBinary(Stream stream)
    {
        var start = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        TimeSpan ts = Value - start;
        var buf = ts.TotalSeconds.GetBytes();
        stream.Write(buf, 0, buf.Length);
    }
}
