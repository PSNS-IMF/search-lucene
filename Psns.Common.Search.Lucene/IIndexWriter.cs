using System;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Psns.Common.Search.Lucene
{
    public interface IIndexWriter : IDisposable
    {
        /// <summary>
        /// The directory where the index is stored
        /// </summary>
        Directory Directory { get; }

        /// <summary>
        /// Remove and re-index a Document
        /// </summary>
        /// <param name="term"></param>
        /// <param name="doc"></param>
        void UpdateDocument(Term term, Document doc);

        /// <summary>
        /// Commit changes to the index
        /// </summary>
        void Commit();

        /// <summary>
        /// Merge in other indexes with no optimization for maximum speed
        /// </summary>
        /// <param name="dirs"></param>
        void AddIndexesNoOptimize(params Directory[] dirs);
    }
}