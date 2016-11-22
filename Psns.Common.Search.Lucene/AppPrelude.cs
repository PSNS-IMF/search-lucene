using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Psns.Common.SystemExtensions;
using static LanguageExt.List;
using static LanguageExt.Prelude;

namespace Psns.Common.Search.Lucene
{
    public static class AppPrelude
    {
        public delegate Either<Exception, Directory> withLuceneIndexWriterFunc(string directory, Action<IndexWriter> action);

        internal static Either<Exception, Directory> withLuceneIndexWriter(string directory, Action<IndexWriter> action)
        {
            BooleanQuery.MaxClauseCount = 4096;
            Cryptography.FIPSCompliant = true;

            return match(
                tryuse(
                    new IndexWriter(FSDirectory.Open(directory), new LowerCaseKeyWordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED),
                    writer => { action(writer); return writer.Directory; }),
                Succ: dir => Right<Exception, Directory>(dir),
                Fail: ex => ex);
        }

        public static IEnumerable<IEnumerable<Document>> mapItems<T>(IEnumerable<T> items, Func<T, Document> mapItem) =>
            map(items, mapItem).Chunk();

        public static Either<Exception, Directory> subIndex(string directory, IEnumerable<Document> documents, withLuceneIndexWriterFunc withWriter) =>
            withWriter(
                directory,
                writer => 
                    iter(
                        documents,
                        doc => writer.AddDocument(doc)));

        public static void mergeIndexes(string directory, IEnumerable<Directory> indexes, withLuceneIndexWriterFunc withWriter)
        {
            withWriter(
                directory,
                writer =>
                    iter(
                        indexes,
                        index => writer.AddIndexesNoOptimize(index)));
        }
    }
}