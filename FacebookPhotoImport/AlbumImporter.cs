using System;
using System.IO;
using System.Net;
using Facebook;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

namespace Daniel15.FacebookPhotoImport
{
	public class AlbumImporter
	{
		public string AccessToken { get; set; }
		public string InputDirectory { get; set; }
		
		private FacebookClient _fbClient;
		
		public AlbumImporter ()
		{
			//HttpWebRequest.DefaultWebProxy = new WebProxy("localhost", 8888);
			// Ignore SSL issues
			//ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
		}
		
		public void Import()
		{			
			_fbClient = new FacebookClient(AccessToken);
			
			var files = Directory.GetFiles(InputDirectory, "index.xml", SearchOption.AllDirectories);
			foreach (var file in files)
			{
				ImportAlbum(file);
			}
		}
		
		private void ImportAlbum(string path)
		{
			XElement xml = XElement.Load(path);
			var album = new Album
			{
				Title = xml.Attribute("title").Value,
				Date = DateTime.Parse(xml.Attribute("date").Value),
				Path = path.Replace("index.xml", string.Empty),
			};
			
			// Create this album on Facebook
			dynamic fbAlbum = _fbClient.Post("me/albums", new
			{
				name = "MySpace - " + album.Title,
				description = "Imported from MySpace - Last Updated " + album.Date.ToShortDateString(),
				created_time = album.Date.ToString("o"),
				updated_time = album.Date.ToString("o")
			});
			album.Id = Convert.ToInt64(fbAlbum.id);
			
			Console.WriteLine("{0} - {1}", album.Title, album.Id);
			
			foreach (var photo in xml.Elements("Photo"))
			{
				ImportPhoto(photo, album);
			}
		}
		
		private void ImportPhoto(XElement photoNode, Album album)
		{
			var filename = photoNode.Attribute("filename").Value;
			var caption = photoNode.Value;
			
			Console.Write(" - {0}... ", caption);			
			// Anonymous object doesn't work in Mono, requires an IDictionary<string, object> instead.
			// https://github.com/facebook-csharp-sdk/facebook-csharp-sdk/issues/131
			_fbClient.Post(string.Format("{0}/photos", album.Id), new Dictionary<string, object>
			{
				{"message", "Imported from MySpace: " + caption},
				{"file", new FacebookMediaObject
				{
					ContentType = "image/jpeg",
					FileName = filename,
				}.SetValue(File.ReadAllBytes(Path.Combine(album.Path, filename)))}
			});
			
			Console.WriteLine("Done!");
		}
	}
}

