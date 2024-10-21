using System.Text;
using PListNet.Nodes;

namespace PListNet.Tests;

[TestFixture]
public class XmlWriterTests
{
    [Test]
    public void WhenXmlFormatIsSavedAndOpened_ThenParsedDocumentMatchesTheOriginal()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/utf8-Info.plist");

        var node = PList.Load(stream);

        using var outStream = new MemoryStream();
        PList.Save(node, outStream, PListFormat.Xml);

        // rewind and reload
        outStream.Seek(0, SeekOrigin.Begin);
        var newNode = PList.Load(outStream);

        // compare
        Assert.That(newNode.GetType().Name, Is.EqualTo(node.GetType().Name));

        var oldDict = node as DictionaryNode;
        var newDict = newNode as DictionaryNode;

        Assert.That(oldDict, Is.Not.Null);
        Assert.That(newDict, Is.Not.Null);
        Assert.That(newDict, Has.Count.EqualTo(oldDict.Count));

        foreach (var key in oldDict.Keys)
        {
            Assert.That(newDict.ContainsKey(key), Is.True);

            var oldValue = oldDict[key];
            var newValue = newDict[key];

            Assert.That(newValue.GetType().Name, Is.EqualTo(oldValue.GetType().Name));
            Assert.That(newValue, Is.EqualTo(oldValue));
        }
    }

    [Test]
    public void WhenBooleanValueIsSaved_ThenThereIsNoWhiteSpace()
    {
        using var outStream = new MemoryStream();
        // create basic PList containing a boolean value
        var node = new DictionaryNode { { "Test", new BooleanNode(true) } };

        // save and reset stream
        PList.Save(node, outStream, PListFormat.Xml);
        outStream.Seek(0, SeekOrigin.Begin);

        // check that boolean was written out without a space per spec (see also issue #11)
        using var reader = new StreamReader(outStream);
        var contents = reader.ReadToEnd();

        Assert.That(contents.Contains("<true/>"), Is.True);
    }

    [Test]
    public void WhenXmlPlistWithBooleanValueIsLoadedAndSaved_ThenWhiteSpaceMatches()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-20.plist");

        // read in the source file and reset the stream so we can parse from it
        using var plistReader = new StreamReader(stream, Encoding.Default, true, 2048, true);
        var source = plistReader.ReadToEnd();

        stream.Seek(0, SeekOrigin.Begin);

        var root = PList.Load(stream) as DictionaryNode;
        Assert.That(root, Is.Not.Null);

        // verify that we parsed expected content
        var node = root["ABool"] as BooleanNode;
        Assert.That(node, Is.Not.Null);
        Assert.That(node.Value, Is.True);

        // write the file out to memory and check that there is still no space
        // in the written out boolean node
        using var outStream = new MemoryStream();
        // save and reset stream
        PList.Save(root, outStream, PListFormat.Xml);
        outStream.Seek(0, SeekOrigin.Begin);

        // check that boolean was written out without a space per spec (see also issue #11)
        using var outReader = new StreamReader(outStream);
        var contents = outReader.ReadToEnd();

        Assert.That(contents, Is.EqualTo(source));
    }

    [Test]
    public void WhenStringContainsUnicode_ThenStringIsWrappedInStringTag()
    {
        using var outStream = new MemoryStream();
        var utf16value = "😂test";

        // create basic PList containing a boolean value
        var node = new DictionaryNode { ["Test"] = new StringNode(utf16value) };

        // save and reset stream
        PList.Save(node, outStream, PListFormat.Xml);
        outStream.Seek(0, SeekOrigin.Begin);

        // check that boolean was written out without a space per spec (see also issue #11)
        using var reader = new StreamReader(outStream);
        var contents = reader.ReadToEnd();

        Assert.That(contents.Contains($"<string>{utf16value}</string>"), Is.True);
    }

    [Test]
    public void WhenWriteXmlMetaIsFalse_ThenWriteNoXmlMeta()
    {
        var node = new BooleanNode(true);

        // save and reset stream
        var str = PList.ToString(node,  writePlistMeta: false);

        Assert.That(str, Does.Not.Contain("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
        Assert.That(str, Contains.Substring("<true/>"));
    }

    [TestCase("TestFiles/asdf-Info.plist")]
    [TestCase("TestFiles/unity.xml.plist")]
    [TestCase("TestFiles/uid-test.xml.plist")]
    [TestCase("TestFiles/utf8-Info.plist")]
    [TestCase("TestFiles/github-7-xml.plist")]
    [TestCase("TestFiles/github-15-large-xml.plist")]
    public void WhenReadXml_MustWriteTheSameXml(string fileName)
    {
        using var stream = TestFileHelper.GetTestFileStream(fileName);

        // read in the source file and reset the stream so we can parse from it
        using var plistReader = new StreamReader(stream, Encoding.Default, true, 2048, true);
        var source = plistReader.ReadToEnd();

        stream.Seek(0, SeekOrigin.Begin);

        var root = PList.Load(stream) as DictionaryNode;
        var serialized = PList.ToString(root);

        Assert.That(serialized, Is.EqualTo(source));
    }
}
