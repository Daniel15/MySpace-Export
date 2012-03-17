using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Linq;
using System.Collections.Specialized;
using System.Text;

namespace Daniel15.MySpaceExport
{
	/// <summary>
	/// Exports photo albums from MySpace
	/// </summary>
	public class AlbumExporter
	{
		private const string PHOTOS_URL = "http://www.myspace.com/{0}/photos/page/{1}";
		private const string BASE_URL = "http://www.myspace.com";
		private const string ALBUM_PAGE_URL = "http://www.myspace.com/Modules/PageEditor/Handlers/Profiles/Module.ashx";

		public string Username { get; set; }
		public string OutputDirectory { get; set; }

		public void Export()
		{			
			var albums = GetAlbumListing();

			foreach (var album in albums)
			{
				Console.WriteLine(album.Title);
				ExportAlbum(album);
			}
		}

		private IEnumerable<Album> GetAlbumListing()
		{
			Console.WriteLine("Getting album pages... ");
			IList<Album> albums = new List<Album>();
			var client = new WebClient();
			int page = 1;

			while (true)
			{
				// Get this album listing page
				var html = new HtmlDocument();
				html.LoadHtml(client.DownloadString(string.Format(PHOTOS_URL, Username, page)));

				// Try to find the album list
				var albumList = html.DocumentNode
					.Descendants("ol")
					.First(element =>
						element.Attributes["class"] != null
						&& element.Attributes["class"].Value == "grid group albumList");


				var albumTitles = albumList.Descendants("h4");

				// If there's no photos, assume we're at the end
				if (!albumTitles.Any())
					break;

				foreach (var titleEl in albumTitles)
				{
					// Get the date from the album's caption
					var caption = titleEl.ParentNode.ParentNode.Element("p").InnerHtml;
					var captionPieces = caption.Split(',');
					var date = DateTime.Parse(captionPieces[1]);
					
					// Get all the other album data
					var title = titleEl.InnerHtml;
					var url = new Uri(BASE_URL + titleEl.ParentNode.Attributes["href"].Value);
					var id = Convert.ToInt32(url.Segments.Last());
					
					albums.Add(new Album { Id = id, Title = title, Url = url, Date = date });
				}

				page++;
			}

			return albums;
		}

		private string CleanPathName(string path)
		{
			string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
			Regex pathNameRegex = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			return pathNameRegex.Replace(path, string.Empty);
		}

		private void ExportAlbum(Album album)
		{
			var path = Path.Combine(OutputDirectory, CleanPathName(album.Title));
			Directory.CreateDirectory(path);
			album.Path = path;
			var page = 1;
			
			var xml = new XDocument();
			var rootNode = new XElement("Album", 
				new XAttribute("id", album.Id),
				new XAttribute("date", album.Date),
				new XAttribute("url", album.Url),
				new XAttribute("title", album.Title)
			);
			xml.Add(rootNode);
			


			// Loops until no more pages are left
			while (true)
			{
				var albumPage = GetAlbumPage(album, page);
	
				// Try to find the list of photos on this page
				var photoList = albumPage.GetElementbyId("photoList");
				// Process photos in background threads
				var photos = new ConcurrentQueue<Photo>();
				Parallel.ForEach(photoList.Descendants("a"), node => ExportAlbumPhoto(node, photos, path));
				album.Photos = photos.ToList();
				
				// Stop looping if there's no photos left.
				if (photos.Count == 0)
					break;
				
				foreach (var photo in photos)
				{
					rootNode.Add(new XElement("Photo", 
						new XAttribute("url", photo.Url),
						new XAttribute("filename", photo.Filename),
						photo.Caption
					));
				}
				
				page++;
			}

			xml.Save(Path.Combine(path, "index.xml"));
		}
		
		private HtmlDocument GetAlbumPage(Album album, int page)
		{
			var client = new WebClient();
			//var albumPage = new HtmlDocument();
			//albumPage.LoadHtml(client.DownloadString(album.Url));
			
			// TODO: No clue what this is - This seems to work though (taken from logging requests on MySpace)
			client.Headers.Add("Hash", "MIGuBgkrBgEEAYI3WAOggaAwgZ0GCisGAQQBgjdYAwGggY4wgYsCAwIAAQICZgMCAgDABAjxk%2fqvtBqGGwQQUc%2fM6FamnbvPgW07TDo%2bGARgibOOx9JuS4eVLj6D6xHR2UJE5uheOK%2brQBfJQOxXreYejDJC%2fa5G3nBJRCpCRZA%2byyQfYNSwGwxipWNrEkIGCrcdDsiKTDeuE6sXGVvrM2iP5XtjGDGTjXhry6Q8Lc6%2f");
			
			NameValueCollection formValues = new NameValueCollection();
			formValues.Add("PageNo", page.ToString());
			formValues.Add("typeid", "11003");
			formValues.Add("albumId", album.Id.ToString());
			
			var response = client.UploadValues(ALBUM_PAGE_URL, "POST", formValues);
			var doc = new HtmlDocument();
			doc.LoadHtml(Encoding.UTF8.GetString(response));
			return doc;
		}

		private void ExportAlbumPhoto(HtmlNode node, ConcurrentQueue<Photo> photoList, string outputDir)
		{
			var fullUrl = BASE_URL + node.Attributes["href"].Value;
			//var smallImageUrl = node.Element("img").Attributes["src"].Value;

			Console.Write(".");

			var imageClient = new WebClient();
			var imagePage = new HtmlDocument();
			imagePage.LoadHtml(imageClient.DownloadString(fullUrl));

			string caption;
			string imageUrl;

			try
			{
				var captionEl = imagePage.GetElementbyId("photoCaption");
				caption = captionEl == null ? string.Empty : captionEl.InnerHtml;
				imageUrl = imagePage.GetElementbyId("singlePhotoImage").Attributes["src"].Value;
			}
			catch (Exception)
			{
				Console.WriteLine("* Broken image? {0}", fullUrl);
				return;
			}

			Console.WriteLine(" - {0}", caption);
			var filename = Path.GetFileName(imageUrl.Replace("/l", ""));
			imageClient.DownloadFile(imageUrl, Path.Combine(outputDir, filename));

			photoList.Enqueue(new Photo { Caption = caption, Filename = filename, Url = fullUrl });
		}
	}
}
