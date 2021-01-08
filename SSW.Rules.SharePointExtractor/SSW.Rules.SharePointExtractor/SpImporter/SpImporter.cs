using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Permissions;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Publishing.Navigation;
using Microsoft.SharePoint.Client.Sharing;
using Microsoft.SharePoint.Client.Taxonomy;
using Microsoft.SharePoint.Client.WebParts;
using Microsoft.SharePoint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SSW.Rules.SharePointExtractor.Models;
using SSW.Rules.SharePointExtractor.SpRulesListsService;
using SSW.Rules.SharePointExtractor.SpWebPartService;
using SSW.Rules.SharePointExtractor.MdWriter;

namespace SSW.Rules.SharePointExtractor.SpImporter
{
    public class SpImporter: ISpImporter
    {
        
        private readonly ILogger<SpImporter> _log;
        private readonly ApplicationSettings _appSettings;

        private TermCollection _termColl;

        public SpImporter(ILogger<SpImporter> log, ApplicationSettings appSettings)
        {
            _log = log;
            _appSettings = appSettings;
        }


        public SpRulesDataSet Import()
        {
            try
            {
                var data = new SpRulesDataSet();

                using (var spClientContext = CreateClientContext(_appSettings))
                {
                    _termColl = initTermCollection(spClientContext);
                    LoadCategories(data, spClientContext);
                    LoadPages(data, spClientContext);
                    CullEmptyCategories(data);
                    ScrapeHomePage(data).GetAwaiter().GetResult();
                    ScrapeCategoryPages(data).GetAwaiter().GetResult();
                    ScrapeCategoryPages(data, true).GetAwaiter().GetResult();
                    LoadUrlTerms(data, spClientContext);
                }
                return data;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "error importing from sharepoint");
                throw;
            }
            
        }


        public void LoadCategories(SpRulesDataSet dataSet, ClientContext ctx)
        {
            var items = GetAllItems(ctx, "TaxonomyHiddenList");

            foreach (var item in items)
            {
                _log.LogInformation("Category: {Category}", item["Title"]);
                if (item["Title"] != null)
                {
                    // we will allow duplicate names through but will clear out empty categories later
                    dataSet.Categories.Add(new Category()
                    {
                        TermStoreId = Convert.ToInt32(item["ID"]),
                        TermStoreGuid = item["IdForTerm"].ToString(),
                        Title = item["Title"].ToString().Trim(),
                        ParentCategoryTitle = ExtractParentCategory(item["Path"].ToString()),
                    });
                }
            }
        }

        private void LoadUrlTerms(SpRulesDataSet dataSet, ClientContext ctx)
        {           
            var targetUrl = ""; //This is the URL to the .aspx page
            var friendlyUrl = "";
            foreach(var term in _termColl)
            {
                term.LocalCustomProperties.TryGetValue("_Sys_Nav_FriendlyUrlSegment", out friendlyUrl);
                term.LocalCustomProperties.TryGetValue("_Sys_Nav_TargetUrl", out targetUrl);

                if (!String.IsNullOrEmpty(targetUrl))
                {
                    targetUrl = targetUrl.Replace("~sitecollection/Pages/", "");
                }
               
                foreach (RulePage rulePage in dataSet.Rules.Where(r => r.Title.Equals(term.Name) || r.Name.Equals(term.Name.ToLower().Replace(' ','-')) || r.FileName.Equals(targetUrl)))
                {
                    if(!String.IsNullOrEmpty(friendlyUrl))
                    {
                        //If the rule page matches the term and there is an avaible value for the friendly URL, add it to the redirects.
                        rulePage.Redirects.Add(friendlyUrl);
                    }

                    if(!String.IsNullOrEmpty(term.Name) && (!rulePage.Name.ToSharePointUri().Equals(term.Name.ToSharePointUri())))
                    {
                        //Sometimes we won't have a friendly URL and we need to calculate a URL from the term name.
                        rulePage.Redirects.Add(term.Name.ToSharePointUri());
                    }
                }
            }
        }

