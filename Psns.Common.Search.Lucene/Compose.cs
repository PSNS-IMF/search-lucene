using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
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
            new LuceneIndexWriter(directory, new LowerCaseKeyWordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

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
        /// Perform an action using an index writer that will be disposed when finished
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="useWriter"></param>
        /// <returns></returns>
        public static Either<Exception, T> tryWithLuceneIndexWriter<T>(string directory, Func<IIndexWriter, T> useWriter) =>
            tryWithIndexWriter(fun(() => indexWriterFactory(directory)), directory, useWriter);

        /// <summary>
        /// Rebuild an index using a Lucene IndexWriter
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="itemDocumentChunks"></param>
        /// <param name="termFactory"></param>
        /// <returns></returns>
        public static async Task<Either<Exception, Unit>> rebuildSearchIndexWithLuceneIndexWriterAsync(
            string directory,
            IEnumerable<IEnumerable<Tuple<Unit, Document>>> itemDocumentChunks,
            Func<Tuple<Unit, Document>, Term> termFactory) =>
                await rebuildSearchIndexAsync(
                    itemDocumentChunks, 
                    fun((Func<IIndexWriter, Unit> useWriter) => tryWithLuceneIndexWriter(directory, useWriter)), 
                    termFactory);
    }
}