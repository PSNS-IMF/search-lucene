using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public static Either<Exception, Ret> tryWithIndexWriter<Ret>(
            Func<IIndexWriter> writerFactory,
            string directory,
            Func<IIndexWriter, Ret> action) =>
            match(
                tryuse(
                    writerFactory,
                    writer => action(writer)),
                Succ: ret => Right<Exception, Ret>(ret),
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
            Func<Func<IIndexWriter, Directory>, Either<Exception, Directory>> withIndexWriter) =>
            withIndexWriter(
                writer =>
                {
                    iter(
                        itemDocumentChunks,
                        itemDocuments =>
                        {
                            iter(
                                itemDocuments,
                                itemDoc => writer.UpdateDocument(termFactory(itemDoc), itemDoc.Item2));
                        });

                    return writer.Directory;
                });

        /// <summary>
        /// Rebuild search index by using recommended strategy of using 
        /// one index writer instance and multiple threads for optimal speed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemDocumentChunks">Chunks of items that are evenly divided for thread pool processing</param>
        /// <param name="withIndexWriter">IndexWriter factory. Best to leave writer MergeFactor as default (10).
        /// Higher MergeFactors are better for batch indexing (>=10); lower for searching (less than 10)</param>
        /// <param name="termFactory">Term generator</param>
        /// <returns>Exception on fail; Unit on success</returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexAsync<T>(
            IEnumerable<IEnumerable<Tuple<T, Document>>> itemDocumentChunks,
            Func<Func<IIndexWriter, Unit>, Either<Exception, Unit>> withIndexWriter,
            Func<Tuple<T, Document>, Term> termFactory,
            Action chunkIndexedCallback) =>
                await Task.Run(() => 
                    withIndexWriter(writer =>
                    {
                        writer.DeleteAll();
                        writer.Commit();

                        var threads = fold(
                            itemDocumentChunks,
                            List<Task>(),
                            (state, itemDocuments) =>
                                state = state.Add(
                                    Task.Factory.StartNew(
                                        (() =>
                                            tee(
                                                iter(
                                                itemDocuments,
                                                item => 
                                                    writer.UpdateDocument(
                                                        termFactory(item), 
                                                        item.Item2)), 
                                                unt => chunkIndexedCallback())))));

                        match(
                            Try(() => { Task.WaitAll(threads.ToArray()); return unit; }),
                            Succ: ut => writer.Optimize(),
                            Fail: exception => raise<Exception>(exception));

                        return unit;
                    }));

        static R tee<R>(R val, Action<R> action)
        {
            action(val);
            return val;
        }
    }
}