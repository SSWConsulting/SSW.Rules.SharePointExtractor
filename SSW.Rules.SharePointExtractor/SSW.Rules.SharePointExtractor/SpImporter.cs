using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor
{
    public class SpImporter: ISpImporter
    {
        
        private readonly ILogger<SpImporter> _log;
        private readonly AppConfig _appConfig;
        private readonly HttpClient _apiHttpClient;
        

        public const string RulePageContentTypeId        = "0x010100C568DB52D9D0A14D9B2FDCC96666E9F2007948130EC3DB064584E219954237AF3900242457EFB8B24247815D688C526CD44D005DFA84120DD02646B1A3F625BA07E04100815983DA80CA3444BB76181B2E691882";
        public const string RuleSummaryPageContentTypeId = "0x010100C568DB52D9D0A14D9B2FDCC96666E9F2007948130EC3DB064584E219954237AF3900242457EFB8B24247815D688C526CD44D00570D4EFC7A036545AA73569FF6BDDAA300CD4B791EBE3ACC458BB6335274B5C081";
        

        public SpImporter(IHttpClientFactory clientFactory, ILogger<SpImporter> log, AppConfig appConfig)
        {
            _apiHttpClient = clientFactory.CreateClient(appConfig.SharePointApiClient);
            _log = log;
            _appConfig = appConfig;
        }


        public async Task<SpRulesDataSet> Import()
        {
            var dataSet = new SpRulesDataSet();

            await LoadCategoriesJson(dataSet);
            await LoadPagesJson(dataSet);
            CullEmptyCategories(dataSet);
            await ScrapeHomePage(dataSet);
            await ScrapeCategoryPages(dataSet);
            
            //_log.LogWarning("{@DataSet}", dataSet);

            return dataSet;
        }

        private void CullEmptyCategories(SpRulesDataSet dataSet)
        {
            var allCategoriesWithRules = dataSet.Rules.SelectMany(r => r.Categories).Distinct();
            dataSet.Categories = dataSet.Categories.Where(c => allCategoriesWithRules.Contains(c)).ToList();
        }


        /// <summary>
        /// we screen-scrape every category summary page to get the ordering of rule items within a category
        /// </summary>
        /// <param name="dataSet">data set we're building</param>
        private async Task ScrapeCategoryPages(SpRulesDataSet dataSet)
        {
            foreach (var cat in dataSet.Categories)
            {
                if (cat.Uri == null)
                {
                    _log.LogWarning("Category {Title} has no Uri", cat.Title);
                    continue;
                }
                // for sanity-checking, we only search the subset of rules that are already associated with category
                var categoryRules = dataSet.Rules
                    .Where(r => r.Categories.Contains(cat))
                    .ToList();

                if (!categoryRules.Any())
                {
                    _log.LogWarning("category {Title} at {Uri} has no rules", cat.Title, cat.Uri);
                    continue;
                }
                
                _log.LogInformation("Screen scrape of category {CatTitle}", cat.Title);
                HtmlWeb web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync(_appConfig.SharePointUrl + cat.Uri.ToString());

                var ruleNodes = doc.DocumentNode.SelectNodes(
                    ".//*[@id='ctl00_PlaceHolderMain_RuleSummaryUC_SSWRuleSummaryUCDiv']/div/ol/li");

                if (ruleNodes == null)
                {
                    _log.LogWarning("no rule links found for {Title} on page {Url}", cat.Title, cat.Uri);
                    continue; 
                }
                
                foreach (var ruleNode in ruleNodes)
                {
                    var linkNode = ruleNode.SelectSingleNode("h2/a");
                    var title = linkNode.InnerText.Trim();
                    var ruleData = categoryRules.FirstOrDefault(r =>
                        r.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase));
                    if (ruleData == null)
                    {
                        _log.LogWarning("Failed to find rule {RuleTitle} under category {CategoryTitle}", title, cat.Title);
                        _log.LogWarning("Available rules titles: {RuleTitles}", categoryRules.Select(r => r.Title).ToList());
                    }
                    else
                    {
                        cat.Rules.Add(ruleData);
                    }
                } // end foreach rule link in summary page

            } // end foreach category
        }


        /// <summary>
        /// SharePoint sucks so hard that I abandoned attempting to talk to the term store and just scraped details from the rules site html instead
        /// - brendan
        /// </summary>
        /// <param name="dataSet">the dataset we're building</param>
        private async Task ScrapeHomePage(SpRulesDataSet dataSet)
        {
            _log.LogInformation("Scraping category details from home page...");
            HtmlWeb web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(_appConfig.SharePointUrl);
            var container = doc.DocumentNode.SelectSingleNode(
                ".//*[@id='ctl00_PlaceHolderMain_RuleLandingUC_SSWLandingUCDiv']/div[@class='ruleSortDiv']");
            var parentCategoryNodes = container.SelectNodes("div");
            
            foreach (var parentCatNode in parentCategoryNodes)
            {
                var title = parentCatNode.SelectSingleNode("h2").InnerText.Trim();
                if (string.IsNullOrWhiteSpace(title)) continue;
                var parentCat = new ParentCategory()
                {
                    Title = title
                };

                var categoriesData = dataSet.Categories
                    .Where(c => c.ParentCategoryTitle.Equals(parentCat.Title, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                
                foreach (var catNode in parentCatNode.SelectNodes("ol/li/a[@class='RuleSortList']"))
                {
                    var catTitle = catNode.InnerText.Trim();
                    var catUrl = catNode.Attributes["href"].Value;
                    var catData = categoriesData.FirstOrDefault(c =>
                        c.Title.Equals(catTitle, StringComparison.InvariantCultureIgnoreCase));
                    if (catData == null)
                    {
                        _log.LogWarning("failed to find {CatTitle} in list {CategoryList}", 
                            catTitle, categoriesData.Select(c => c.Title).ToList());
                        continue;
                    }
                    catData.Uri = new Uri(catUrl, UriKind.Relative);
                    parentCat.Categories.Add(catData);
                    //_log.LogInformation("=== {Category}", catData.Title);
                    
                }// end foreach category
                dataSet.ParentCategories.Add(parentCat);
            } // end foreach parent 

        }

        private async Task LoadCategoriesJson(SpRulesDataSet dataSet)
        {
            var taxonomyJson = await GetJsonListByTitle("TaxonomyHiddenList");
            JObject doc = JObject.Parse(taxonomyJson);
            var cats = doc.GetValue("value").Children();

            foreach (var cat in cats)
            {
                //if (dataSet.Categories.Any(c => c.Title.Equals(cat["Title"].Value<string>().Trim(), StringComparison.InvariantCultureIgnoreCase)))
                //{
                //    throw new Exception("Duplicate Category: "+cat["Title"].Value<string>().Trim());
                //}
                dataSet.Categories.Add(new Category()
                {
                    TermStoreId = cat["Id"].Value<int>(),
                    TermStoreGuid = cat["IdForTerm"].Value<string>(),
                    Title = cat["Title"].Value<string>().Trim(),
                    ParentCategoryTitle = ExtractParentCategory(cat["Path"].Value<string>()),
                });
            }
        }



        private async Task LoadPagesJson(SpRulesDataSet dataSet)
        {
            var pagesJson = await GetJsonListByTitle("Pages");
            JObject doc = JObject.Parse(pagesJson);
            var pages = doc.GetValue("value").Children();

            foreach (var page in pages)
            {
                if (page["ContentTypeId"].NpValue<string>() == RulePageContentTypeId)
                {
                    LoadRulePage(dataSet, page);
                }
                else if (page["ContentTypeId"].NpValue<string>() == RuleSummaryPageContentTypeId)
                {
                    LoadRuleSummaryPage(dataSet, page);
                }
                else
                {
                    _log.LogWarning("unhandled page content type {contentType}", page["PublishingPageLayout"]["Description"].NpValue<string>());
                }
            }
        }

        private void LoadRuleSummaryPage(SpRulesDataSet dataSet, JToken page)
        {
            var cat = dataSet.CategoryByTitle(page["Title"].NpValue<string>());
            if (cat == null)
            {
                _log.LogWarning("failed to match rule summary page {SummaryPageTitle} to a category from the term store", page["Title"].NpValue<string>());
                return;
            }
            cat.Content = page["PublishingPageContent"].NpValue<string>();
            cat.PageGuid = page["GUID"].NpValue<string>();
        }

        private void LoadRulePage(SpRulesDataSet dataSet, JToken page)
        {
            if (page["Title"].IsNull()) return;
            var ruleData = new RulePage()
            {
                Title = page["Title"].Value<string>().Trim(),
                Id = page["Id"].Value<int>(),
                Content = page["PublishingPageContent"].NpValue<string>(),
                IntroText = page["RuleContentTop"].NpValue<string>(),
                CreatedUtc = page["Created"].Value<DateTime>(),
                ModifiedUtc = page["Modified"].Value<DateTime>(),
                Guid = page["GUID"].Value<string>()
            };
            // follow metadata -> term store reference to set rule->category relationship
            // we will use this relationship with screen scraping later to set ordered category->rules list
            foreach (var catRef in page["RuleCategoriesMetadata"].Children())
            {
                var termStoreRef = catRef["TermGuid"].NpValue<string>();
                var catData = dataSet.Categories.FirstOrDefault(c =>
                    c.TermStoreGuid.Equals(termStoreRef, StringComparison.InvariantCultureIgnoreCase));
                if (catData == null)
                {
                    _log.LogWarning("Failed to resolve category term store giud {TermStoreRef} for rule {RuleTitle}", 
                        termStoreRef, ruleData.Title);
                }
                else
                {
                    ruleData.Categories.Add(catData);
                }
            } // end foreach category reference
            dataSet.Rules.Add(ruleData);
        }

        /// <summary>
        /// example - Communication:Rules to Better Blogging
        /// </summary>
        /// <param name="value">raw path value</param>
        /// <returns>first value from : delimited path, or string.empty</returns>
        private string ExtractParentCategory(string value)
        {
            var data = value.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (data.Length < 2) return string.Empty;
            return data[0];
        }


        private async Task<string> .GetJsonListByTitle(string title)
        {
            var response =
                await _apiHttpClient.GetAsync(
                    $"{_appConfig.SharePointUrl}/_api/Web/Lists/getbytitle('{title}')/items?$top=10000");
            if (!response.IsSuccessStatusCode) throw new Exception($"Http Failed: {response.ReasonPhrase}");
            return await response.Content.ReadAsStringAsync();
        }

    }
    
    
    
    
    
    public static partial class JTokenExtensions
    {
        /// <summary>
        /// null handling as per https://stackoverflow.com/questions/31141527/issue-with-json-null-handling-in-newtonsoft
        /// </summary>
        public static bool IsNull(this JToken token)
        {
            return token == null || token.Type == JTokenType.Null;
        }
        
        /// <summary>
        /// null-propagating wrapper around Value extension method
        /// </summary>
        public static T NpValue<T>(this IEnumerable<JToken> jt)
        {
            return jt == null ? default (T) : jt.Value<T>();
        }
    }
    
    
    
    
}