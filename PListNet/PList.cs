using System.Text;
using System.Xml;
using PListNet.Internal;

namespace PListNet;

/// <summary>
/// Parses, saves, and creates a PList File
/// </summary>
public static class PList
{
    /// <summary>
    /// Loads the PList from specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the PList.</param>
    /// <returns>A <see cref="PNode"/> object loaded from the stream</returns>
    public static PNode Load(Stream stream)
        => IsFormatBinary(stream) // Detect binary format, and read using the appropriate method
            ? LoadAsBinary(stream)
            : LoadAsXml(stream);

    private static bool IsFormatBinary(Stream stream)
    {
        var buf = new byte[8];

        // read in first 8 bytes
        stream.Read(buf, 0, buf.Length);

        // rewind
        stream.Seek(0, SeekOrigin.Begin);

        // compare to known indicator (TODO: validate version as well)
        return Encoding.UTF8.GetString(buf, 0, 6) == "bplist";
    }

    private static PNode LoadAsBinary(Stream stream)
    {
        var reader = new BinaryFormatReader();
        return reader.Read(stream);
    }

    private static PNode LoadAsXml(Stream stream)
    {
        // set resolver to null in order to avoid calls to apple.com to resolve DTD
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
        };

        using var reader = XmlReader.Create(stream, settings);

        reader.MoveToContent();
        reader.ReadStartElement("plist");

        reader.MoveToContent();
        var node = NodeFactory.Create(reader.LocalName);
        node.ReadXml(reader);

        reader.ReadEndElement();

        return node;
    }

    /// <summary>
    /// Saves the PList to the specified stream.
    /// </summary>
    /// <param name="rootNode">Root node of the PList structure.</param>
    /// <param name="stream">The stream in which the PList is saves.</param>
    /// <param name="format">The format of the PList (Binary/Xml).</param>
    public static void Save(PNode rootNode, Stream stream, PListFormat format)
    {
        if (format == PListFormat.Xml)
            WriteXmlToStream(rootNode, stream);
        else
            WriteBinaryToStream(rootNode, stream);
    }

    /// <summary>
    /// Saves the PList to the specified stream.
    /// </summary>
    /// <param name="rootNode">Root node of the PList structure.</param>
    public static string SaveToString(PNode rootNode, bool writePlistMeta = true)
    {
        using var xmlStream = new MemoryStream();
        WriteXmlToStream(rootNode, xmlStream, writePlistMeta: writePlistMeta);

        return Encoding.UTF8.GetString(xmlStream.ToArray(), 0, (int)xmlStream.Length);
    }

    private static void WriteXmlToStream(PNode rootNode, Stream stream, string newLine = "\n", bool writePlistMeta = true)
    {
        var sets = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "\t",
            NewLineChars = newLine,
            OmitXmlDeclaration = !writePlistMeta,
        };
        var tmpStream = new MemoryStream();

        using (var xmlWriter = XmlWriter.Create(tmpStream, sets))
        {
            if (writePlistMeta)
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteDocType("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);

                // write out nodes, wrapped in plist root element
                xmlWriter.WriteStartElement("plist");
                xmlWriter.WriteAttributeString("version", "1.0");
            }

            rootNode.WriteXml(xmlWriter);

            if (writePlistMeta)
                xmlWriter.WriteEndElement();

            xmlWriter.Flush();
        }

        // XmlWriter always inserts a space before element closing (e.g. <true />)
        // whereas the Apple parser can't deal with the space and expects <true/>
        tmpStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(tmpStream);
        using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);

        writer.NewLine = newLine;
        for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
        {
            line = line.Trim() switch
            {
                "<true />" => line.Replace("<true />", "<true/>"),
                "<false />" => line.Replace("<false />", "<false/>"),
                _ => line
            };

            writer.WriteLine(line);
        }
    }

    private static void WriteBinaryToStream(PNode rootNode, Stream stream)
        => new BinaryFormatWriter().Write(stream, rootNode);
}
