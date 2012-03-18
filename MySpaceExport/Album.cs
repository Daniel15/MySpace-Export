using System;
using System.Collections.Generic;

namespace Daniel15.MySpaceExport
{
	/// <summary>
	/// Represents an album on MySpace
	/// </summary>
	class Album
	{
		public int Id { get; set; }
		public string Title { get; set; }
		public Uri Url { get; set; }
		public string Path { get; set; }
		public DateTime Date { get; set; }
		public IList<Photo> Photos { get; set; } 
	}
}
