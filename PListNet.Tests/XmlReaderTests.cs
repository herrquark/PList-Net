using PListNet.Nodes;

namespace PListNet.Tests;

[TestFixture]
public class XmlReaderTests
{
    [Test]
    public void WhenParsingXmlDocumentWithSingleDictionary_ThenItIsParsedCorrectly()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/asdf-Info.plist");
        var node = PList.Load(stream);

        Assert.That(node, Is.Not.Null);

        var dictionary = node as DictionaryNode;
        Assert.That(dictionary, Is.Not.Null);

        Assert.That(dictionary, Has.Count.EqualTo(14));
    }

    [Test]
    public void WhenDocumentContainsNestedCollections_ThenDocumentIsParsedCorrectly()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/dict-inside-array.plist");
        var node = PList.Load(stream);

        Assert.That(node, Is.Not.Null);
        Assert.That(node, Is.InstanceOf<DictionaryNode>());

        var array = ((DictionaryNode)node).Values.First() as ArrayNode;
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Has.Count.EqualTo(1));

        var dictionary = array[0] as DictionaryNode;
        Assert.That(dictionary, Is.Not.Null);

        Assert.That(dictionary, Has.Count.EqualTo(4));
    }

    [Test]
    public void WhenDocumentContainsNestedCollectionsAndComplexText_ThenDocumentIsParsedCorrectly()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/Pods-acknowledgements.plist");
        var root = PList.Load(stream) as DictionaryNode;

        Assert.That(root, Is.Not.Null);
        Assert.That(root, Has.Count.EqualTo(3));

        Assert.That(root["StringsTable"], Is.InstanceOf<StringNode>());
        Assert.That(root["Title"], Is.InstanceOf<StringNode>());

        var array = root["PreferenceSpecifiers"] as ArrayNode;
        Assert.That(array, Is.Not.Null);
        Assert.That(array, Has.Count.EqualTo(15));

        foreach (var node in array)
        {
            Assert.That(node, Is.InstanceOf<DictionaryNode>());

            var dictionary = (DictionaryNode)node;
            Assert.That(dictionary, Has.Count.EqualTo(3));
        }
    }

    [Test]
    public void WhenDocumentContainsEmptyArray_ThenDocumentIsParsedCorrectly()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/empty-array.plist");
        var root = PList.Load(stream) as DictionaryNode;

        Assert.That(root, Is.Not.Null);
        Assert.That(root, Has.Count.EqualTo(1));

        Assert.That(root["Entitlements"], Is.InstanceOf<DictionaryNode>());
        var dict = root["Entitlements"] as DictionaryNode;

        var array = dict["com.apple.developer.icloud-container-identifiers"] as ArrayNode;
        Assert.That(array, Is.Not.Null);
        Assert.That(array.Count, Is.EqualTo(0));
    }

    [Test]
    public void WhenReadingUid_UidNodeIsParsed()
    {
        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-7-xml.plist");
            var node = PList.Load(stream);
            Assert.Pass();
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }
}