        private void LoadPages(SpRulesDataSet dataSet, ClientContext ctx)
        {
            _log.LogInformation("Loading  Pages...");
            var web = ctx.Web;

            List oList = ctx.Web.Lists.GetByTitle("Pages");
            ctx.Load(oList);
            ctx.ExecuteQuery();
            _log.LogInformation("got list id" + oList.Id);


            CamlQuery camlQuery = new CamlQuery();
            camlQuery.ViewXml = "<View><Query><Where></Where></Query><RowLimit>5000</RowLimit></View>";
            ListItemCollection items = oList.GetItems(camlQuery);
            ctx.Load(items); 
            ctx.ExecuteQuery();

            int count = 0;
            foreach (var item in items)
            {
                count++;
                var contentType = item.ContentType;
                ctx.Load(item);
                ctx.Load(contentType);
                var file = ctx.Web.GetFileByServerRelativeUrl($"/Pages/{item["FileLeafRef"]}");
                ctx.Load(file);
                ctx.ExecuteQuery();

                if (item.ContentType.Name == "RulePage")
                {
                    _log.LogInformation($"Rule Page {item["Title"]} - {count} of {items.Count}");
                    var rulePage = LoadRulePage(dataSet, ctx, item);
                    if (rulePage != null)
                    {
                        LoadContentHistory(rulePage, ctx, item);
                    }
                }
                else if (item.ContentType.Name == "RuleSummaryPage")
                {
                    _log.LogInformation("Summary Page " + item["Title"]);
                    var category = LoadRuleSummaryPage(dataSet, item, ctx);
                    if (category != null)
                    {
                        LoadContentHistory(category, ctx, item);
                    }
                }
                else
                {
                    _log.LogInformation("unhandled page content type {contentType}", item.ContentType.Name);
                }

                // DEBUG uncomment this for testing with a smaller amount of data
                //if (count > 100) break;
            }
        }


        /// <summary>
        ///  need to hit 2 different services in order to get full history: CSOM and lists.asmx
        /// </summary>
        /// <param name="contentItem"></param>
        /// <param name="ctx"></param>
        /// <param name="item"></param>
        private void LoadContentHistory(IHasContentHistory contentItem, ClientContext ctx, ListItem item)
        {
            _log.LogInformation("load history for item {item} ", item.Id);

            var introTextFieldName = (item.ContentType.Name == "RuleSummaryPage") ? "RuleSummaryIntro" : "RuleContentTop";

            var file = ctx.Web.GetFileByServerRelativeUrl($"/Pages/{item["FileLeafRef"]}");
            ctx.Load(file);

            var versions = file.Versions;
            ctx.Load(versions);
            var oldVersions = ctx.LoadQuery(versions.Where(v => v != null));
            ctx.ExecuteQuery();

            var svc = new ListsSoapClient();
            svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_appSettings.Username, _appSettings.Password, "SSW2000");

            XNamespace xmlns = "http://schemas.microsoft.com/sharepoint/soap/";

            var fields = new List<string> {"Title", introTextFieldName, "PublishingPageContent" };
            // we need to fetch history for each field individually - and we might not have data for every record reported by the "Version" field
            // build this data into a 2-level dictionary fieldName->modifiedDate->Value
            // use of modified date key ensures we map incomplete datasets to correct versions
            var fieldHistoryData = new Dictionary<string, Dictionary<string, string>>();
            foreach (var field in fields)
            {
                fieldHistoryData[field] = new Dictionary<string, string>();
                var fieldVersionsXml = svc.GetVersionCollection("Pages", item.Id.ToString(), field);
                var elements = fieldVersionsXml.Descendants(xmlns + "Version");
                foreach (var element in elements) // note: I started with a ToDictionary() here but we need to handle duplicate 'modified' value in source data
                {
                    var modified = element.Attribute("Modified")?.Value;
                    if (!string.IsNullOrWhiteSpace(modified) && !fieldHistoryData[field].ContainsKey(modified))
                    {
                        fieldHistoryData[field][modified] = element.Attribute(field)?.Value;
                    }
                }
            }

