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

        public static async Task<IList<string>> GetBranchesAsync()
        {
            var url = $"{Startup.Config["Organization"]}/{Startup.Config["Repository"]}/branches/all";
            using (var client = new HttpClient { BaseAddress = GitHubUri })
            {
                var responseMessage = await client.GetAsync(url);
                var html = await responseMessage.Content.ReadAsStringAsync();
                var ret = new List<string>();
                foreach (Match x in BranchesRegex.Matches(html))
                {
                    ret.Add(x.Value);
                }
                return ret;
            }
        }

        public static async Task<string> GetRawFileAsync(string Branch, string Endpoint)
        {
            var url = $"https://raw.githubusercontent.com/{Startup.Config["Organization"]}/{Startup.Config["Repository"]}/{Branch}/{Startup.Config["RootPath"]}/{Endpoint}";
            using (var client = new HttpClient { BaseAddress = GitHubUri })
            {
                var responseMessage = await client.GetAsync(url);
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        private static Regex ContributorAnchorRegex = new Regex("<a class=\"avatar-link tooltipped tooltipped-s\" aria-label=\".*\" width=\"20\" /> </a>");
        private static Regex ContributorNameRegex = new Regex(@"(?<=<a class=""avatar-link tooltipped tooltipped-s"" aria-label="")[a-zA-Z0-9_-]{0,}(?="" href="")");
        private static Regex ContributorAvatarRegex = new Regex(@"(?<= class=""avatar"" height=""20"" src="").*(?="" width=""20"" /> </a>)");
        private static Regex ContributorLastUpdateRegex = new Regex(@"(?<=<relative-time datetime="")[0-9TZ:-]{0,}(?="">)");
        public static async Task<Models.Contribution> GetContributionAsync(string Branch, string Endpoint)
        {
            var url = $"https://github.com/{Startup.Config["Organization"]}/{Startup.Config["Repository"]}/contributors/{Branch}/{Startup.Config["RootPath"]}/{Endpoint}";
            using (var client = new HttpClient { BaseAddress = GitHubUri })
            {
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
                return ret;
            }
        }

        public static async Task<string> GetTocMdAsync(string Branch)
        {
            var toc = (await GetRawFileAsync(Branch, "toc.md")).Split('\n');
            return TocToUl(toc);
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
