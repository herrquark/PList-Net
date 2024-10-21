using System.Text;
using System.Xml;
using PlistNet.Extensions;
using XmlTools;

namespace PListNet.Nodes;

/// <summary>
/// Represents an string Value from a PList
/// </summary>
public class StringNode : PNode<string>
{
    private static readonly byte[] _utf8Bytes = Enumerable.Range(0, 256).Select(i => (byte) i).ToArray();

    private static readonly HashSet<char> _utf8Chars = new(Encoding.UTF8.GetChars(_utf8Bytes));

    private string _value;

    /// <summary>
    /// Gets the Xml tag of this element.
    /// </summary>
    /// <value>The Xml tag of this element.</value>
    internal override string XmlTag => "string";

    /// <summary>
    /// Gets the binary typecode of this element.
    /// </summary>
    /// <value>The binary typecode of this element.</value>
    internal override byte BinaryTag => (byte) (IsUtf16 ? 6 : 5);

    /// <summary>
    /// Gets the length of this PList element.
    /// </summary>
    /// <returns>The length of this PList element.</returns>
    internal override int BinaryLength => Value.Length;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is UTF16.
    /// </summary>
    /// <value><c>true</c> if this instance is UTF16; otherwise, <c>false</c>.</value>
    internal bool IsUtf16 { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringNode"/> class.
    /// </summary>
    public StringNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringNode"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    public StringNode(string value)
        => Value = value;

    /// <summary>
    /// Gets or sets the value of this element.
    /// </summary>
    /// <value>The value of this element.</value>
    public sealed override string Value
    {
        get => _value;
        set
        {
            _value = value;

            //Detect Encoding
            foreach (char c in value)
            {
                if (!_utf8Chars.Contains(c))
                {
                    IsUtf16 = true;
                    return;
                }
            }

            IsUtf16 = false;
        }
    }

    /// <summary>
    /// Parses the specified value from a given string, read from Xml.
    /// </summary>
    /// <param name="data">The string whis is parsed.</param>
    internal override void Parse(string data)
        => Value = data;

    internal override void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement(XmlTag);
        writer.WriteValue(ToXmlString());
        writer.WriteEndElement();
    }

    internal override void WriteXml(LightXmlWriter writer, int indent = 0)
        => writer.WriteElementLineWithValue(XmlTag, ToXmlString(), indent);

    /// <summary>
    /// Gets the XML string representation of the Value.
    /// </summary>
    /// <returns>
    /// The XML string representation of the Value.
    /// </returns>
    internal override string ToXmlString()
        => Value;

    /// <summary>
    /// Reads this element binary from the reader.
    /// </summary>
    internal override void ReadBinary(Stream stream, int nodeLength)
    {
        var buf = new byte[nodeLength * (BinaryTag == 5 ? 1 : 2)];

        if (stream.Read(buf, 0, buf.Length) != buf.Length)
            throw new PListFormatException();

        var encoding = BinaryTag == 5 ? Encoding.UTF8 : Encoding.BigEndianUnicode;

        Value = encoding.GetString(buf, 0, buf.Length);
    }

    /// <summary>
    /// Writes this element binary to the writer.
    /// </summary>
    internal override void WriteBinary(Stream stream)
    {
        Encoding enc = IsUtf16 ? Encoding.BigEndianUnicode : Encoding.UTF8;
        var buf = enc.GetBytes(Value);
        stream.Write(buf, 0, buf.Length);
    }
}
