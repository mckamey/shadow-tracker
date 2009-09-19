using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using MimeUtils;
using JsonFx.JsonRpc;
using Shadow.Model;

namespace Shadow.Browser.Services
{
	[JsonService(Namespace="Shadow", Name="BrowseService")]
	public class BrowseService
	{
		#region Constants

		private static readonly string PhysicalRoot;
		private static readonly string VirtualRoot;

		private static readonly Regex Regex_InvalidChars = new Regex(@"[&#%=]", RegexOptions.Compiled);
		private static readonly Regex Regex_EncodedChars = new Regex(@"_0x(?<charCode>[0-9]+)_", RegexOptions.Compiled|RegexOptions.ExplicitCapture);

		private const long MaxFileSize = 1024L*1024L;// cap at 1MB

		#endregion Constants

		#region Init

		/// <summary>
		/// CCtor
		/// </summary>
		static BrowseService()
		{
			BrowseService.PhysicalRoot = HttpRuntime.AppDomainAppPath;
			BrowseService.VirtualRoot = HttpRuntime.AppDomainAppVirtualPath;
			if (String.IsNullOrEmpty(BrowseService.VirtualRoot) ||
				BrowseService.VirtualRoot.Length > 1)
			{
				BrowseService.VirtualRoot += '/';
			}
		}

		#endregion Init

		#region Service Methods

		[JsonMethod("getSummary")]
		public CatalogEntry GetSummary(string path)
		{
			path = BrowseService.RepairPath(path, false).ToLowerInvariant();

			IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();

			var entry =
				(from n in unitOfWork.Entries
				 where n.Parent.ToLower()+n.Name.ToLower() == path
				 select n).FirstOrDefault();

			return entry;
		}

		[JsonMethod("browse")]
		public object Browse(string path)
		{
			path = BrowseService.RepairPath(path, true);

			IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();

			var children =
				from n in unitOfWork.Entries
				where n.Parent.ToLower() == path
				orderby n.Name
				select n;

			var nodes =
				from n in children.AsEnumerable().Distinct(CatalogEntry.PathComparer)
				let category =
					n.IsDirectory ?
					MimeCategory.Folder :
					MimeTypes.GetByExtension(Path.GetExtension(n.Name)).Category
				let name = this.GetName(n.Name)
				orderby n.IsDirectory descending
				select new {
					name = name,
					path = BrowseService.ScrubPath(n.Parent+n.Name),
					category = category,
					isSpecial = !StringComparer.Ordinal.Equals(name, n.Name)
				};

			return new
			{
				name = Path.GetDirectoryName(path) ?? String.Empty,
				path = path,
				category = MimeCategory.Folder,
				children = nodes
			};
		}

		#endregion Service Methods

		#region Utility Methods

		private string GetName(string folderName)
		{
			if ((folderName.StartsWith("[ ") && folderName.EndsWith(" ]")) ||
				(folderName.StartsWith("( ") && folderName.EndsWith(" )")))
			{
				return folderName.Substring(2, folderName.Length-4);
			}

			return folderName;
		}

		public static string GetPhysicalPath(string path)
		{
			path = BrowseService.RepairPath(path, false);
			if (path.Length < BrowseService.VirtualRoot.Length)
			{
				throw new ArgumentException("Invalid path.");
			}

			path = path.Substring(BrowseService.VirtualRoot.Length-1);
			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			if (path.IndexOf("../") >= 0)
			{
				throw new NotSupportedException("Invalid path.");
			}

			if (path.StartsWith("\\"))
			{
				path = path.Substring(1);
			}

			path = BrowseService.PhysicalRoot + path;
			return path;
		}

		private static string ScrubPath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				return String.Empty;
			}

			path = BrowseService.Regex_InvalidChars.Replace(path, BrowseService.InvalidCharReplace);
			return HttpUtility.UrlPathEncode(path);
		}

		private static string RepairPath(string path, bool asDir)
		{
			if (String.IsNullOrEmpty(path))
			{
				return VirtualRoot;
			}

			path = HttpUtility.UrlDecode(path);
			if (asDir)
			{
				path = path.TrimEnd('/').ToLowerInvariant()+'/';
			}

			return BrowseService.Regex_EncodedChars.Replace(path, BrowseService.EncodedCharReplace);
		}

		private static string InvalidCharReplace(Match match)
		{
			if (!match.Success || String.IsNullOrEmpty(match.Value) || (match.Value.Length < 1))
			{
				return match.Value;
			}

			return String.Format("_0x{0:x2}_", (int)match.Value[0]);
		}

		private static string EncodedCharReplace(Match match)
		{
			int charCode;
			if (!Int32.TryParse(match.Groups["charCode"].Value, NumberStyles.HexNumber,
				CultureInfo.InvariantCulture, out charCode))
			{
				return match.Value;
			}

			return ((char)charCode).ToString();
		}

		#endregion Utility Methods
	}
}