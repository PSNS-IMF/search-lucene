using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Psns.Common.Search.Lucene
{
    /// <summary>
    /// An adapter class for the IIndexWriter interface
    /// </summary>
    internal class LuceneIndexWriter : IndexWriter, IIndexWriter
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="analyzer"></param>
        /// <param name="maxFieldLength"></param>
        public LuceneIndexWriter(string directory, Analyzer analyzer, MaxFieldLength maxFieldLength)
            : base(FSDirectory.Open(directory), analyzer, maxFieldLength) { }

        public LuceneIndexWriter(Directory directory, Analyzer analyzer, MaxFieldLength maxFieldLength)
            : base(directory, analyzer, maxFieldLength) { }
    }
}