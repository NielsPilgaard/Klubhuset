namespace Skoleoverblikket.Api.Storage;

public static class FileExtensions
{
	public static readonly IReadOnlyDictionary<string, string> MimeTypes =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[".pdf"] = "application/pdf",
			[".doc"] = "application/msword",
			[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
			[".xls"] = "application/vnd.ms-excel",
			[".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
			[".ppt"] = "application/vnd.ms-powerpoint",
			[".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
			[".txt"] = "text/plain",
			[".rtf"] = "application/rtf",
			[".csv"] = "text/csv",
			[".md"] = "text/markdown",
			[".zip"] = "application/zip",
			[".png"] = "image/png",
			[".jpg"] = "image/jpeg",
			[".jpeg"] = "image/jpeg",
			[".webp"] = "image/webp",
			[".gif"] = "image/gif",
			[".bmp"] = "image/bmp",
			[".tiff"] = "image/tiff",
			[".svg"] = "image/svg+xml",
			[".mp4"] = "video/mp4",
			[".webm"] = "video/webm",
			[".mov"] = "video/quicktime",
			[".avi"] = "video/x-msvideo",
			[".mkv"] = "video/x-matroska",
			[".mp3"] = "audio/mpeg",
			[".m4a"] = "audio/mp4",
			[".wav"] = "audio/wav",
			[".ogg"] = "audio/ogg",
			[".aac"] = "audio/aac",
			[".ps1"] = "application/x-powershell",
			[".sh"] = "application/x-sh",
			[".bat"] = "application/x-msdos-program",
			[".json"] = "application/json",
			[".xml"] = "application/xml",
			[".yaml"] = "application/yaml",
			[".yml"] = "application/yaml",
		};
}
