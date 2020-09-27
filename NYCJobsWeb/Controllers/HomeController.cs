using NYCJobsWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace NYCJobsWeb.Controllers
{
    public class HomeController : Controller
    {
        private JobsSearch _jobsSearch = new JobsSearch();

        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult JobDetails()
        {
            return View();
        }

        public ActionResult Search(string q = "", string businessTitleFacet = "", string postingTypeFacet = "", string salaryRangeFacet = "",
            string sortType = "", double lat = 40.736224, double lon = -73.99251, int currentPage = 0, int zipCode = 10001,
            int maxDistance = 0)
        {
            // If blank search, assume they want to search everything
            if (string.IsNullOrWhiteSpace(q))
                q = "*";

            string maxDistanceLat = string.Empty;
            string maxDistanceLon = string.Empty;

            //Do a search of the zip code index to get lat / long of this location
            //Eventually this should be extended to search beyond just zip (i.e. city)
            if (maxDistance > 0)
            {
                var zipReponse = _jobsSearch.SearchZip(zipCode.ToString());
                foreach (var result in zipReponse.GetResults())
                {
                    var doc = (dynamic)result.Document;
                    maxDistanceLat = Convert.ToString(doc["geo_location"]["coordinates"][1], CultureInfo.InvariantCulture);
                    maxDistanceLon = Convert.ToString(doc["geo_location"]["coordinates"][0], CultureInfo.InvariantCulture);
                }
            }

            var response = _jobsSearch.Search(q, businessTitleFacet, postingTypeFacet, salaryRangeFacet, sortType, lat, lon, currentPage, maxDistance, maxDistanceLat, maxDistanceLon);

            NYCJob data = new NYCJob()
            { 
                Results = response.GetResults().ToList(),
                Count = Convert.ToInt32(response.TotalCount)
            };

            //The count is missing in view side, wrap Facets to a custom entity.
            foreach (var facet in response.Facets)
            {
                List<MyFacetResult> facetResults = new List<MyFacetResult>();

                foreach (var result in response.Facets.FirstOrDefault(x => x.Key == facet.Key).Value)
                {
                    MyFacetResult facetResult = new MyFacetResult();

                    facetResult.Keys = result.Keys;
                    facetResult.Values = result.Values;
                    facetResult.Count = result.Count;

                    facetResults.Add(facetResult);
                }

                data.Facets.Add(facet.Key, facetResults);
            }

            return new JsonResult()
            {
                // ***************************************************************************************************************************
                // If you get an error here, make sure to check that you updated the SearchServiceUri and SearchServiceApiKey in Web.config
                // ***************************************************************************************************************************

                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = data
            };

        }

        [HttpGet]
        public ActionResult Suggest(string term, bool fuzzy = true)
        {
            // Call suggest query and return results
            var response = _jobsSearch.Suggest(term, fuzzy);
            List<string> suggestions = new List<string>();
            foreach (var result in response.Results)
            {
                suggestions.Add(result.Text);
            }

            // Get unique items
            List<string> uniqueItems = suggestions.Distinct().ToList();

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = uniqueItems
            };

        }

        public ActionResult LookUp(string id)
        {
            // Take a key ID and do a lookup to get the job details
            if (id != null)
            {
                var response = _jobsSearch.LookUp(id);
                return new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new NYCJobLookup() { Result = response }
                };
            }
            else
            {
                return null;
            }

        }

    }
}