            // now fetch all data for the "Version" field to make ContentVersion objects - adding fields from fieldHistoryData where we can
            var versionsXml = svc.GetVersionCollection("Pages", item.Id.ToString(), "Version");
            var contentVersions = versionsXml.Descendants(xmlns + "Version")
                .Select(v => new ContentVersion()
                {
                    VersionLabel = v.Attribute("Version")?.Value,
                    Comment = oldVersions.FirstOrDefault(x => x.VersionLabel == v.Attribute("Version")?.Value)
                        ?.CheckInComment,
                    ModifiedUtc = DateTime.Parse(v.Attribute("Modified")?.Value),
                    ModifiedBy = v.Attribute("Editor")?.Value?.Split(new char[] {','})[2],
                    ModifiedByFullName = v.Attribute("Editor")?.Value?.Split(new char[] { ',' })[4],
                    Title = fieldHistoryData.ValueOrNull("Title")?.ValueOrNull(v.Attribute("Modified")?.Value),
                    IntroText = fieldHistoryData.ValueOrNull(introTextFieldName)?.ValueOrNull(v.Attribute("Modified")?.Value),
                    Content = fieldHistoryData.ValueOrNull("PublishingPageContent")?.ValueOrNull(v.Attribute("Modified")?.Value),
                }).ToList();

