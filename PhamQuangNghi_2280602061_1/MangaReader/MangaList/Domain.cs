using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.IO;
using System.Web;
using Avalonia.Controls.Chrome;
using MangaReader.DomainCommon;

namespace MangaReader.MangaList;
public class Manga{
public Manga(string title, string description, string coverUrl, string lastChapter, string mangaUrl)
{
    Title = title;
    Description = description;
    CoverUrl = coverUrl;
    LastChapter = lastChapter;
    MangaUrl = mangaUrl;
}

public string Title { get; init; }
public string Description { get; init; }
public string CoverUrl { get; init; }
public string LastChapter { get; init; }
public string MangaUrl { get; init; }

}

public class MangaList
{
    public int TotalMangaNumber { get; init; }
    public int TotalPageNumber { get; init; }
    public List<Manga> CurrentPage { get; init; }
    
    public MangaList(int totalMangaNumber, int totalPageNumber, List<Manga> currentPage)
    {
        TotalMangaNumber = totalMangaNumber;
        TotalPageNumber = totalPageNumber;
        CurrentPage = currentPage;
    }

   
}

public class Domain
{
    private readonly string baseUrl;
    private readonly Http http;

    public Domain(string baseUrl, Http http)
    {
        this.baseUrl = baseUrl;
        this.http = http;
    }

    private async Task<string> DownloadHtml(int page, string filterText)
    {
        if (page < 1) page = 1;
        string url;
        if (filterText == "")
        {
            url = $"{this.baseUrl}/filter?status=0&sort=updatedAt&page={page}";
        }
        else
        {
            var text = HttpUtility.HtmlEncode(filterText);
            url = $"{this.baseUrl}/tim-kiem?keyword={text}&page={page}";
        }
        Console.WriteLine($"Downloading page {page} form {url}");
        return await http.GetStringAsync(url);
    }

    // private int ParseTotalMangaNumber(XmlDocument doc)
    // {
    //     var text = doc.DocumentElement!.FirstChild!.FirstChild!.InnerText.Trim();
    //     var number = text.Substring(7);
    //     return int.Parse(number);
    // }
    // private int ParseTotalMangaNumber(HtmlNode section)
    // {
    //     var text = section.QuerySelector("").InnerText.Trim();
    //     var number = text.Substring(7);
    //     return int.Parse(number);
    // }


    private int ParseTotalPageNumber(HtmlNode section)
    {
        var a = section.QuerySelector("nav.paging-new li:last-child>a");
        if (a == null) return 1;
        var href = a.Attributes["href"].Value;
        var equalIndex = href.LastIndexOf('=');
        var page = href[(equalIndex + 1)..];
        return int.Parse(page);
    }

    // var div = doc.DocumentElement!.ChildNodes[3];
        // var span = div.LastChild;
        // if (span.Attributes!["class"].Value == "current_page")
        //     return int.Parse(span.InnerText);
        // var href = span.FirstChild!.Attributes!["href"].Value;
        // var openingParenthesisIndex = href.LastIndexOf('(');
        // var number = href.Substring(openingParenthesisIndex + 1, href.Length - openingParenthesisIndex - 2);
        // return int.Parse(number);
    
    private int FindTotalPageNumber(string html)
    {
        var s = html.Substring(html.IndexOf("totalPages") + 13);
        s = s.Substring(0, s.IndexOf(","));
        return int.Parse(s);
    }

    private int FindTotalMangaNumber(string html)
    {
        var s = html.Substring(html.IndexOf("totalDocs")+12);
        s = s.Substring(0, s.IndexOf("}"));
        return int.Parse(s);
    }
    private List<Manga> ParseMangaList(HtmlNode section)
    {
        List<Manga> mangaList = new List<Manga>();
        var bookWrapperNodes = section.QuerySelector("div.grid").QuerySelectorAll("div.book-wrapper").ToArray();
        for (int i = 0; i < bookWrapperNodes.Length; i++)
        {
            var bookNode = bookWrapperNodes[i];
            var a_relative = bookNode.QuerySelector("div > a");
            var div_content = bookNode.QuerySelector("div > div");
            
            var title = Html.Decode(div_content.QuerySelector("a").InnerText.Trim());
            var description = Html.Decode(div_content.QuerySelector("div").InnerText.Trim());
            var div_lastChapter = div_content.QuerySelector("div.flex.flex-col.flex-1.pr-4.justify-end.w-full > a");
            var lastChapter = Html.Decode(div_lastChapter?.InnerText.Trim());
            
            var coverUrl = baseUrl + Html.Decode(a_relative.QuerySelector("img").Attributes!["src"]!.Value);
            var mangaUrl = baseUrl + Html.Decode(a_relative.Attributes!["href"]!.Value);

            var manga = new Manga(title, description, coverUrl,lastChapter, mangaUrl);
            mangaList.Add(manga);
        }
        return mangaList;
    }

    private MangaList Parse(string html)
    {
        try
        {
            var totalPageNumber= FindTotalPageNumber(html);
            var totalMangaNumber = FindTotalMangaNumber(html);
            
            File.WriteAllText("docbefore.html",html);
            var xmlStartAt = html.IndexOf("<div class=\"grid grid-cols-1");
            html = html.Substring(xmlStartAt);
            html = html.Substring(0, html.IndexOf("<div class=\"mt-6\">"));
            var doc = new HtmlDocument();
            doc.LoadHtml("<html> </head> <body>" + html + "<body/></html>");
            var section = doc.DocumentNode;
            
            if(section == null)
                return new MangaList(0,0, new List<Manga>());
            return new MangaList(
                totalPageNumber: totalMangaNumber,
                totalMangaNumber: totalMangaNumber,
                currentPage: this.ParseMangaList(section)
            );
            // {
            //     // TotalMangaNumber = totalMangaNumber,
            //     // TotalPageNumber = totalPageNumber,
            //     // CurrentPage = page
            // };
        }
        catch (Exception e)
        {
            throw new ParseException();
        }
    }

    public async Task<MangaList> LoadMangaList(int page, string filterText="")
    {
        var html = await this.DownloadHtml(page, filterText);
        return this.Parse(html);
    }

    public Task<byte[]> LoadBytes(string url, CancellationToken token)
    {
        return http.GetByteAsync(url, token);
    }
}