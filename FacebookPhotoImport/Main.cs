using System;
using Facebook;

namespace Daniel15.FacebookPhotoImport
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var importer = new AlbumImporter
			{
				InputDirectory = @"/home/daniel/Pictures/MySpace/",
				// Create Facebook app, then get token from https://developers.facebook.com/tools/explorer/
				AccessToken = "Put your access token here"
			};
			importer.Import();
		}
	}
}
