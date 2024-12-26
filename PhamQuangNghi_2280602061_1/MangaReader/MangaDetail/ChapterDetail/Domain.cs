using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MangaReader.DomainCommon;
namespace MangaReader.MangaDetail.ChapterDetail;

public class Chapter
{
    public string Title { get; }
    public List<string> PageUrls { get; }

    public Chapter(string title, List<string> pageUrls)
    {
        this.Title = title;
        this.PageUrls = pageUrls;
    }
}

public class Domain
{
    private readonly string chapterUrl;
    private readonly Http http;

    public Domain(string chapterUrl, Http http)
    {
        this.chapterUrl = chapterUrl;
        this.http = http;
    }

    public async Task<Chapter> LoadChapter(CancellationToken token)
    {
        var html = await http.GetStringAsync(chapterUrl, token);

        string parseTitle(HtmlNode docNode)
        {
            var title = docNode.QuerySelector("head > title");
            return Html.Decode(title.InnerText);
        }

        List<string> parsePageUrls(HtmlNode docNode)
        {
            var contain_div = docNode.QuerySelector("body > main > div").ChildNodes[3].FirstChild;
            var imgNodes = contain_div.ChildNodes;
            var pageUrls = new List<string>();
            foreach (var node in imgNodes)
            {
                if (node?.Name == "div" && node?.FirstChild?.Name == "img")
                    pageUrls.Add(node.FirstChild.Attributes["src"].Value);
            }

            return pageUrls;
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var docNode = doc.DocumentNode;
            return new Chapter(parseTitle(docNode), parsePageUrls(docNode));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new ParseException();
        }
    }

    public Task<byte[]> LoadBytes(string url, CancellationToken token)
    {
        return http.GetByteAsync(url, token);
    }

}
