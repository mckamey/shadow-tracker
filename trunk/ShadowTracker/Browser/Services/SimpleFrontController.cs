using System;
using System.Web;
using System.Web.Compilation;
using System.Web.UI;

using JsonFx.Json;

namespace Shadow.Browser.Services
{
	/// <summary>
	/// Loads the view which corresponds to the URL.
	/// </summary>
	public class SimpleFrontController : IHttpHandlerFactory
	{
		#region Constants

		private const string DefaultFile = "Default.aspx";
		private const string ViewPath = "~/"+DefaultFile;

		#endregion Constants

		#region IHttpHandlerFactory Members

		IHttpHandler IHttpHandlerFactory.GetHandler(HttpContext context, string requestType, string virtualPath, string physicalPath)
		{
			if (virtualPath.EndsWith(DefaultFile, StringComparison.OrdinalIgnoreCase))
			{
				virtualPath = virtualPath.Substring(0, virtualPath.Length-DefaultFile.Length);
				context.RewritePath(virtualPath, true);
			}
			else if (!virtualPath.EndsWith("/", StringComparison.OrdinalIgnoreCase))
			{
				context.Response.Redirect(virtualPath+'/');
			}

			if (String.IsNullOrEmpty(context.Request.QueryString[null]))
			{
				context.Items["TreeView"] = new EcmaScriptIdentifier("UIT.ExpandoTree");
			}
			else
			{
				context.Items["TreeView"] = new EcmaScriptIdentifier("UIT.FileFolderTree");
			}

			return BuildManager.CreateInstanceFromVirtualPath(ViewPath, typeof(Page)) as Page;
		}

		void IHttpHandlerFactory.ReleaseHandler(IHttpHandler handler)
		{
		}

		#endregion IHttpHandlerFactory Members
	}
}