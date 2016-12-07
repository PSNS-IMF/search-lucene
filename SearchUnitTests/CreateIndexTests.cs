using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using NUnit.Framework;
using static LanguageExt.List;
using static LanguageExt.Prelude;
using static Psns.Common.Search.Lucene.AppPrelude;
using Lucene.Net.Store;

namespace SearchUnitTests
{
    [TestFixture]
    public class CreateIndexTests : AssertionHelper
    {
        [Test]
        public void MapItems_AppliesMappingFunctionAndChunks()
        {
            var documentChunks = mapItems(Range(1, 1000), i => new Document());

            Expect(documentChunks.Count(), EqualTo(2));

            iter(
                documentChunks,
                chunk => Expect(chunk.Count(), EqualTo(500)));
        }

        [Test]
        public void SubIndex_AddDocumentIsOk_ReturnsDirectory()
        {
            var result = subIndex(
                "directory",
                new List<List<Document>> { new List<Document> { new Document() } },
                (directory, useWriter) => new RAMDirectory());

            Expect(match(result, dir => "ok", dir => "failed"), Is.EqualTo("ok"));
        }

        [Test]
        public void SubIndex_AddDocumentsFails_ReturnsException()
        {
            var result = subIndex(
                "directory",
                new List<List<Document>> { new List<Document> { new Document() } },
                (directory, useWriter) => new Exception("bam"));

            Expect(match(result, dir => "ok", ex => ex.Message), Is.EqualTo("bam"));
        }

        [Test]
        public void MergeIndexes_AddIndexesNoOptimizeOk_ReturnsDocument()
        {

        }
    }
}