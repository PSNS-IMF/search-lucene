using System.Linq;
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

            var result = withIndexWriter(() => mockWriter.Object, "directory", writer => { called = true; });

            Expect(match(result, dir => "ok", ex => "failed"), Is.EqualTo("ok"));
            Expect(called, Is.True);
        }

        [Test]
        public void WithIndexWriter_FactoryThrows_ReturnsException()
        {
            var called = false;
            var mockWriter = new Mock<IIndexWriter>();
            mockWriter.Setup(w => w.Directory).Returns(new RAMDirectory());

            var result = withIndexWriter(() => failwith<IIndexWriter>("error"), "directory", writer => { called = true; });

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
    }
}