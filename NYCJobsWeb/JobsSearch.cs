using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace NYCJobsWeb
{
    public class JobsSearch
    {
        //private static SearchServiceClient _indexClient;
        private static SearchClient _searchClient;
        private static string IndexName = "nycjobs";
        private static SearchClient _searchZipClient;
        private static string IndexZipCodes = "zipcodes";

        public static string errorMessage;

        static JobsSearch()
        {
            try
            {
                string searchServiceUri = ConfigurationManager.AppSettings["SearchServiceUri"];
                string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

                // Create an HTTP reference to the catalog index
                //_indexClient = new SearchServiceClient(new Uri(searchServiceUri), new AzureKeyCredential(apiKey));
                _searchClient = new SearchClient(new Uri(searchServiceUri), IndexName, new AzureKeyCredential(apiKey));
                _searchZipClient = new SearchClient(new Uri(searchServiceUri), IndexZipCodes, new AzureKeyCredential(apiKey));
            }
            catch (RequestFailedException ex)
            {
                errorMessage = ex.Message.ToString();
            }
        }

        public SearchResults<SearchDocument> Search(string searchText, string businessTitleFacet, string postingTypeFacet, string salaryRangeFacet,
            string sortType, double lat, double lon, int currentPage, int maxDistance, string maxDistanceLat, string maxDistanceLon)
        {
            // Execute search based on query string
            try
            {
                SearchOptions options = new SearchOptions()
                {
                    SearchMode = SearchMode.Any,
                    Size = 10,
                    Skip = currentPage - 1,

                    // Add count
                    IncludeTotalCount = true,

                    // Add search highlights
                    HighlightPreTag = "<b>",
                    HighlightPostTag = "</b>"
                };
                options.HighlightFields.Add("job_description");

                // Limit results
                var selectList = new List<String>() {"id", "agency", "posting_type", "num_of_positions", "business_title",
                                                     "salary_range_from", "salary_range_to", "salary_frequency", "work_location",
                                                     "job_description","posting_date", "geo_location", "tags"};
                foreach (var item in selectList)
                {
                    options.Select.Add(item);
                }

                // Add facets
                var facetsList = new List<String>() { "business_title", "posting_type", "level", "salary_range_from,interval:50000" };
                foreach (var item in facetsList)
                {
                    options.Facets.Add(item);
                }

                // Define the sort type
                if (sortType == "featured")
                {
                    options.ScoringProfile = "jobsScoringFeatured";      // Use a scoring profile
                    options.ScoringParameters.Add("featuredParam-featured");
                    //options.ScoringParameters.Add(new ScoringParameter("mapCenterParam", GeographyPoint.Create(lon, lat)));
                    options.ScoringParameters.Add($"mapCenterParam-{lon},{lat}");
                }
                else if (sortType == "salaryDesc")
                    options.OrderBy.Add("salary_range_from desc");
                else if (sortType == "salaryIncr")
                    options.OrderBy.Add("salary_range_from");
                else if (sortType == "mostRecent")
                    options.OrderBy.Add("posting_date desc");

                // Add filtering
                string filter = null;
                if (businessTitleFacet != "")
                    filter = "business_title eq '" + businessTitleFacet + "'";
                if (postingTypeFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "posting_type eq '" + postingTypeFacet + "'";

                }
                if (salaryRangeFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "salary_range_from ge " + salaryRangeFacet + " and salary_range_from lt " + (Convert.ToInt32(salaryRangeFacet) + 50000).ToString();
                }

                if (maxDistance > 0)
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "geo.distance(geo_location, geography'POINT(" + maxDistanceLon + " " + maxDistanceLat + ")') le " + maxDistance.ToString();
                }

                options.Filter = filter;

                return _searchClient.Search(searchText, options);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public SearchResults<SearchDocument> SearchZip(string zipCode)
        {
            // Execute search based on query string
            try
            {
                SearchOptions options = new SearchOptions()
                {
                    SearchMode = SearchMode.All,
                    Size = 1,
                };
                return _searchZipClient.Search(zipCode, options);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public SuggestResults<SearchDocument> Suggest(string searchText, bool fuzzy)
        {
            // Execute search based on query string
            try
            {
                SuggestOptions options = new SuggestOptions()
                {
                    UseFuzzyMatching = fuzzy,
                    Size = 8
                };

                return _searchClient.Suggest(searchText, "sg", options);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public SearchDocument LookUp(string id)
        {
            // Execute geo search based on query string
            try
            {
                return _searchClient.GetDocument(id);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

    }
}
