using PListNet.Nodes;

namespace PListNet.Tests;

public class BinaryWriterTests
{
    [Test]
    public void WhenXmlFormatIsResavedAsBinaryAndOpened_ThenParsedDocumentMatchesTheOriginal()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/asdf-Info.plist");
        var node = PList.Load(stream);

        using var outStream = new MemoryStream();
        PList.Save(node, outStream, PListFormat.Binary);

        // rewind and reload
        outStream.Seek(0, SeekOrigin.Begin);
        var newNode = PList.Load(outStream);

        // compare
        Assert.That(newNode.GetType().Name, Is.EqualTo(node.GetType().Name));

        var oldDict = node as DictionaryNode;
        var newDict = newNode as DictionaryNode;

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
}
