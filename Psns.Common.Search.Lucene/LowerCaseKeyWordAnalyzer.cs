﻿using System.IO;
using Lucene.Net.Analysis;

namespace Psns.Common.Search.Lucene
{
    internal class LowerCaseKeyWordAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new LowerCaseFilter(new KeywordTokenizer(reader));
        }
    }
}