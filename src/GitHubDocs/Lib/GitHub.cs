using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;

namespace GitHubDocs.Lib
{
    public static class GitHub
    {
        private static readonly Uri GitHubUri = new Uri("https://github.com/");
        private static Regex BranchesRegex = new Regex(@"(?<=<div class=""branch-summary js-branch-row"" data-branch-name="").*(?="">)");
        private static List<string> CachedBranches;
        private static DateTime CachedBranchesExpireTime;
        private static string CachedRenderedToc;
        private static DateTime CachedRenderedTocExpireTime;
        private static Dictionary<string, KeyValuePair<DateTime, string>> CachedHttpContent = new Dictionary<string, KeyValuePair<DateTime, string>>();
        private static Dictionary<string, KeyValuePair<DateTime, Models.Contribution>> CachedContributor = new Dictionary<string, KeyValuePair<DateTime, Models.Contribution>>();

        public static async Task<IList<string>> GetBranchesAsync()
        {
            if (DateTime.Now > CachedBranchesExpireTime)
            {
                var url = $"{Startup.Config["Organization"]}/{Startup.Config["Repository"]}/branches/all";
                using (var client = new HttpClient { BaseAddress = GitHubUri })
                {
                    client.Timeout = new TimeSpan(0, 1, 0);
                    var responseMessage = await client.GetAsync(url);
                    var html = await responseMessage.Content.ReadAsStringAsync();
                    var ret = new List<string>();
                    foreach (Match x in BranchesRegex.Matches(html))
                    {
                        ret.Add(x.Value);
                    }
                    CachedBranches = ret;
                    CachedBranchesExpireTime = DateTime.Now.AddMinutes(Convert.ToInt32(Startup.Config["Caching"]));
                }
            }
            return CachedBranches;
        }

        public static async Task<string> GetRawFileAsync(string Branch, string Endpoint)
        {
            var url = $"https://raw.githubusercontent.com/{Startup.Config["Organization"]}/{Startup.Config["Repository"]}/{Branch}/{Startup.Config["RootPath"]}/{Endpoint}";
            if (url.EndsWith("toc.md") || !CachedHttpContent.ContainsKey(url) || DateTime.Now > CachedHttpContent[url].Key)
            {
                using (var client = new HttpClient { BaseAddress = GitHubUri })
                {
                    client.Timeout = new TimeSpan(0, 1, 0);
                    var responseMessage = await client.GetAsync(url);
                    var ret = await responseMessage.Content.ReadAsStringAsync();
                    if (!url.EndsWith("toc.md"))
                    {
                        if (CachedHttpContent.ContainsKey(url))
                            CachedHttpContent[url] = new KeyValuePair<DateTime, string>(DateTime.Now.AddMinutes(Convert.ToInt32(Startup.Config["Caching"])), ret);
                        else
                            CachedHttpContent.Add(url, new KeyValuePair<DateTime, string>(DateTime.Now.AddMinutes(Convert.ToInt32(Startup.Config["Caching"])), ret));
                    }
                    else
                    {
                        return ret;
                    }
                }
            }
            return CachedHttpContent[url].Value;
        }

