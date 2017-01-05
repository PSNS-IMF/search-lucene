using System;
using System.Collections.Generic;
using LanguageExt;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Psns.Common.SystemExtensions;
using static LanguageExt.List;
using static LanguageExt.Prelude;

namespace Psns.Common.Search.Lucene
{
    public static partial class AppPrelude
    {
        /// <summary>
        /// Perform an action using an index writer that will be disposed when finished
        /// </summary>
        /// <param name="writerFactory"></param>
        /// <param name="directory"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Either<Exception, Directory> withIndexWriter(
            Func<IIndexWriter> writerFactory,
            string directory,
            Action<IIndexWriter> action) =>
            match(
                tryuse(
                    writerFactory,
                    writer => { action(writer); return writer.Directory; }),
                Succ: dir => Right<Exception, Directory>(dir),
                Fail: ex => ex);

        /// <summary>
        /// Map Ts to chunks of Lucene Documents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="mapItem"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<Tuple<T, Document>>> mapItems<T>(IEnumerable<T> items, Func<T, Tuple<T, Document>> mapItem) =>
            map(items, mapItem).Chunk();

        /*
         * very no explicit optimize or commit
         * find best merge policy
         */

        /// <summary>
        /// Index and commit documents
        /// </summary>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <param name="withIndexWriter"></param>
        /// <returns></returns>
        public static Either<Exception, Directory> index<T>(
            IEnumerable<IEnumerable<Tuple<T, Document>>> itemDocumentChunks,
            Func<Tuple<T, Document>, Term> termFactory,
            Func<Action<IIndexWriter>, Either<Exception, Directory>> withIndexWriter) =>
            withIndexWriter(
                writer =>
                iter(
                    itemDocumentChunks,
                    itemDocuments =>
                    {
                        iter(
                            itemDocuments,
                            itemDoc => writer.UpdateDocument(termFactory(itemDoc), itemDoc.Item2));
                    }));
    }
}