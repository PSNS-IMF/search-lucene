using System;
using LanguageExt;
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
            new LuceneIndexWriter(directory, new LowerCaseKeyWordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

        /// <summary>
        /// Set MaxClauseCount to 4096 and FIPSCompliant to true
        /// </summary>
        /// <returns></returns>
        public static Unit initializeLucene()
        {
            BooleanQuery.MaxClauseCount = 4096;
            Cryptography.FIPSCompliant = true;

            return Unit.Default;
        }

        /// <summary>
        /// Perform an action using an index writer that will be disposed when finished
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="useWriter"></param>
        /// <returns></returns>
        public static Either<Exception, Directory> withLuceneIndexWriter(string directory, Action<IIndexWriter> useWriter) =>
            withIndexWriter(fun(() => indexWriterFactory(directory)), directory, useWriter);
    }
}