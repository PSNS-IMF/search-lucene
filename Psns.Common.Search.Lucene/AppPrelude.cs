using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Psns.Common.Functional;
using Psns.Common.SystemExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Psns.Common.Functional.Prelude;

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
                TryUse(
                    writerFactory,
                    writer => action(writer))
                .Match(
                    success: ret => Right<Exception, Ret>(ret),
                    fail: ex => ex);

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
                TryUse(
                    writerFactory,
                    writer => action(writer))
                .Match(
                    success: ret => Right<Exception, Ret>(ret),
                    fail: ex => ex);

        /// <summary>
        /// Map <typeparamref name="T"/>s to chunks of <see cref="Document"/>s.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> MapToChunks<T>(
            this IEnumerable<T> self,
            Func<T, Tuple<T, ICollection<Document>>> mapper) =>
                self.Select(mapper).Chunk();

        /// <summary>
        /// Index and commit documents
        /// </summary>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="updateTermFactory"></param>
        /// <param name="deleteTermFactory"></param>
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
                        itemDocumentChunks.Iter(
                            chunk =>
                            {
                                // iterate through all documents
                                chunk.Iter(
                                    itemDocs =>
                                    {
                                        // Delete documents from the index, only if an id factory is supplied
                                        if (deleteTermFactory != null)
                                            writer.DeleteDocuments(deleteTermFactory(itemDocs.Item1));

                                        // iterate through a single document's parts
                                        itemDocs.Item2.Iter(
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
        /// <returns>Exception on fail; UnitValue on success</returns>
        public static async Task<Either<Exception, UnitValue>> rebuildSearchIndexAsync<T>(
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Func<IIndexWriter, UnitValue>, Either<Exception, UnitValue>> withIndexWriter,
            Func<Tuple<T, Document>, Term> termFactory,
            Action<int> chunkIndexedCallback) =>
                await Task.Run(() => 
                    withIndexWriter(writer =>
                    {
                        writer.DeleteAll();
                        writer.Commit();

                        var threads = itemDocumentChunks.Aggregate(
                            new List<Task>(),
                            (state, itemDocuments) =>
                            {
                                state.Add(
                                    Task.Factory.StartNew(
                                        (() =>
                                                itemDocuments.Iter(
                                                    items =>
                                                    {
                                                        items.Item2.Iter(
                                                            item => writer.UpdateDocument(termFactory(Tuple(items.Item1, item)), item));
                                                    })
                                                .Tap(unt => chunkIndexedCallback(itemDocuments.Count())))));

                                return state;
                            });

                        Task.WaitAll(threads.ToArray());

                        return Unit;
                    }));

        /// <summary>
        /// Can be used to increase search speed by reducing the number of segments in an index.
        /// Should be run during non-peak, index using hours.
        /// </summary>
        /// <param name="withIndexWriter"></param>
        /// <returns></returns>
        public static async Task<Either<Exception, UnitValue>> optimizeIndexAsync(
            Func<Func<IIndexWriter, UnitValue>, Either<Exception, UnitValue>> withIndexWriter) =>
            await Task.Run(() =>
                withIndexWriter(writer =>
                {
                    return Try(() => { writer.Optimize(); return Unit; }).Match(
                        success: ut => { },
                        fail: exception => raise<UnitValue>(exception));
                }));
    }
}