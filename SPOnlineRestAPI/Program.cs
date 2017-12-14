using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using System.Security;
using Microsoft.SharePoint.Client;
using SPOnlineRestAPI;

namespace SPOnlineRestAPI
{
	class Program
	{
		static void Main(string[] args)
		{
			Uri myUri = new Uri("https://pttlab.sharepoint.com/sites/ScanDemo/", UriKind.Absolute);
			string sUsername = "steveyu2@pttlab.onmicrosoft.com";
			string sPassword = "P@ssw0rd1234";
			string sDocLib = "Documents";

			//GetListItems(myUri, sUsername, sPassword,sDocLib);
			//GetListItemByID(myUri, sUsername, sPassword, sDocLib, "37");
			UploadDoc(myUri, sUsername, sPassword, sDocLib);
		}

		public static void GetListItems(Uri webUri,string userName,string password,string LstTitle)
		{
			using (var client = new SPHttpClient(webUri, userName, password))
			{
				var listTitle = LstTitle;
				var endpointUrl = string.Format("{0}/_api/web/lists/getbytitle('{1}')/items", webUri, listTitle);
				//The JSON Result
				var data = client.ExecuteJson(endpointUrl);
				//foreach (var item in data["Value"])
				//{
				//	Console.WriteLine(item["Title"]);
				//}
			}
		}

		public static void GetListItemByID(Uri webUri, string userName, string password, string LstTitle,string itemId)
		{
			using (var client = new SPHttpClient(webUri, userName, password))
			{
				//var listTitle = "Tasks";
				//var itemId = 1;
				var endpointUrl = string.Format("{0}/_api/web/lists/getbytitle('{1}')/items({2})", webUri, LstTitle, itemId);
				var data = client.ExecuteJson(endpointUrl);
				//Console.WriteLine(data["Title"]);
			}
		}

		public static void CreateDoc(Uri webUri, string userName, string password, string LstTitle)
		{
			using (var client = new SPHttpClient(webUri, userName, password))
			{
				var passWord = new SecureString();
				foreach (var c in password) passWord.AppendChar(c);
				SharePointOnlineCredentials credential = new SharePointOnlineCredentials(userName, passWord);

				//byte[] bytefile = System.IO.File.ReadAllBytes(pathToFile);
				string siteurl = webUri.ToString();
				string documentlibrary = LstTitle; //Document library where file needs to be uploaded

				string filePath = @"C:\temp\test.docx";

				byte[] binary = System.IO.File.ReadAllBytes(filePath);
				string fname = System.IO.Path.GetFileName(filePath);
				string result = string.Empty;
				//Url to upload file
				string resourceUrl = string.Format("{0}/_api",siteurl);

				RestClient RC = new RestClient(resourceUrl);
				NetworkCredential NCredential = System.Net.CredentialCache.DefaultNetworkCredentials;
				//client.c
				RC.Authenticator = new NtlmAuthenticator(NCredential);

				Console.WriteLine("Creating Rest Request");

				RestRequest Request = new RestRequest("contextinfo ?$select = FormDigestValue", Method.POST);
				Request.AddHeader("Accept", "application/json; odata = verbose");
				Request.AddHeader("Body", "");

				string ReturnedStr = RC.Execute(Request).Content;
				int StartPos = ReturnedStr.IndexOf("FormDigestValue") + 18;
				int length = ReturnedStr.IndexOf(@""",", StartPos)-StartPos;
				string FormDigestValue = ReturnedStr.Substring(StartPos, length);

				Console.WriteLine("Uploading file Site……");

				resourceUrl = string.Format("/web/GetFolderByServerRelativeUrl(‘{ 0}’)/ Files / add(url = '{1}’,overwrite=true)", documentlibrary ,fname);
					Request = new RestRequest(resourceUrl, Method.POST);
				Request.RequestFormat = DataFormat.Json;
				Request.AddHeader("Accept", "application/json; odata = verbose");
				Request.AddHeader("X - RequestDigest", FormDigestValue);
				Console.WriteLine("File is successfully uploaded to sharepoint site.");
				Console.ReadLine();

			}
		}

		
		public static void UploadDoc(Uri webUri, string userName, string password, string LstTitle)
		{
			try
			{
				const string DOCNAME = @"c:\temp\test.docx";
				const string FOLDERURL = "Documents";

				//Reading document from file system
				System.IO.MemoryStream doc = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(DOCNAME));

				//Uploading document
				
				var t = SPUploadClient.uploadDocumentAsync(webUri.ToString(), userName, password, doc, FOLDERURL, System.IO.Path.GetFileName(DOCNAME));
				t.Wait();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			Console.ReadLine();
		}



	}
}
