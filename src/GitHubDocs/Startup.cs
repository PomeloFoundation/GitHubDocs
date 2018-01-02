using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GitHubDocs
{
    public class Startup
    {
        public static Dictionary<string, string> Config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "config.json")));

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddAntiXss();
            services.AddTransient<Lib.RazorViewToStringRenderer>();
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.Run(async (context) =>
            {
                var branches = await Lib.GitHub.GetBranchesAsync();
                var endpoint = context.Request.Path.ToString();
                var splited = endpoint.Split('/');
                var branch = branches.First();
                if (branches.Contains(splited[1]))
                {
                    branch = splited[1];
                    endpoint = endpoint.Replace("/" + branch, "");
                    if (string.IsNullOrEmpty(endpoint))
                        endpoint = "/";
                }
                if (endpoint.EndsWith("/"))
                    endpoint += "index.md";
                var toc = await Lib.GitHub.RenderTocMdAsync(branch);
                var content = Lib.GitHub.ReplaceImages(Lib.GitHub.FilterMarkdown(await Lib.GitHub.GetRawFileAsync(branch, endpoint)), branch);
                var contribution = await Lib.GitHub.GetContributionAsync(branch, endpoint);
                var render = context.RequestServices.GetRequiredService<Lib.RazorViewToStringRenderer>();
                await context.Response.WriteAsync(await render.RenderViewToStringAsync("Index", new Models.Page
                {
                    CurrentBranch = branch == branches.First() ? "" : branch,
                    Branches = branches,
                    Toc = toc,
                    Content = content,
                    Contribution = contribution,
                    Endpoint = endpoint
                }));
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
