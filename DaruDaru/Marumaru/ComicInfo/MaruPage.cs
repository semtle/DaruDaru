using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DaruDaru.Config;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;
using HtmlAgilityPack;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class MaruPage : Comic
    {
        public MaruPage(IMainWindow mainWindow, bool addNewOnly, Uri uri, string comicName)
            : base(mainWindow, addNewOnly, uri, comicName)
        {
        }

        struct WasabisyrupLinks
        {
            public Uri Uri;
            public string TitleNo;
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            var lstArchives = new List<WasabisyrupLinks>();
            Uri newUri = null;

            using (var wc = new WebClientEx())
            {
                var doc = new HtmlDocument();

                var success = Retry(() =>
                {
                    doc.LoadHtml(wc.DownloadString(this.Uri));

                    newUri = wc.ResponseUri ?? this.Uri;

                    this.Title = ReplcaeHtmlTag(doc.DocumentNode.SelectSingleNode("//div[@class='subject']").InnerText.Replace("\n", "")).Trim();

                    string titleNo;
                    foreach (var a in doc.DocumentNode.SelectSingleNode("//div[@class='content']").SelectNodes(".//a[@href]"))
                    {
                        var a_uri = new Uri(newUri, a.Attributes["href"].Value);

                        if (DaruUriParser.Archive.CheckUri(a_uri))
                        {
                            titleNo = a.InnerText;

                            if (!string.IsNullOrWhiteSpace(titleNo))
                                lstArchives.Add(new WasabisyrupLinks
                                {
                                    Uri     = a_uri,
                                    TitleNo = ReplcaeHtmlTag(a.InnerText)
                                });
                        }
                    }

                    return true;
                });

                if (!success || lstArchives.Count == 0)
                {
                    this.State = MaruComicState.Error_1_Error;
                    return false;
                }
            }

            this.Uri = newUri;

            try
            {
                ArchiveManager.UpdateMarumaru(DaruUriParser.Marumaru.GetCode(this.Uri), this.Title);

                IEnumerable<WasabisyrupLinks> items = lstArchives;

                if (this.AddNewonly)
                    items = ArchiveManager.IsNewArchive(items, e => DaruUriParser.Archive.GetCode(e.Uri));

                var comics = items.Select(e => new WasabiPage(this.IMainWindow, this.AddNewonly, e.Uri, this.Title, e.TitleNo)).ToArray();
                
                var noNew = this.AddNewonly && comics.Length == 0;

                this.IMainWindow.InsertNewComic(this, comics, !noNew);

                if (noNew)
                {
                    this.State = MaruComicState.Complete_3_NoNew;
                    this.IMainWindow.UpdateTaskbarProgress();
                }

                count = lstArchives.Count;

                // Create Shortcut
                if (this.ConfigCur.CreateUrlLink)
                {
                    Directory.CreateDirectory(this.ConfigCur.UrlLinkPath);

                    var path = Path.Combine(this.ConfigCur.UrlLinkPath, $"{ReplaceInvalid(this.Title)}.url");
                    if (!File.Exists(path))
                        File.WriteAllText(path, $"[InternetShortcut]\r\nURL=" + this.Uri.AbsoluteUri);
                }

                return count > 0;
            }
            catch (Exception ex)
            {
                this.State = MaruComicState.Error_1_Error;

                CrashReport.Error(ex);
            }

            return false;
        }
    }
}
