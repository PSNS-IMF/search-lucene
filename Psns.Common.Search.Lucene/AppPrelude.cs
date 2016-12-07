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
    public static partial class AppPrelude
    {
        public delegate Either<Exception, Directory> withIndexWriterFunc(
            string directory, 
            Action<IIndexWriter> action);

        internal static Either<Exception, Directory> withIndexWriter(
            string directory,
            Func<IIndexWriter> writerFactory,
            Action<IIndexWriter> action) =>
            match(
                tryuse(
                    writerFactory(),
                    writer => { action(writer); return writer.Directory; }),
                Succ: dir => Right<Exception, Directory>(dir),
                Fail: ex => ex);

        public static IEnumerable<IEnumerable<Document>> mapItems<T>(IEnumerable<T> items, Func<T, Document> mapItem) =>
            map(items, mapItem).Chunk();

        public static Either<Exception, Directory> subIndex(string directory,
            IEnumerable<IEnumerable<Document>> documentChunks,
            withIndexWriterFunc withIndexWriter) =>
            withIndexWriter(directory, writer =>
                iter(
                    documentChunks,
                    documents =>
                        iter(
                            documents,
                            doc => writer.AddDocument(doc))));


        public static Either<Exception, Directory> mergeIndexes(
            string directory, 
            IEnumerable<Directory> indexes, 
            withIndexWriterFunc withWriter) =>
            withWriter(
                directory,
                writer =>
                    iter(
                        indexes,
                        index => writer.AddIndexesNoOptimize(index)));
    }
}