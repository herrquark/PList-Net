using NUnit.Framework;
using PListNet.Nodes;

namespace PListNet.Tests;

public class BinaryReaderTests
{
    [Test]
    public void WhenParsingBinaryDocumentWithSingleDictionary_ThenItIsParsedCorrectly()
    {
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/asdf-Info-bin.plist");
        var node = PList.Load(stream);

        Assert.That(node, Is.Not.Null);

        var dictionary = node as DictionaryNode;
        Assert.That(dictionary, Is.Not.Null);

        Assert.That(dictionary, Has.Count.EqualTo(14));
    }

    [Test]
    public void ReadingFile_With_UID_Field_Fail()
    {

        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/uid-test.plist");
            var node = PList.Load(stream);
            Assert.Pass();
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [Test]
    public void WhenReadingUid_UidNodeIsParsed()
    {
        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-7-binary.plist");
            var node = PList.Load(stream);
            Assert.Pass();
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [Test]
    public void WhenReadingFileWithUid_UidValueIsParsed()
    {
        // this binary .plist file came from https://bugs.python.org/issue26707
        using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-7-binary-2.plist");
        var root = PList.Load(stream) as DictionaryNode;

        Assert.That(root, Is.Not.Null);
        Assert.That(root, Has.Count.EqualTo(4));

        var dict = root["$top"] as DictionaryNode;
        Assert.That(dict, Is.Not.Null);

        var uid = dict["data"] as UidNode;
        Assert.That(uid, Is.Not.Null);

        Assert.That(uid.Value, Is.EqualTo(1));
    }

    [Test]
    public void ReadingFile_With_16bit_Integers_Fail()
    {
        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/unity.binary.plist");
            var node = PList.Load(stream);
            Assert.Pass();
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [Test]
    public void ReadingFile_GitHub_Issue9_Fail()
    {
        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-9.plist");
            var node = PList.Load(stream);
            Assert.Pass();
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [Test]
    public void ReadingFile_GitHub_Issue15_FailReadingLargeDictionary()
    {
        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-15-medium-binary.plist");
            var node = PList.Load(stream);

            var dictNode = node as DictionaryNode;
            Assert.That(dictNode, Is.Not.Null);
            Assert.That(dictNode.Keys, Has.Count.EqualTo(16384));
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }

    [Test]
    public void ReadingFile_GitHub_Issue15_FailReadingLargeArray()
    {
        try
        {
            using var stream = TestFileHelper.GetTestFileStream("TestFiles/github-15-large-binary.plist");
            var node = PList.Load(stream);

            var dictNode = node as DictionaryNode;
            Assert.That(dictNode, Is.Not.Null);
            Assert.That(dictNode.Keys, Has.Count.EqualTo(32768));
        }
        catch (PListFormatException ex)
        {
            Assert.Fail(ex.Message);
        }
    }
}
