using Azure.Search.Documents.Models;
using System.Collections.Generic;

namespace NYCJobsWeb.Models
{
    public class NYCJob
    {
        //public IDictionary<string, IList<FacetResult>> Facets { get; set; }
        public NYCJob()
        {
            Facets = new Dictionary<string, IList<MyFacetResult>>();
        }

        public IDictionary<string, IList<MyFacetResult>> Facets { get; set; }
        public IList<SearchResult<SearchDocument>> Results { get; set; }
        public int? Count { get; set; }
    }

    public class NYCJobLookup
    {
        public SearchDocument Result { get; set; }
    }

    public class MyFacetResult
    {
        public IEnumerable<string> Keys { get; set; }
        public IEnumerable<object> Values { get; set; }
        public long? Count { get; set; }
    }
}
