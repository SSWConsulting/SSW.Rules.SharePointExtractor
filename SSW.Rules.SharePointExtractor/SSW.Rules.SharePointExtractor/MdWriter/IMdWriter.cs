using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SSW.Rules.SharePointExtractor.Converter;
using SSW.Rules.SharePointExtractor.Helpers;
using SSW.Rules.SharePointExtractor.MdWriter.FrontMatterModels;
using SSW.Rules.SharePointExtractor.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SSW.Rules.SharePointExtractor.MdWriter
{

    public interface IMdWriter
    {
        void WriteMarkdown(SpRulesDataSet data);
    }


    public class MdWriterConfig
    {
        public string TargetRepository { get; set; }
        public string AssetsFolder { get; set; }
        public string RulesFolder { get; set; }
        public string CategoriesFolder { get; set; }
        public bool FetchImages { get; set; }
        public bool ProcessHistory { get; set; }
        public bool WriteRules { get; set; }
        public bool WriteCategories { get; set; }
        public bool WriteJsonFileHistory { get; set; }
        public string CategoriesFolderFull => Path.Combine(TargetRepository, CategoriesFolder);
        public string RulesFolderFull => Path.Combine(TargetRepository, RulesFolder);
        public string AssetsFolderFull => Path.Combine(TargetRepository, AssetsFolder);
    }


    public class MdWriter : IMdWriter
    {
        private readonly MdWriterConfig _config;
        private readonly ILogger<MdWriter> _log;

        public static readonly ISerializer YamlSerializer = new SerializerBuilder()
            .WithNamingConvention(LowerCaseNamingConvention.Instance)
            .Build();

        public MdWriter(ILogger<MdWriter> log, MdWriterConfig config = null)
        {
            _log = log;
            _config = config ?? DefaultConfig;
        }

        public static MdWriterConfig DefaultConfig = new MdWriterConfig()
        {
            TargetRepository = @"D:\Code\SSW\Rules\SSW.Rules",
            RulesFolder = @"rules",
            CategoriesFolder = @"categories",
            AssetsFolder = @"assets",
            WriteCategories = true,
            FetchImages = true,
            WriteRules = true,
            ProcessHistory = true,
            WriteJsonFileHistory = true
        };


        public void WriteMarkdown(SpRulesDataSet data)
        {
            try
            {
                if (!Directory.Exists(Path.Combine(_config.RulesFolderFull))) Directory.CreateDirectory(_config.RulesFolderFull);
                if (!Directory.Exists(Path.Combine(_config.CategoriesFolderFull))) Directory.CreateDirectory(_config.CategoriesFolderFull);
                if (!Directory.Exists(Path.Combine(_config.AssetsFolderFull))) Directory.CreateDirectory(_config.AssetsFolderFull);

                if (_config.FetchImages)
                {
                    FetchImages(data);
                }

                ProcessImageUrls(data);

                if (_config.WriteCategories)
                {
                    WriteIndexPage(data);
                    WriteParentCategories(data);
                    WriteCategoryPages(data);
                }

                if (_config.WriteRules)
                {
                    WriteRules(data);
                }

                if (_config.WriteJsonFileHistory)
                {
                    List<HistoryInfos> list = new List<HistoryInfos>();
                    list = WriteRulesJsonFileHistory(data);
                    list = list.Concat(WriteCategoriesJsonFileHistory(data)).ToList();
                    string json = JsonConvert.SerializeObject(list);
                    //write string to file
                    string fileName = "history.json";
                    System.IO.File.WriteAllText(Path.Combine(_config.TargetRepository, "history.json"), json);
                    //commit file
                    GitCommit(
                        _config.TargetRepository,
                        $"Extracted from Sharepoint to Git",
                        new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                        new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                       fileName);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex,
                    "Exception thrown while writing output");
                throw new ApplicationException(
                    "Error writing markdown",
                    ex);
            }

        }

        private List<HistoryInfos> WriteRulesJsonFileHistory(SpRulesDataSet data)
        {
            _log.LogInformation($"Writing History Json File");


            List<HistoryInfos> infos = new List<HistoryInfos>();
            foreach (var rule in data.Rules)
            {
                var rulePaths = new RulePaths(_config, rule);
                var creationData = rule.Versions
                 .Where(v => !string.IsNullOrWhiteSpace(v.IntroText) || !string.IsNullOrWhiteSpace(v.Content)) // filter out where we have no content
                 .OrderBy(v => v.ModifiedUtc).FirstOrDefault();

                var lastModifiedData = rule.Versions
                .Where(v => !string.IsNullOrWhiteSpace(v.IntroText) || !string.IsNullOrWhiteSpace(v.Content)) // filter out where we have no content
                .OrderByDescending(v => v.ModifiedUtc).FirstOrDefault();
                var info = new HistoryInfos
                {
                    file = rulePaths.RuleFileRelative.Replace("\\", "/"),
                    created = creationData.ModifiedUtc,
                    createdBy = creationData.ModifiedByDisplayName,
                    createdByEmail = creationData.ModifiedByEmail,
                    lastUpdated = lastModifiedData.ModifiedUtc,
                    lastUpdatedBy = lastModifiedData.ModifiedByDisplayName,
                    lastUpdatedByEmail = lastModifiedData.ModifiedByEmail
                };
                infos.Add(info);
            }
            return infos;

        }

        private List<HistoryInfos> WriteCategoriesJsonFileHistory(SpRulesDataSet data)
        {
            _log.LogInformation($"Writing History Json File");

            List<HistoryInfos> infos = new List<HistoryInfos>();
            foreach (var cat in data.Categories)
            {
                if (cat.Versions.Count == 0)
                    continue;
                var catPath = new CategoryPaths(_config, cat);
                var creationData = cat.Versions
                 .OrderBy(v => v.ModifiedUtc).FirstOrDefault();

                var lastModifiedData = cat.Versions
                .OrderByDescending(v => v.ModifiedUtc).FirstOrDefault();
                var info = new HistoryInfos
                {
                    file = catPath.CategoryFileRelative.Replace("\\", "/"),
                    created = creationData.ModifiedUtc,
                    createdBy = creationData.ModifiedByDisplayName,
                    createdByEmail = creationData.ModifiedByEmail,
                    lastUpdated = lastModifiedData.ModifiedUtc,
                    lastUpdatedBy = lastModifiedData.ModifiedByName,
                    lastUpdatedByEmail = lastModifiedData.ModifiedByDisplayName
                };
                infos.Add(info);
            }

            return infos;
        }

        private void WriteRules(SpRulesDataSet data)
        {
            _log.LogInformation($"Writing {data.Rules.Count} rules");
            // process rules
            foreach (var rule in data.Rules)
            {
                //Check for Related Rules
                if (rule.Related.Count > 0)
                {
                    var tempRelated = rule.Related;
                    foreach(var relatedRule in tempRelated)
                    {
                        //Check to make sure it's the correct URI
                        var ruleUri = data.Rules.Where(r => r.Redirects.Contains(relatedRule)).ToList();
                        if(ruleUri.Count > 0)
                        {
                            //Update the Uri in the Related rules list
                            rule.Related = rule.Related.Select(s => s.Replace(relatedRule, ruleUri[0].GetRuleUri())).ToList();
                        }
                    }
                    var relatedRules = rule.Related;
                }

                if (!String.IsNullOrEmpty(rule.IntroText))
                {
                    rule.IntroText = Helpers.EncodedHtmlTags.Decode(rule.IntroText);
                }

                if (!String.IsNullOrEmpty(rule.Content))
                {
                    rule.Content = Helpers.EncodedHtmlTags.Decode(rule.Content);
                }
                
                WriteRule(rule);
            }
        }


        private void WriteCategoryPages(SpRulesDataSet data)
        {
            foreach (var cat in data.Categories)
            {
                if (!cat.Rules.Any()) continue;
                var catPaths = new CategoryPaths(_config, cat);


                if (_config.ProcessHistory)
                {
                    ProcessCategoryHistory(cat);
                }


                var html = $@"
{cat.IntroText}
{cat.Content}
";


                string markdown =
                            $@"---
{YamlSerializer.Serialize(new CategoryMdModel(cat))}
---
{ MarkdownConverter.Convert(html)}

";
                _log.LogInformation($"writing {catPaths.CategoryFileFull}");
                using (var sw = new StreamWriter(catPaths.CategoryFileFull, false))
                {
                    sw.Write(markdown);
                    sw.Flush();
                }
                if (_config.ProcessHistory)
                {
                    GitCommit(
                        _config.TargetRepository,
                        $"Extracted from Sharepoint to Git",
                        new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                        new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                        catPaths.CategoryFileRelative);
                }

            }
        }



        private void WriteParentCategories(SpRulesDataSet data)
        {
            foreach (var pc in data.ParentCategories)
            {
                var pcFolder = pc.Title.ToFileName();
                var pcFile = Path.Combine(pcFolder, "index.md");
                var pcFolderFull = Path.Combine(_config.CategoriesFolderFull, pcFolder);
                var pcFileFull = Path.Combine(_config.CategoriesFolderFull, pcFile);
                if (!Directory.Exists(pcFolderFull)) Directory.CreateDirectory(pcFolderFull);


                var markdown = $@"---
{YamlSerializer.Serialize(new ParentCategoryMdModel(pc))}
---

";
                _log.LogInformation($"writing {pcFileFull}");
                using (var sw = new StreamWriter(pcFileFull, false))
                {
                    sw.Write(markdown);
                    sw.Flush();
                }
                if (_config.ProcessHistory)
                {
                    GitCommit(
                        _config.TargetRepository,
                        $"Extracted from SharePoint",
                        new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                        new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                        "*");
                }
            }
        }

        private void WriteIndexPage(SpRulesDataSet data)
        {
            var indexPath = Path.Combine(_config.CategoriesFolder, "index.md");
            var indexPathFull = Path.Combine(_config.TargetRepository, indexPath);

            var markdown = $@"---
{YamlSerializer.Serialize(new IndexMdModel(data))}
---

";
            _log.LogInformation("writing index");
            using (var sw = new StreamWriter(indexPathFull, false))
            {
                sw.Write(markdown);
                sw.Flush();
            }

            if (_config.ProcessHistory)
            {
                GitCommit(
                    _config.TargetRepository,
                    $"Extracted from SharePoint",
                    new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                    new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                    "*");
            }
        }

        private void WriteRule(RulePage rule)
        {
            var tempRule = rule.ToFrontMatter(); //Hack to make sure the redirect Urls are populated

            var rulePaths = new RulePaths(_config, rule);

            if (!Directory.Exists(rulePaths.RuleFolderFull)) Directory.CreateDirectory(rulePaths.RuleFolderFull);

            _log.LogInformation("writing file {Path}", rulePaths.RuleFileFull);

            if (_config.ProcessHistory)
            {
                ProcessRuleHistory(rule);
            }

            // Write final HTML Version
            using (var writer = new StreamWriter(rulePaths.RuleFileFull, false))
            {
                writer.Write(rule.ToMarkdown(true));
                writer.Flush();
            }
            if (_config.ProcessHistory)
            {
                GitCommit(
                _config.TargetRepository,
                $"Extracted from Sharepoint to Git",
                new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                rulePaths.RuleFileRelative);
            }

            // Write final Markdown version
            using (var writer = new StreamWriter(rulePaths.RuleFileFull, false))
            {
                writer.Write(rule.ToMarkdown(false));
                writer.Flush();
            }
            if (_config.ProcessHistory)
            {
                GitCommit(
                _config.TargetRepository,
                $"Converted to Markdown",
                new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                rulePaths.RuleFileRelative);
            }

        }

        private void ProcessRuleHistory(RulePage rule)
        {
            var rulePaths = new RulePaths(_config, rule);

            bool isFirst = true;
            foreach (var version in rule.Versions
                .Where(v => !string.IsNullOrWhiteSpace(v.IntroText) || !string.IsNullOrWhiteSpace(v.Content)) // filter out where we have no content
                .OrderBy(v => v.ModifiedUtc))
            {
                using (var writer = new StreamWriter(rulePaths.RuleFileFull, false))
                {
                    writer.Write(version.ToMarkdown(rule));
                    writer.Flush();
                }

                var gitComment = $"{version.VersionLabel} - {version.Comment}";
                // if the first version label isn't 1.0, the history was truncated by SharePoint. add comment.
                if (isFirst && !version.VersionLabel.StartsWith("1.0"))
                {
                    gitComment = gitComment +
                                 " Note: previous versions of this content may have been deleted by SharePoint.";
                }

                var sw = new Stopwatch();
                sw.Start();
                GitCommit(
                    _config.TargetRepository,
                    gitComment,
                    new LibGit2Sharp.Signature(version.ModifiedByName, version.ModifiedByEmail, version.ModifiedUtc),
                    new LibGit2Sharp.Signature(version.ModifiedByName, version.ModifiedByEmail, version.ModifiedUtc),
                    rulePaths.RuleFileRelative);
                sw.Stop();
                _log.LogInformation($"{version.VersionLabel} committed in {sw.Elapsed.TotalMilliseconds}ms");

                isFirst = false;
            }
        }




        private void ProcessCategoryHistory(Category cat)
        {
            var catPaths = new CategoryPaths(_config, cat);

            bool isFirst = true;
            var versions = cat.Versions
                .Where(v => !string.IsNullOrWhiteSpace(v.IntroText) ||
                            !string.IsNullOrWhiteSpace(v.Content)) // filter out where we have no content
                .OrderBy(v => v.ModifiedUtc);
            foreach (var version in versions)
            {

                var markdown = $@"---
{YamlSerializer.Serialize(new CategoryMdModel(cat))}
---
{version.IntroText}
{version.Content}

";
                using (var writer = new StreamWriter(catPaths.CategoryFileFull, false))
                {

                    writer.Write(markdown);
                    writer.Flush();
                }

                var gitComment = $"{version.VersionLabel} - {version.Comment}";
                // if the first version label isn't 1.0, the history was truncated by SharePoint. add comment.
                if (isFirst && !version.VersionLabel.StartsWith("1.0"))
                {
                    gitComment = gitComment +
                                 " Note: previous versions of this content may have been deleted by SharePoint.";
                }

                var sw = new Stopwatch();
                sw.Start();
                GitCommit(
                    _config.TargetRepository,
                    gitComment,
                    new LibGit2Sharp.Signature(version.ModifiedByName, version.ModifiedByEmail, version.ModifiedUtc),
                    new LibGit2Sharp.Signature(version.ModifiedByName, version.ModifiedByEmail, version.ModifiedUtc),
                    catPaths.CategoryFileRelative);
                sw.Stop();
                _log.LogInformation($"{version.VersionLabel} committed in {sw.Elapsed.TotalMilliseconds}ms");

                isFirst = false;
            }
        }



        private void FetchImages(SpRulesDataSet data)
        {
            foreach (var rule in data.Rules.Where(r => r.ImageUrls.Count > 0))
            {
                FetchImages(rule.ImageUrls, new RulePaths(_config, rule).RuleFolderFull);
                FetchImages(HtmlHelper.GetImageUrls(rule.IntroText), new RulePaths(_config, rule).RuleFolderFull);
                FetchImages(HtmlHelper.GetImageUrls(rule.Content), new RulePaths(_config, rule).RuleFolderFull);
            }

            foreach (var category in data.Categories)
            {
                if (category.ImageUrls.Count > 0)
                    FetchImages(category.ImageUrls, new CategoryPaths(_config, category).CategoryFolderFull);

                var imgsIntro = HtmlHelper.GetImageUrls(category.IntroText);
                if (imgsIntro.Count > 0)
                    FetchImages(imgsIntro, new CategoryPaths(_config, category).CategoryFolderFull);

                var imgsContent = HtmlHelper.GetImageUrls(category.Content);
                if (imgsContent.Count > 0)
                    FetchImages(imgsContent, new CategoryPaths(_config, category).CategoryFolderFull);
            }

            if (_config.ProcessHistory)
            {
                GitCommit(
                _config.TargetRepository,
                $"Extracted Images from Sharepoint to Git",
                new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                new LibGit2Sharp.Signature("SSW.Rules.SharePointExtractor", "SSW.Rules.SharePointExtractor@ssw.com.au", DateTime.UtcNow),
                "*");
            }
        }

        /// <summary>
        /// download a rule's images and place in a per-rule folder.
        /// </summary>

        private void FetchImages(HashSet<string> imageUrls, string imageFolder)
        {
            if (!imageUrls.Any()) return;

            //var imageFolder = new RulePaths(_config, rule).RuleFolderFull;
            if (!Directory.Exists(imageFolder)) Directory.CreateDirectory(imageFolder);

            var webClient = new WebClient();
            foreach (var url in imageUrls)
            {
                var imgUrl = WebUtility.HtmlDecode(url).Replace("%5c", "/");
                var outputPath = "";
                if (imgUrl.StartsWith("/"))
                {
                    imgUrl = "https://rules.ssw.com.au" + url;

                    var filename = ImageFilenameFromUrl(imgUrl);
                    outputPath = Path.Combine(imageFolder, filename);
                }
                else
                {
                    var filename = ImageFilenameFromUrl(imgUrl);
                    outputPath = Path.Combine(_config.AssetsFolderFull, filename);
                }

                if (!File.Exists(outputPath))
                {
                    try
                    {
                        _log.LogInformation("Getting {Url} to {FileName}", imgUrl, outputPath);
                        webClient.DownloadFile(imgUrl, outputPath);

                        if (outputPath.EndsWith(".bmp"))
                        {
                            Bitmap bmp1 = new Bitmap(outputPath);
                            bmp1.Save(outputPath.Replace(".bmp", ".png"), ImageFormat.Png);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "failed to fetch {URL} to {FileName}", imgUrl, outputPath);
                    }
                }
            }
        }


        private void ProcessImageUrls(SpRulesDataSet data)
        {
            foreach (var rule in data.Rules)
            {
                UpdateImageUrlsInRule(rule);
            }

            foreach (var category in data.Categories)
            {
                HashSet<string> images = new HashSet<string>();
                if (category.ImageUrls.Count > 0)
                    images.UnionWith(category.ImageUrls);

                var imgsIntro = HtmlHelper.GetImageUrls(category.IntroText);
                if (imgsIntro.Count > 0)
                    images.UnionWith(imgsIntro);

                var imgsContent = HtmlHelper.GetImageUrls(category.Content);
                if (imgsContent.Count > 0)
                    images.UnionWith(imgsContent);
                UpdateImageUrlsInCategory(category, images);
            }
        }

        private string ImageFilenameFromUrl(string imgUrl)
        {
            var uriObj = new Uri(imgUrl);
            var filename = WebUtility.UrlDecode(uriObj.Segments.Last());
            if (!filename.Contains("."))
            {
                filename += ".jpg";
            }
            return filename;
        }

        private string GetImageFilename(string url)
        {
            var imgUrl = WebUtility.HtmlDecode(url).Replace("%5c", "/");
            var filenamePrefix = "";

            if (imgUrl.StartsWith("/"))
            {
                imgUrl = "https://rules.ssw.com.au" + url;
            }
            else
            {
                filenamePrefix = "../../assets/";
            }

            var filename = ImageFilenameFromUrl(imgUrl);
            filename = filename.Replace(".bmp", ".png");
            return filenamePrefix + filename;
        }

        private void UpdateImageUrlsInRule(RulePage rule)
        {
            if (!rule.ImageUrls.Any()) return;
            foreach (var url in rule.ImageUrls)
            {
                var filename = GetImageFilename(url);
                rule.IntroText = WebUtility.HtmlDecode(rule.IntroText)?.Replace(url, filename);
                rule.Content = WebUtility.HtmlDecode(rule.Content)?.Replace(url, filename);
            }
        }

        private void UpdateImageUrlsInCategory(Category category, HashSet<string> imageUrls)
        {
            if (!imageUrls.Any()) return;
            foreach (var url in imageUrls)
            {
                var filename = GetImageFilename(url);
                category.IntroText = WebUtility.HtmlDecode(category.IntroText)?.Replace(url, filename);
                category.Content = WebUtility.HtmlDecode(category.Content)?.Replace(url, filename);
            }
        }

        public void GitCommit(string directory, string message, LibGit2Sharp.Signature author,
            LibGit2Sharp.Signature committer, string path = "*")
        {
            using (var repo = new Repository(directory))
            {
                try
                {
                    Commands.Stage(repo, path);
                    repo.Commit(message, author, committer, new CommitOptions()
                    {
                        //AllowEmptyCommit = true
                    });

                }
                catch (EmptyCommitException)
                {
                    _log.LogWarning("ignored empty commit exception");
                }
                catch (Exception ex)
                {
                    _log.LogWarning("Git Error:", ex);
                    throw;
                }
            }
        }

    }


    /// <summary>
    /// build paths to rule files
    /// </summary>
    public class RulePaths
    {
        private MdWriterConfig _config;
        private RulePage _rule;
        public RulePaths(MdWriterConfig config, RulePage rule)
        {
            _config = config;
            _rule = rule;
        }

        public string RuleFolderRelative => Path.Combine(_config.RulesFolder, _rule.GetRuleUri());
        public string RuleFileRelative => Path.Combine(RuleFolderRelative, "rule.md");
        public string RuleFolderFull => Path.Combine(_config.TargetRepository, RuleFolderRelative);
        public string RuleFileFull => Path.Combine(_config.TargetRepository, RuleFileRelative);
        public string RuleAssetsFolderFull => _config.AssetsFolderFull;

    }


    /// <summary>
    /// build paths to category files
    /// </summary>
    public class CategoryPaths
    {
        private MdWriterConfig _config;
        private Category _cat;
        public CategoryPaths(MdWriterConfig config, Category cat)
        {
            _config = config;
            _cat = cat;
        }

        public string ParentCategoryFolderRelative =>
            Path.Combine(_config.CategoriesFolder, _cat.ParentCategoryTitle.ToFileName());

        public string ParentCategoryIndexRelative => Path.Combine(ParentCategoryFolderRelative, "index.md");

        public string ParentCategoryIndexFull => Path.Combine(_config.TargetRepository, ParentCategoryFolderRelative);


        public string CategoryFileRelative => Path.Combine(ParentCategoryFolderRelative, _cat.Name.ToFileName() + ".md");
        public string CategoryFileFull => Path.Combine(_config.TargetRepository, CategoryFileRelative);
        public string CategoryFolderFull => Path.Combine(_config.TargetRepository, ParentCategoryFolderRelative);

    }


    public static class RuleExtensions
    {
        public static string GetRuleUri(this RulePage rule)
        {
            //Check for any friendly urls
            if (rule.Redirects.Count > 0)
            {
                return rule.Redirects[0].ToFileName();
            }
            else
            {
                return rule.Name.ToFileName();
            }
        }
        /// <summary>
        /// clean up a string to use as a file name. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// As Per rule https://rules.ssw.com.au/avoid-using-specific-characters-in-friendly-url
        public static string ToFileName(this string name)
        {
            string permittedCharacters = "abcdefghijklmnopqrstuvwxyz1234567890-_";
            var newName= new string(
                name.Replace(' ', '-').Replace('/', '-').Replace('.', '-').Replace('=', '-')
                    .ToLower()
                    .ToCharArray()
                    .Where(c => permittedCharacters.Contains(c))
                    .ToArray());
            newName = Regex.Replace(newName, "-{2,}", "-").Trim('-');        
            return newName;
        }

        //This is for preserving the SharePoint Beta Link Redirects
        public static string CreateUriAndRedirect(this string name, RulePage rule)
        {
            var gatsbyUri = rule.GetRuleUri();

            //Add SharePoint Beta-Link redirect
            var spBetaLink = rule.Name.ToFileName();
            if (spBetaLink != gatsbyUri)
            {
                rule.Redirects.Add(spBetaLink);
            }

            //Add SharePoint Link Redirect
            var spNewName = name.ToSharePointUri();
            if(spNewName != gatsbyUri)
            {
                rule.Redirects.Add(spNewName);
            }
            
            return gatsbyUri;
        }

        public static string ToSharePointUri(this string name)
        {
            /*
            - Spaces and the following characters /.= are converted to -
            - The following characters are removed &?':",%#~*
            - Everything is lowercase
            - Multiple instances of - are converted to a single -
            */
            string permittedCharacters = "abcdefghijklmnopqrstuvwxyz1234567890-–_()’＂@+";
            var spNewName = new string(
                name.Replace(' ', '-').Replace('/', '-').Replace('.', '-').Replace('=', '-')
                    .ToLower()
                    .ToCharArray()
                    .Where(c => permittedCharacters.Contains(c))
                    .ToArray());
            spNewName = Regex.Replace(spNewName, "-{2,}", "-").Trim('-');
            return spNewName;
        }

        public static string ToMarkdown(this ContentVersion contentVersion, RulePage rulePage)
        {
            var result = $@"{rulePage.ToFrontMatter()}
{ToMarkdownBody(contentVersion.IntroText, contentVersion.Content, true)}
";
            return result;
        }

        public static string ToMarkdown(this RulePage rulePage, bool skipHtmlMarkdownConversion)
        {
            var result = $@"{rulePage.ToFrontMatter()}
{ToMarkdownBody(rulePage.IntroText, rulePage.Content, skipHtmlMarkdownConversion)}
";
            return result;
        }


        public static string ToFrontMatter(this RulePage rulePage)
        {
            return $@"---
{MdWriter.YamlSerializer.Serialize(new RuleMdModel(rulePage))}
---
";
        }



        public static string ToMarkdownBody(string introText, string content, bool skipHtmlMarkdownConversion)
        {


            string html = $@"
{introText}
<br><excerpt class='endintro'></excerpt><br>
{content}

";
            if (skipHtmlMarkdownConversion)
            {
                //We don't want to convert all the history from HTML to Markdown so we skip it here
                return html;
            }

            string result = MarkdownConverter.Convert(html);
            return result;
        }
    }


}
