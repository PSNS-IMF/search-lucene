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
        /// Perform an action using an index writer that will be disposed when finished
        /// </summary>
        /// <param name="writerFactory"></param>
        /// <param name="directory"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Either<Exception, Ret> tryWithIndexWriter<Ret>(
            Func<IIndexWriter> writerFactory,
            Directory directory,
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
        public static IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> mapItems<T>(IEnumerable<T> items, Func<T, Tuple<T, ICollection<Document>>> mapItem) =>
            map(items, mapItem).Chunk();

        /// <summary>
        /// Index and commit documents
        /// </summary>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <param name="withIndexWriter"></param>
        /// <returns></returns>
        public static Either<Exception, Directory> index<T>(
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Tuple<T, Document>, Term> updateTermFactory,
            Func<T, Term> deleteTermFactory,
            Func<Func<IIndexWriter, Directory>, Either<Exception, Directory>> withIndexWriter) =>
            withIndexWriter(
                writer =>
                {
                    iter(
                        itemDocumentChunks,
                        chunk =>
                        {
                            // iterate through all documents
                            iter(
                                chunk,
                                itemDocs =>
                                {
                                    // Delete documents from the index, only if an id factory is supplied
                                    if (deleteTermFactory != null)
                                        writer.DeleteDocuments(deleteTermFactory(itemDocs.Item1));

                                    // iterate through a single document's parts
                                    iter(
                                        itemDocs.Item2,
                                        itemDoc =>
                                        {
                                            writer.UpdateDocument(
                                                updateTermFactory(Tuple(itemDocs.Item1, itemDoc)),
                                                itemDoc);
                                        });
                                });
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
        /// <param name="chunkIndexedCallback">Will be called after each chunk is indexed providing a count of 
        /// how many documents are in the chunk</param>
        /// <returns>Exception on fail; Unit on success</returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexAsync<T>(
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Func<IIndexWriter, Unit>, Either<Exception, Unit>> withIndexWriter,
            Func<Tuple<T, Document>, Term> termFactory,
            Action<int> chunkIndexedCallback) =>
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
                                                items => 
                                                {
                                                    iter(
                                                        items.Item2, 
                                                        item => writer.UpdateDocument(termFactory(Tuple(items.Item1, item)), item));
                                                }),
                                                unt => chunkIndexedCallback(length(itemDocuments)))))));

                        Task.WaitAll(threads.ToArray());

                        return unit;
                    }));

        /// <summary>
        /// Can be used to increase search speed by reducing the number of segments in an index.
        /// Should be run during non-peak, index using hours.
        /// </summary>
        /// <param name="withIndexWriter"></param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> optimizeIndexAsync(
            Func<Func<IIndexWriter, Unit>, Either<Exception, Unit>> withIndexWriter) =>
            await Task.Run(() =>
                withIndexWriter(writer =>
                {
                    return match(
                        Try(() => { writer.Optimize(); return unit; }),
                        Succ: ut => { },
                        Fail: exception => raise<Exception>(exception));
                }));

        static R tee<R>(R val, Action<R> action)
        {
            action(val);
            return val;
        }
    }
}