using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using static LanguageExt.Prelude;

namespace Psns.Common.Search.Lucene
{
    public static partial class AppPrelude
    {
        /// <summary>
        /// Create a LuceneIndexWriter with LowerCaseKeyWordAnalyzer and Unlimited MaxFieldLength
        /// </summary>
        /// <param name="directory">Path to store the index</param>
        /// <returns></returns>
        public static IIndexWriter indexWriterFactory(string directory) =>
            indexWriterFactory(directory, new LowerCaseKeyWordAnalyzer());

        /// <summary>
        /// Create a LuceneIndexWriter with Unlimited MaxFieldLength
        /// </summary>
        /// <param name="directory">Lucene Directory object</param>
        /// <returns></returns>
        public static IIndexWriter indexWriterFactory(Directory directory) =>
            indexWriterFactory(directory, new LowerCaseKeyWordAnalyzer());

        /// <summary>
        /// Create a LuceneIndexWriter with Unlimited MaxFieldLength
        /// </summary>
        /// <param name="directory">Path to store the index</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <returns></returns>
        public static IIndexWriter indexWriterFactory(string directory, Analyzer analyzer) =>
            new LuceneIndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

        /// <summary>
        /// Create a LuceneIndexWriter with Unlimited MaxFieldLength
        /// </summary>
        /// <param name="directory">Lucene Directory object</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <returns></returns>
        public static IIndexWriter indexWriterFactory(Directory directory, Analyzer analyzer) =>
            new LuceneIndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

        /// <summary>
        /// Set MaxClauseCount to 4096 and FIPSCompliant to true
        /// </summary>
        /// <returns></returns>
        public static Unit initializeLucene()
        {
            BooleanQuery.MaxClauseCount = 4096;
            Cryptography.FIPSCompliant = true;

            return unit;
        }

        /// <summary>
        /// Perform an action using an index writer (using LowerCaseKeyWordAnalyzer) that will be disposed when finished
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="useWriter">The function to be provided an index writer</param>
        /// <returns></returns>
        public static Either<Exception, T> tryWithLuceneIndexWriter<T>(string directory, Func<IIndexWriter, T> useWriter) =>
            tryWithLuceneIndexWriter(directory, new LowerCaseKeyWordAnalyzer(), useWriter);

        /// <summary>
        /// Perform an action using an index writer (using LowerCaseKeyWordAnalyzer) that will be disposed when finished
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="useWriter">The function to be provided an index writer</param>
        /// <returns></returns>
        public static Either<Exception, T> tryWithLuceneIndexWriter<T>(Directory directory, Func<IIndexWriter, T> useWriter) =>
            tryWithLuceneIndexWriter(directory, new LowerCaseKeyWordAnalyzer(), useWriter);

        /// <summary>
        /// Perform an action using an index writer that will be disposed when finished
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <param name="useWriter">The function to be provided an index writer</param>
        /// <returns></returns>
        public static Either<Exception, T> tryWithLuceneIndexWriter<T>(string directory, Analyzer analyzer, Func<IIndexWriter, T> useWriter) =>
            tryWithIndexWriter(fun(() => indexWriterFactory(directory, analyzer)), directory, useWriter);

        /// <summary>
        /// Perform an action using an index writer that will be disposed when finished
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <param name="useWriter">The function to be provided an index writer</param>
        /// <returns></returns>
        public static Either<Exception, T> tryWithLuceneIndexWriter<T>(Directory directory, Analyzer analyzer, Func<IIndexWriter, T> useWriter) =>
            tryWithIndexWriter(fun(() => indexWriterFactory(directory, analyzer)), directory, useWriter);

        /// <summary>
        /// Rebuild an index using a Lucene IndexWriter with a LowerCaseKeyWordAnalyzer
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <param name="chunkIndexedCallback">Will be called after each chunk is indexed providing a count of 
        /// how many documents are in the chunk</param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexWithLuceneIndexWriterAsync<T>(
            string directory,
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Tuple<T, Document>, Term> termFactory,
            Action<int> chunkIndexedCallback) =>
                await rebuildSearchIndexWithLuceneIndexWriterAsync(
                    directory,
                    new LowerCaseKeyWordAnalyzer(),
                    itemDocumentChunks,
                    termFactory,
                    chunkIndexedCallback);

        /// <summary>
        /// Rebuild an index using a Lucene IndexWriter with a LowerCaseKeyWordAnalyzer
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <param name="chunkIndexedCallback">Will be called after each chunk is indexed providing a count of 
        /// how many documents are in the chunk</param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexWithLuceneIndexWriterAsync<T>(
            Directory directory,
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Tuple<T, Document>, Term> termFactory,
            Action<int> chunkIndexedCallback) =>
                await rebuildSearchIndexWithLuceneIndexWriterAsync(
                    directory,
                    new LowerCaseKeyWordAnalyzer(),
                    itemDocumentChunks,
                    termFactory,
                    chunkIndexedCallback);

        /// <summary>
        /// Rebuild an index using a Lucene IndexWriter
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <param name="chunkIndexedCallback">Will be called after each chunk is indexed providing a count of 
        /// how many documents are in the chunk</param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexWithLuceneIndexWriterAsync<T>(
            string directory,
            Analyzer analyzer,
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Tuple<T, Document>, Term> termFactory,
            Action<int> chunkIndexedCallback) =>
                await rebuildSearchIndexAsync(
                    itemDocumentChunks,
                    fun((Func<IIndexWriter, Unit> useWriter) => tryWithLuceneIndexWriter(directory, analyzer, useWriter)),
                    termFactory,
                    chunkIndexedCallback);

        /// <summary>
        /// Rebuild an index using a Lucene IndexWriter
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <param name="chunkIndexedCallback">Will be called after each chunk is indexed providing a count of 
        /// how many documents are in the chunk</param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexWithLuceneIndexWriterAsync<T>(
            Directory directory,
            Analyzer analyzer,
            IEnumerable<IEnumerable<Tuple<T, ICollection<Document>>>> itemDocumentChunks,
            Func<Tuple<T, Document>, Term> termFactory,
            Action<int> chunkIndexedCallback) =>
                await rebuildSearchIndexAsync(
                    itemDocumentChunks,
                    fun((Func<IIndexWriter, Unit> useWriter) => tryWithLuceneIndexWriter(directory, analyzer, useWriter)),
                    termFactory,
                    chunkIndexedCallback);

        /// <summary>
        /// Increase search speed by reducing index segmentation with a LowerCaseKeyWordAnalyzer
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> optimizeWithLuceneIndexWriterAsync(string directory) =>
            await optimizeWithLuceneIndexWriterAsync(directory, new LowerCaseKeyWordAnalyzer());

        /// <summary>
        /// Increase search speed by reducing index segmentation
        /// </summary>
        /// <param name="directory">The directory where the index is stored</param>
        /// <param name="analyzer">An analyzer to use for converting text into search terms</param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> optimizeWithLuceneIndexWriterAsync(string directory, Analyzer analyzer) =>
            await optimizeIndexAsync(
                fun((Func<IIndexWriter, Unit> useWriter) => tryWithLuceneIndexWriter(directory, analyzer, useWriter)));
    }
}