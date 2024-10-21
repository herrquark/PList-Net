using XmlTools;

namespace PlistNet.Extensions;

public static class LightXmlWriterExtensions
{
    public static void WriteStartElementWithIndent(this LightXmlWriter writer, string name, int indent, bool newLine = false)
    {
        writer.WriteRaw(new string('\t', indent));
        writer.WriteStartElement(name);
        if (newLine)
            writer.WriteRaw("\n");
    }

    public static void WriteStartElementLineWithIndent(this LightXmlWriter writer, string name, int indent)
        => writer.WriteStartElementWithIndent(name, indent, true);

    public static void WriteEndElementWithIndent(this LightXmlWriter writer, string name, int indent, bool newLine = false)
    {
        writer.WriteRaw(new string('\t', indent));
        writer.WriteEndElement(name);
        if (newLine)
            writer.WriteRaw("\n");
    }

    public static void WriteEndElementLineWithIndent(this LightXmlWriter writer, string name, int indent)
        => writer.WriteEndElementWithIndent(name, indent, true);

    public static void WriteSelfClosingLineWithIndent(this LightXmlWriter writer, string name, int indent)
    {
        writer.WriteRaw(new string('\t', indent));
        writer.WriteStartElement(name);
        writer.WriteEndElement(name);
        writer.WriteRaw("\n");
    }

    public static void WriteElementLineWithValue(this LightXmlWriter writer, string name, string value, int indent)
    {
        writer.WriteRaw(new string('\t', indent));
        writer.WriteStartElement(name);
        writer.WriteValue(value);
        writer.WriteEndElement(name);
        writer.WriteRaw("\n");
    }

    public static void WriteNewLine(this LightXmlWriter writer)
        => writer.WriteRaw("\n");

    public static void WritePlistHeader(this LightXmlWriter writer)
        => writer.WriteRaw("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">

            """);

    public static void WritePlistFooter(this LightXmlWriter writer)
        => writer.WriteRaw("</plist>\n");
}
