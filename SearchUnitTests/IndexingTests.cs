using System;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Moq;
using NUnit.Framework;
using Psns.Common.Search.Lucene;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static Psns.Common.Search.Lucene.AppPrelude;

namespace SearchUnitTests
{
    [TestFixture]
    public class IndexingTests : AssertionHelper
    {
        [Test]
        public void MapItems_AppliesMappingFunctionAndChunks()
        {
            var documentChunks = mapItems(Range(1, 1000), i => Tuple(i, new Document()));

            Expect(documentChunks.Count(), EqualTo(2));

            iter(
                documentChunks,
                chunk => Expect(chunk.Count(), EqualTo(500)));
        }

        [Test]
        public void WithIndexWriter_FactoryReturnsWriter_ReturnsDirectory()
        {
            var called = false;
            var mockWriter = new Mock<IIndexWriter>();
            mockWriter.Setup(w => w.Directory).Returns(new RAMDirectory());

            var result = tryWithIndexWriter(() => mockWriter.Object, "directory", writer => { called = true; return unit; });

            Expect(match(result, dir => "ok", ex => "failed"), Is.EqualTo("ok"));
            Expect(called, Is.True);
        }

        [Test]
        public void WithIndexWriter_FactoryThrows_ReturnsException()
        {
            var called = false;
            var mockWriter = new Mock<IIndexWriter>();
            mockWriter.Setup(w => w.Directory).Returns(new RAMDirectory());

            var result = tryWithIndexWriter(() => failwith<IIndexWriter>("error"), "directory", writer => { called = true; return unit; });

            Expect(match(result, dir => "ok", ex => ex.Message), Is.EqualTo("error"));
            Expect(called, Is.False);
        }

        [Test]
        public void Index_UpdatesDocumentsAndCommits()
        {
            var mockWriter = new Mock<IIndexWriter>();

            var result = index(
                List(List(Tuple(1, new Document()))),
                doc => new Term("Id"),
                action => { action(mockWriter.Object); return new RAMDirectory(); });

            mockWriter.Verify(w => w.UpdateDocument(It.Is<Term>(t => t.Field == "Id"), It.IsAny<Document>()), Times.Once());
        }

        [Test]
        public async Task RebuildSearchIndexAsync_UpdatesAllDocumentsAndCallsOptimize()
        {
            var mockIndexWriter = new Mock<IIndexWriter>();
            var idDocs = List(Tuple(1, new Document()), Tuple(2, new Document()), Tuple(3, new Document()));
            var callBackCount = 0;

            var result = await rebuildSearchIndexAsync(
                List(idDocs, create<Tuple<int, Document>>()),
                action => { action(mockIndexWriter.Object); return unit; },
                intDocument => new Term("Id", intDocument.Item1.ToString()),
                count => callBackCount += count);

            Expect(match(result, Right: unit => "ok", Left: ex => "fail"), Is.EqualTo("ok"));
            Expect(callBackCount, Is.EqualTo(3));

            mockIndexWriter.Verify(w => w.DeleteAll(), Times.Once());
            mockIndexWriter.Verify(w => w.Commit(), Times.Once());
            mockIndexWriter.Verify(w => w.UpdateDocument(It.Is<Term>(term => term.Field == "Id" && term.Text == "1"), It.IsAny<Document>()), Times.Once());
            mockIndexWriter.Verify(w => w.UpdateDocument(It.Is<Term>(term => term.Field == "Id" && term.Text == "2"), It.IsAny<Document>()), Times.Once());
            mockIndexWriter.Verify(w => w.UpdateDocument(It.Is<Term>(term => term.Field == "Id" && term.Text == "3"), It.IsAny<Document>()), Times.Once());
            mockIndexWriter.Verify(w => w.Optimize(), Times.Once());
        }

        [Test]
        public async Task RebuildSearchIndexAsync_UpdateThrows_ResultIsException()
        {
            var mockIndexWriter = new Mock<IIndexWriter>();
            mockIndexWriter.Setup(w => w.UpdateDocument(It.IsAny<Term>(), It.IsAny<Document>())).Throws(new Exception("message"));

            var idDocs = List(Tuple(1, new Document()));

            var result = await rebuildSearchIndexAsync(
                List(idDocs),
                action => tryWithIndexWriter(() => mockIndexWriter.Object, "", action),
                intDocument => new Term("Id", intDocument.Item1.ToString()),
                count => { });

            Expect(match(result, Right: unit => "ok", Left: ex => "fail"), Is.EqualTo("fail"));

            mockIndexWriter.Verify(w => w.DeleteAll(), Times.Once());
            mockIndexWriter.Verify(w => w.Commit(), Times.Once());
            mockIndexWriter.Verify(w => w.Optimize(), Times.Never());
        }

        [Test]
        public async Task RebuildSearchIndexAsync_OptimizeThrows_ResultIsException()
        {
            var mockIndexWriter = new Mock<IIndexWriter>();
            mockIndexWriter.Setup(w => w.Optimize()).Throws(new Exception("message"));

            var idDocs = List(Tuple(1, new Document()));

            var result = await rebuildSearchIndexAsync(
                List(idDocs),
                action => tryWithIndexWriter(() => mockIndexWriter.Object, "", action),
                intDocument => new Term("Id", intDocument.Item1.ToString()),
                count => { });

            Expect(match(result, Right: unit => "ok", Left: ex => "fail"), Is.EqualTo("fail"));

            mockIndexWriter.Verify(w => w.DeleteAll(), Times.Once());
            mockIndexWriter.Verify(w => w.Commit(), Times.Once());
            mockIndexWriter.Verify(w => w.UpdateDocument(It.Is<Term>(term => term.Field == "Id" && term.Text == "1"), It.IsAny<Document>()), Times.Once());
        }
    }
}