        private static Regex ContributorAnchorRegex = new Regex("<a class=\"avatar-link tooltipped tooltipped-s\" aria-label=\".*\" width=\"20\" /> </a>");
        private static Regex ContributorNameRegex = new Regex(@"(?<=<a class=""avatar-link tooltipped tooltipped-s"" aria-label="")[a-zA-Z0-9_-]{0,}(?="" href="")");
        private static Regex ContributorAvatarRegex = new Regex(@"(?<= class=""avatar"" height=""20"" src="").*(?="" width=""20"" /> </a>)");
        private static Regex ContributorLastUpdateRegex = new Regex(@"(?<=<relative-time datetime="")[0-9TZ:-]{0,}(?="">)");
        private static Regex SingleContributorNameRegex = new Regex(@"(?<=<img alt=""@)[a-zA-Z0-9_-]{0,}(?="" class=""avatar"" height=""20"" src=)");
        private static Regex SingleContributorAvatarRegex = new Regex(@"(?<="" class=""avatar"" height=""20"" src="").*(?="" width=""20"" />)");
        public static async Task<Models.Contribution> GetContributionAsync(string Branch, string Endpoint)
        {
            var url = $"https://github.com/{Startup.Config["Organization"]}/{Startup.Config["Repository"]}/contributors/{Branch}/{Startup.Config["RootPath"]}/{Endpoint}";
            if (!CachedContributor.ContainsKey(url) || DateTime.Now > CachedContributor[url].Key)
            {
                using (var client = new HttpClient { BaseAddress = GitHubUri })
                {
                    client.Timeout = new TimeSpan(0, 1, 0);
                    var responseMessage = await client.GetAsync(url);
                    var html = await responseMessage.Content.ReadAsStringAsync();
                    var ret = new Models.Contribution();
                    foreach (Match x in ContributorAnchorRegex.Matches(html))
                    {
                        try
                        {
                            var key = ContributorNameRegex.Match(x.Value).Value;
                            var value = ContributorAvatarRegex.Match(x.Value).Value;
                            ret.Contributors.Add(key, value);
                        }
                        catch
                        {
                        }
                    }
                    try
                    {
                        ret.LastUpdate = Convert.ToDateTime(ContributorLastUpdateRegex.Match(html).Value);
                    }
                    catch
                    {
                    }
                    if (ret.Contributors.Count == 0)
                    {
                        try
                        {
                            var match = SingleContributorNameRegex.Match(html);
                            if (match.Success)
                            {
                                ret.Contributors.Add(match.Value, SingleContributorAvatarRegex.Match(html).Value);
                            }
                        }
                        catch { }
                    }
                    if (CachedContributor.ContainsKey(url))
                    {
                        CachedContributor[url] = new KeyValuePair<DateTime, Models.Contribution>(DateTime.Now.AddMinutes(Convert.ToInt32(Startup.Config["Caching"])), ret);
                    }
                    else
                    {
                        CachedContributor.Add(url, new KeyValuePair<DateTime, Models.Contribution>(DateTime.Now.AddMinutes(Convert.ToInt32(Startup.Config["Caching"])), ret));
                    }
                }
            }
            return CachedContributor[url].Value;
        }

        public static async Task<string> RenderTocMdAsync(string Branch)
        {
            if (DateTime.Now > CachedRenderedTocExpireTime)
            {
                var toc = await GetTocMdAsync(Branch);
                CachedRenderedToc = TocToUl(toc.Split('\n').Select(x => x.TrimEnd('\r')).ToArray());
                CachedRenderedTocExpireTime = DateTime.Now.AddMinutes(Convert.ToInt32(Startup.Config["Caching"]));
            }
            return CachedRenderedToc;
        }

        private static async Task<string> GetTocMdAsync(string Branch, string Endpoint = "toc.md", int Level = 0)
        {
            var toc = (await GetRawFileAsync(Branch, Endpoint)).Split('\n').Select(x => x.TrimEnd('\r')).ToList();
            var tasks = new List<Task>();
            Parallel.For(0, toc.Count, i => {
                tasks.Add(Task.Run(async () => 
                {
                    var href = AHrefRegex.Match(toc[i]).Value;
                    var title = AInnerTextRegex.Match(toc[i]).Value;

                    if (toc[i].StartsWith("#"))
                    {
                        toc[i] = BuildSharps(Level) + toc[i];
                        if (!string.IsNullOrWhiteSpace(href))
                            toc[i] = toc[i].Replace(href, Endpoint.Substring(0, Endpoint.Length - "toc.md".Length) + href);
                    }

                    if (toc[i].EndsWith("toc.md)"))
                    {
                        var cnt = CountLeft(toc[i], '#');
                        if (!string.IsNullOrWhiteSpace(href))
                        {
                            toc[i] = BuildSharps(cnt) + " " + title + "\n" + await GetTocMdAsync(Branch, Endpoint.Substring(0, Endpoint.Length - "toc.md".Length) + href, cnt);
                        }
                    }
                }));
            });
            Task.WaitAll(tasks.ToArray());
            return string.Join("\n", toc);
        }

