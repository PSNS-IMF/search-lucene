using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Documents;

namespace Psns.Common.Search.Lucene
{
    public interface IIndexWriter : IDisposable
    {
        Directory Directory { get; }

        void UpdateDocument(Term term, Document doc);
        void Commit();
        void DeleteAll();
        void Optimize();
        void AddDocument(Document doc);
        void DeleteDocuments(string field, string value);
        void AddIndexesNoOptimize(params Directory[] dirs);
    }
}