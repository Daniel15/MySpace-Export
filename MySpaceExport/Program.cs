namespace Daniel15.MySpaceExport
{
	class Program
	{
		static void Main(string[] args)
		{
			var exporter = new AlbumExporter
			{
				Username = "daniel_1515",
				//OutputDirectory = @"c:\temp\MySpace_Export\"
				OutputDirectory = @"/home/daniel/Pictures/MySpace/"
			};

			exporter.Export();
		}
	}
}
