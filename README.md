MySpace Export
==============

This small app assists with exporting your data from MySpace. Currently, it only supports photos.
Additionally, an app is included to import the exported photos to Facebook. MySpace's API is 
extremely unreliable and their support is non-responsive so this just scrapes the HTML of their 
site. It's ugly but it works.

It has been used with Mono 2.10 on Ubuntu Linux but should also work with the Microsoft .NET 
Framework on Windows.

How To Use
----------

### MySpace Export

1. Edit MySpaceExport/Program.cs and enter your MySpace username and directory to save files in
2. Run it

### Facebook Import

1. Create a Facebook application
2. Create an access token (TODO: Document this more)
3. Edit FacebookPhotoImport/Main.cs
4. Run it