        private static Regex AHrefRegex = new Regex(@"(?<=\().*(?=\))");
        private static Regex AInnerTextRegex = new Regex(@"(?<=\[).*(?=\])");

        private static string TocToUl(string[] toc, int level = 1, int begin = 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ul>");
            var cnt = 0;
            for (var i = begin; i < toc.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(toc[i]) || toc[i].StartsWith("<!--") || CountLeft(toc[i], '#') > level)
                    continue;
                if (CountLeft(toc[i], '#') < level)
                    break;
                cnt++;
                sb.AppendLine("<li>");
                var text = AInnerTextRegex.Match(toc[i]);
                var href = AHrefRegex.Match(toc[i].Replace($"[{ text }]", ""));
                if (href.Success && text.Success)
                {
                    sb.AppendLine($"<a href=\"{ href.Value }\">");
                    sb.AppendLine(text.Value);
                    sb.AppendLine("</a>");
                }
                else
                {
                    sb.AppendLine($"<a href=\"javascript:;\" onclick=\"Expand(this)\">");
                    sb.AppendLine(toc[i].Trim().TrimStart('#').Trim());
                    sb.AppendLine("</a>");
                }
                sb.Append(TocToUl(toc, level + 1, i + 1));
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul>");
            if (cnt == 0)
                return "";
            else
                return sb.ToString();
        }

        private static string BuildSharps(int count)
        {
            var ret = new StringBuilder();
            for (var i = 0; i < count; i++)
                ret.Append("#");
            return ret.ToString();
        }

        private static int CountLeft(string src, char ch)
        {
            var ret = 0;
            for (var i = 0; i < src.Length; i++)
                if (src[i] == ch)
                    ret++;
                else
                    break;
            return ret;
        }

        private static Regex _imgRegex = new Regex(@"!\[[a-zA-Z0-9-_ ]{1,}\][ ]{0,}\(~[/a-zA-Z0-9._-]{1,}\)");

        public static string ReplaceImages(string md, string branch)
        {
            foreach (Match x in _imgRegex.Matches(md))
            {
                md = md.Replace(x.Value, x.Value.Replace("(~", $"(https://raw.githubusercontent.com/JoyOI/Docs/{branch}/{Startup.Config["RootPath"]}/"));
            }
            return md;
        }

        public static string FilterMarkdown(string md)
        {
            var tmp = md.Replace("\r", "").Split('\n').ToList();
            filter:
            var cnt = -1;
            var begin = -1;
            for (var i = 0; i < tmp.Count; i++)
            {
                if (IsDash(tmp[i].TrimEnd()))
                {
                    if (begin == -1)
                    {
                        begin = i;
                        cnt = CountDash(tmp[i]);
                    }
                    else if (CountDash(tmp[i]) == cnt && begin >= 0)
                    {
                        var gotoFlag = true;
                        for (var j = begin + 1; j < i; j++)
                        {
                            if (!string.IsNullOrWhiteSpace(tmp[j]) && tmp[j].Split(':').Length != 2)
                            {
                                gotoFlag = false;
                                break;
                            }
                        }
                        if (gotoFlag)
                        {
                            tmp.RemoveRange(begin, i - begin + 1);
                            goto filter;
                        }
                    }
                }
            }
            return string.Join("\r\n", tmp);
        }

        private static bool IsDash(string src)
        {
            for (var i = 0; i < src.Length; i++)
                if (src[i] != '-')
                    return false;
            return true;
        }

        private static int CountDash(string src)
        {
            var ret = 0;
            for (var i = 0; i < src.Length; i++)
                if (src[i] == '-')
                    ret++;
            return ret;
        }
    }
}