            contentItem.Versions = contentVersions;
        }



        private Category LoadRuleSummaryPage(SpRulesDataSet dataSet, ListItem page, ClientContext ctx)
        {
            var cat = dataSet.CategoryByTitle(page["Title"]?.ToString());
            if (cat == null)
            {
                _log.LogWarning("failed to match rule summary item {SummaryPageTitle} to a category from the term store", page["Title"]);
                return null;
            }

            cat.Content = page["PublishingPageContent"]?.ToString();
           
            cat.IntroText = page["RuleSummaryIntro"]?.ToString();
            cat.PageGuid = page["GUID"]?.ToString();
            cat.ImageUrls.UnionWith(GetImageUrls(cat.Content));

            ExtractWebParts(cat, page, ctx);

            if (cat.Content != null)
            {
                MatchEvaluator matchEval = new MatchEvaluator(ReplaceRelativeURl);
                cat.Content = Regex.Replace(cat.Content, @"(""(\/_layouts\/15\/FIXUPREDIRECT.ASPX).*""\s)", matchEval);
            }

            return cat;
        }

        private void ExtractWebParts(Category cat, ListItem page, ClientContext ctx)
        {
            var file = ctx.Web.GetFileByServerRelativeUrl($"/Pages/{page["FileLeafRef"]}");
            ctx.Load(file);
            ctx.ExecuteQuery();
            // look for any web parts under this page.
            var wpManager = file.GetLimitedWebPartManager(PersonalizationScope.Shared);
            var webParts = wpManager.WebParts;
            ctx.Load(webParts);
            ctx.ExecuteQuery();

            if (webParts.Count > 0)
            {
                _log.LogInformation("Processing {Count} WebParts", webParts.Count);
                foreach (var webPart in webParts)
                {
                    _log.LogInformation("got web part id: {Id}", webPart.Id);
                    var svc = new WebPartPagesWebServiceSoapClient();
                    svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_appSettings.Username, _appSettings.Password, "SSW2000");

                    var webPartXmlString = svc.GetWebPart2(file.ServerRelativeUrl, webPart.Id, SpWebPartService.Storage.Shared, SPWebServiceBehavior.Version3);
                    var xmlDoc = XDocument.Parse(webPartXmlString);
                    var contentElement = xmlDoc.Root.Elements().FirstOrDefault(e => e.Name.LocalName == "Content");
                    if (contentElement != null)
                    {
                        cat.Content = contentElement.Value;
                        if (cat.Content != null)
                        {
                            MatchEvaluator matchEval = new MatchEvaluator(ReplaceRelativeURl);
                            cat.Content = Regex.Replace(cat.Content, @"(""(\/_layouts\/15\/FIXUPREDIRECT.ASPX).*""\s)", matchEval);
                        }
                        _log.LogInformation($"got web part content {contentElement.Value}");
                    }
                }
            }

        }

        private RulePage LoadRulePage(SpRulesDataSet dataSet, ClientContext ctx, ListItem item)
        {
            if (string.IsNullOrEmpty(item["Title"]?.ToString())) return null;


            var rulePage = new RulePage()
            {
                ArchivedReason = item["ObsoleteReason"]?.ToString(),
                Title = item["Title"].ToString().Trim(),
                Id = Convert.ToInt32(item["ID"]),
                Content = item["PublishingPageContent"]?.ToString(),
                IntroText = item["RuleContentTop"]?.ToString(),
                RulesKeyWords = item["RulesKeyWords"]?.ToString(),
                CreatedUtc = (DateTime)item["Created"],
                ModifiedUtc = (DateTime)item["Modified"],
                Guid = item["GUID"].ToString(),
                FileName = item["FileLeafRef"].ToString()
            };

            

            RulePageAuthors(item, dataSet, rulePage);
            RulePageRelated(item, dataSet, rulePage);

            rulePage.ImageUrls.UnionWith(GetImageUrls(rulePage.IntroText));
            rulePage.ImageUrls.UnionWith(GetImageUrls(rulePage.Content));

            // I could not work out how to drill into the RuleCategoriesMetaData object other than serializing to json and parsing via JObject
            var metadataJason = JsonConvert.SerializeObject(item["RuleCategoriesMetadata"]);
            // follow metadata -> term store reference to set rule->category relationship

            var jArray = JArray.Parse(metadataJason);

            foreach(var elt in jArray)
            {
                var termGuid = elt["TermGuid"].NpValue<string>();
                var catData = dataSet.Categories.FirstOrDefault(c =>
                    c.TermStoreGuid.Equals(termGuid, StringComparison.InvariantCultureIgnoreCase));
                if (catData != null)
                {
                    rulePage.Categories.Add(catData);
                }
            }

            /* var jObject = JObject.Parse(metadataJason); 
           
            foreach (var reference in jObject["_Child_Items_"].Children())
            {
                //_log.LogInformation("link to {REF}", reference);
                
                var termGuid = reference["TermGuid"].NpValue<string>();
                var catData = dataSet.Categories.FirstOrDefault(c =>
                    c.TermStoreGuid.Equals(termGuid, StringComparison.InvariantCultureIgnoreCase));
                if (catData != null)
                {
                    rulePage.Categories.Add(catData);
                }
            }*/
           
            if (rulePage.Content != null)
            {
                MatchEvaluator matchEval = new MatchEvaluator(ReplaceRelativeURl);
                rulePage.Content = Regex.Replace(rulePage.Content, @"(""(\/_layouts\/15\/FIXUPREDIRECT.ASPX)[^""]*"")", matchEval);
            }

            dataSet.Rules.Add(rulePage);
            return rulePage;
        }

        private string ReplaceRelativeURl(Match match)
        {
            string uri = match.Value.Substring(match.Value.IndexOf("\"") + 1);
            uri = uri.Substring(0, uri.IndexOf("\""));
            var paramstring = uri.Substring(uri.LastIndexOf("?") + 1);
            var sep = new string[] { "&amp;" };
            string[] parameters = paramstring.Split(sep, StringSplitOptions.None);
            var paramNameValue = new Dictionary<string, string>();
            foreach (var p in parameters)
            {
                var param = p.Split('=');
                paramNameValue.Add(param[0], param[1]);
            }
    
            var term = _termColl.Where(t => t.Id == Guid.Parse(paramNameValue["TermId"])).FirstOrDefault();
            if(term != null)
            {
                var newUri = RuleExtensions.ToFileName(term.Name);
                return "/" + newUri;
            } else {
                _log.LogWarning("Couldn't resolve SharePoint Relative URL: {SpRelativeUrl} from the term store", match.Value);
                return match.Value;
            }
        }

        private TermCollection initTermCollection(ClientContext ctx)
        {
            TaxonomySession taxonomySession = TaxonomySession.GetTaxonomySession(ctx);
            TermStore termStore = taxonomySession.TermStores.GetByName("Managed Metadata Service");
            TermGroup termGroup = termStore.Groups.GetByName("Site Collection - rules.ssw.com.au");
            TermSet termSet = termGroup.TermSets.GetByName("Home Navigation");
            TermCollection termColl = termSet.Terms;
            ctx.Load(termColl);
            ctx.ExecuteQuery();
            return termColl;
        }

        /// <summary>
        /// parse html content to get referenced image urls
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private HashSet<string> GetImageUrls(string content)
        {
            var result = new HashSet<string>();
            if (content == null) return result;

            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            if (doc.DocumentNode == null) return result;

            var imageNodes = doc.DocumentNode.SelectNodes("//img");
            if (imageNodes == null) return result;

            foreach (var imgNode in imageNodes)
            {
                var src = imgNode.Attributes["src"]?.Value;
                if (src == null) continue;
                result.Add(WebUtility.HtmlDecode(src));
            }

            return result;
        }


        /// <summary>
        /// process authors for a rule page
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dataSet"></param>
        /// <param name="rulePage"></param>
        private void RulePageAuthors(ListItem item, SpRulesDataSet dataSet, RulePage rulePage)
        {
            var employeeRefs = item["EmployeeLookup"] as FieldLookupValue[];
            if (employeeRefs == null || employeeRefs.Length < 1)
            {
                return;
            }
            foreach (var employeeRef in employeeRefs)
            {
                var employee = dataSet.Employees.FirstOrDefault(e => e.Id == employeeRef.LookupId);
                if (employee == null)
                {
                    employee = new Employee()
                    {
                        Id = employeeRef.LookupId,
                        Title = employeeRef.LookupValue
                    };
                    dataSet.Employees.Add(employee);
                }
                rulePage.Employees.Add(employee);
            }
        }

        /// <summary>
        /// process related rules for a rule page
        /// </summary>
        /// <param name="item"></param>
        /// <param name="dataSet"></param>
        /// <param name="rulePage"></param>
        private void RulePageRelated(ListItem item, SpRulesDataSet dataSet, RulePage rulePage)
        {
            //Check if the rule has any keywords for related rules
            if (string.IsNullOrEmpty(item["RulesKeyWords"]?.ToString())) return;

            string url = rulePage.Name;
            var web = new HtmlWeb();

            WebClient webClient = new WebClient();

            var ruleKeyWords = item["RulesKeyWords"]?.ToString().Split(';');
            XNamespace xmlns = "http://schemas.microsoft.com/ado/2007/08/dataservices";

            foreach (var keyWord in ruleKeyWords)
            {
                try
                {
                    var relatedUri = _appSettings.SharePointUrl + "/_api/web/lists/getByTitle('Pages')/Items?$filter=GUID ne '" + rulePage.Guid + "' and substringof('" + keyWord + "',RulesKeyWords)&$select=GUID,Title,RulesKeyWords,FileRef";
                    var relatedXmlPageString = webClient.DownloadString(relatedUri);

                    var xmlDoc = XDocument.Parse(relatedXmlPageString);
                    var relatedRulesNodes = xmlDoc.Descendants(xmlns + "RulesKeyWords");

                    foreach (var node in relatedRulesNodes)
                    {
                        var keywordsArray = node.Value.Trim().Split(';');
                        if (keywordsArray.Contains(keyWord))
                        {
                            var relatedRuleTitle = node.Parent.Descendants(xmlns + "Title").FirstOrDefault();
                            var relatedRuleUri = relatedRuleTitle.Value.ToLowerInvariant().Replace(' ', '-');
                            relatedRuleUri = Regex.Replace(relatedRuleUri, "[?().:]", "");
                            rulePage.Related.Add(relatedRuleUri);
                        }
                    }
                }
                catch (WebException ex)
                {
                    _log.LogWarning("ignored http error when fetching related rules: ", ex);
                }
            }
            rulePage.Related = rulePage.Related.Distinct().ToList();
        }

        /// <summary>
        /// example - Communication:Rules to Better Blogging
        /// </summary>
        /// <param name="value">raw path value</param>
        /// <returns>first value from : delimited path, or string.empty</returns>
        private string ExtractParentCategory(string value)
        {
            var data = value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (data.Length < 2) return string.Empty;
            return data[0];
        }

        public static ClientContext CreateClientContext(ApplicationSettings appSettings)
        {
            var ctx = new ClientContext(appSettings.SharePointUrl);
            ctx.Credentials = new NetworkCredential(appSettings.Username, appSettings.Password, appSettings.Domain);
            ctx.ExecutingWebRequest += new EventHandler<WebRequestEventArgs>(SpClientContext_CustomHeaders);
            return ctx;
        }


        private static void SpClientContext_CustomHeaders(object sender, WebRequestEventArgs e)
        {
            try
            {
                e.WebRequestExecutor.WebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }




        public static ListItemCollection GetAllItems(ClientContext clientContext, string libraryTitle)
        {
            List oList = clientContext.Web.Lists.GetByTitle(libraryTitle);
            
            CamlQuery camlQuery = new CamlQuery();
            camlQuery.ViewXml = "<View><Query><Where></Where></Query><RowLimit>5000</RowLimit></View>";
            ListItemCollection collListItems = oList.GetItems(camlQuery);
            clientContext.Load(collListItems);
            clientContext.ExecuteQuery();

            return collListItems;
        }





        private void CullEmptyCategories(SpRulesDataSet dataSet)
        {
            var allCategoriesWithRules = dataSet.Rules.SelectMany(r => r.Categories).Distinct();
            dataSet.Categories = dataSet.Categories.Where(c => allCategoriesWithRules.Contains(c)).ToList();
        }



        /// <summary>
        /// we screen-scrape every category summary item to get the ordering of rule items within a category
        /// </summary>
        /// <param name="dataSet">data set we're building</param>
        public async Task ScrapeCategoryPages(SpRulesDataSet dataSet, bool archived = false)
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
                    .Where(r => r.Categories.Select(c => c.TermStoreId).ToList().Contains(cat.TermStoreId))
                    .ToList();

                if (!categoryRules.Any())
                {
                    _log.LogWarning("category {Title} at {Uri} has no rules", cat.Title, cat.Uri);
                    continue;
                }

                _log.LogInformation("Screen scrape of category {CatTitle}", cat.Title);
                HtmlWeb web = new HtmlWeb();
                var uri = cat.Uri.ToString();
                if (archived)
                {
                    uri += "?showarchived=True";
                }
                var doc = await web.LoadFromWebAsync(_appSettings.SharePointUrl + uri);

                var ruleNodes = doc.DocumentNode.SelectNodes(
                    ".//*[@id='ctl00_PlaceHolderMain_RuleSummaryUC_SSWRuleSummaryUCDiv']/div/ol/li");

                if (ruleNodes == null)
                {
                    _log.LogWarning("no rule links found for {Title} on item {Url}", cat.Title, cat.Uri);
                    continue;
                }

                foreach (var ruleNode in ruleNodes)
                {
                    var linkNode = ruleNode.SelectSingleNode("h2/a");
                    var title = linkNode.InnerText.Trim();
                    var ruleData = categoryRules.FirstOrDefault(r =>
                        r.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase)); ;
                    if (ruleData == null)
                    {
                        _log.LogWarning("Failed to find rule {RuleTitle} under category {CategoryTitle}", title, cat.Title);
                        _log.LogWarning("Available rules titles: {RuleTitles}", categoryRules.Select(r => r.Title).ToList());
                    }
                    else
                    {
                        cat.Rules.Add(ruleData);
                    }
                } // end foreach rule link in summary item

            } // end foreach category
        }


        /// <summary>
        /// SharePoint sucks so hard that I abandoned attempting to talk to the term store and just scraped details from the rules site html instead
        /// </summary>
        /// <param name="dataSet">the dataset we're building</param>
        public async Task ScrapeHomePage(SpRulesDataSet dataSet)
        {
            _log.LogInformation("Scraping category details from home item...");
            HtmlWeb web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(_appSettings.SharePointUrl);
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


    public static class DictionaryExtensions
    {
        /// <summary>
        /// dictionary lookup that returns null when key is not found.
        /// Based on https://stackoverflow.com/a/33223183
        /// </summary>
        /// <typeparam name="TK"></typeparam>
        /// <typeparam name="TV"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TV ValueOrNull<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default(TV))
        {
            if (key == null) return defaultValue;
            TV value;
            return dict.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
    
    
    
    
}