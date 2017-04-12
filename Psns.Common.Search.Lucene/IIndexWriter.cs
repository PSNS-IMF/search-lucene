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

        int MergeFactor { get; set; }

        /// <summary>
        /// Remove and re-index a Document
        /// </summary>
        /// <param name="term"></param>
        /// <param name="doc"></param>
        void UpdateDocument(Term term, Document doc);

        /// <summary>
        /// Merges all segments from an array of indexes into this index. 
        /// This may be used to parallelize batch indexing. A large document 
        /// collection can be broken into sub-collections. Each sub-collection 
        /// can be indexed in parallel, on a different thread, process or machine. 
        /// The complete index can then be created by merging sub-collection 
        /// indexes with this method. NOTE: the index in each Directory must 
        /// not be changed (opened by a writer) while this method is running. 
        /// This method does not acquire a write lock in each input Directory, 
        /// so it is up to the caller to enforce this.
        /// </summary>
        /// <param name="dirs"></param>
        void AddIndexesNoOptimize(params Directory[] dirs);

        void Optimize();

        void DeleteAll();

        void Commit();
    }
}