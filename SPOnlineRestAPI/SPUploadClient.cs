using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SPOnlineRestAPI
{
	public class SPUploadClient
	{
		/// <summary>
		/// Return Form Digest information
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="webUrl"></param>
		/// <returns></returns>
		private static async Task<Models.FormDigestInfo.Rootobject> GetFormDigest(HttpClientHandler handler, string webUrl)
		{
			//Creating REST url to get Form Digest
			const string RESTURL = "{0}/_api/contextinfo";
			string restUrl = string.Format(RESTURL, webUrl);

			//Adding headers
			var client = new HttpClient(handler);
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");

			//Perform call
			HttpResponseMessage response = await client.PostAsync(restUrl, null).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			//Reading string data
			string jsonData = await response.Content.ReadAsStringAsync();

			//Creating FormDigest object
			Models.FormDigestInfo.Rootobject res = JsonConvert.DeserializeObject<Models.FormDigestInfo.Rootobject>(jsonData);
			return res;
		}

		/// <summary>
		/// Upload a document
		/// </summary>
		/// <param name="webUrl"></param>
		/// <param name="loginName"></param>
		/// <param name="pwd"></param>
		/// <param name="document"></param>
		/// <param name="folderServerRelativeUrl"></param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static async Task uploadDocumentAsync(string webUrl, string loginName, string pwd, System.IO.MemoryStream document, string folderServerRelativeUrl, string fileName)
		{
			try
			{
				//Creating credentials
				var passWord = new SecureString();
				foreach (var c in pwd) passWord.AppendChar(c);
				SharePointOnlineCredentials credential = new SharePointOnlineCredentials(loginName, passWord);

				//Creating REST url
				const string RESTURL = "{0}/_api/web/GetFolderByServerRelativeUrl('{1}')/Files/add(url='{2}',overwrite=true)";
				string rESTUrl = string.Format(RESTURL, webUrl, folderServerRelativeUrl, fileName);

				//Creating handler
				using (var handler = new HttpClientHandler() { Credentials = credential })
				{
					//Getting authentication cookies
					Uri uri = new Uri(webUrl);
					handler.CookieContainer.SetCookies(uri, credential.GetAuthenticationCookie(uri));

					//Getting form digest
					var tFormDigest = GetFormDigest(handler, webUrl);
					tFormDigest.Wait();

					//Creating HTTP Client
					using (var client = new HttpClient(handler))
					{
						client.DefaultRequestHeaders.Accept.Clear();
						client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
						client.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");
						client.DefaultRequestHeaders.Add("binaryStringRequestBody", "true");
						client.DefaultRequestHeaders.Add("X-RequestDigest", tFormDigest.Result.FormDigestValue);
						client.MaxResponseContentBufferSize = 2147483647;

						//Creating Content
						ByteArrayContent content = new ByteArrayContent(document.ToArray());

						//Perform post
						HttpResponseMessage response = await client.PostAsync(rESTUrl, content).ConfigureAwait(false);

						//Ensure 200 (Ok)
						response.EnsureSuccessStatusCode();
					}
				}
			}
			catch (Exception ex)
			{
				throw new ApplicationException($"Error uploading document {fileName} call on folder {folderServerRelativeUrl}. {ex.Message}", ex);
			}
		}
	}

	public class Models
	{
		public class FormDigestInfo
		{
			public class Rootobject
			{
				public int FormDigestTimeoutSeconds { get; set; }
				public string FormDigestValue { get; set; }
				public string LibraryVersion { get; set; }
				public string SiteFullUrl { get; set; }
				public string[] SupportedSchemaVersions { get; set; }
				public string WebFullUrl { get; set; }
			}
		}
	}
}

