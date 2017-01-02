﻿namespace Imagician
{
	public class FolderItem
	{
		public int ID { get; internal set; }
		public bool IsImage { get; internal set; }
		public bool IsFolder { get; internal set; }
		public string Title { get; internal set; }
		public string Path { get; internal set; }
		public string ImagePath { get; internal set; }
	}

	public class FolderItemExif : FolderItem
	{
		public ExifLib.JpegInfo Info { get; internal set; }

	}
}