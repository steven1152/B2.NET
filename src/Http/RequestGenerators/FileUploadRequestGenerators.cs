﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using B2Net.Http.RequestGenerators;
using B2Net.Models;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace B2Net.Http {
	public static class FileUploadRequestGenerators {
		private static class Endpoints {
			public const string GetUploadUrl = "b2_get_upload_url";
		}

        /// <summary>
        /// Upload a file to B2. This method will calculate the SHA1 checksum before sending any data.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="uploadUrl"></param>
        /// <param name="fileData"></param>
        /// <param name="fileName"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static HttpRequestMessage Upload(B2Options options, string uploadUrl, byte[] fileData, string fileName, Dictionary<string, string> fileInfo)
        {
            var uri = new Uri(uploadUrl);
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = new ByteArrayContent(fileData),
            };
			request.Content.Headers.ContentLength = fileData.Length;

			// Get the file checksum
			string hash = Utilities.GetSHA1Hash(fileData);

			return Upload(request, options, fileName, fileInfo, hash);
		}

		/// <summary>
		/// Upload a file to B2. This method will calculate the SHA1 checksum before sending any data.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="uploadUrl"></param>
		/// <param name="fileData"></param>
		/// <param name="fileName"></param>
		/// <param name="fileInfo"></param>
		/// <returns></returns>
		public static HttpRequestMessage Upload(B2Options options, string uploadUrl, Stream fileData, string fileName, Dictionary<string, string> fileInfo)
		{
			var uri = new Uri(uploadUrl);
			var request = new HttpRequestMessage()
			{
				Method = HttpMethod.Post,
				RequestUri = uri,
				Content = new StreamContent(fileData)
			};
			request.Content.Headers.ContentLength = fileData.Length;

			// Get the file checksum
			string hash = Utilities.GetSHA1Hash(fileData);

			return Upload(request, options, fileName, fileInfo, hash);
		}

		private static HttpRequestMessage Upload(HttpRequestMessage request, B2Options options, string fileName, Dictionary<string, string> fileInfo, string hash)
		{
			// Add headers
			request.Headers.TryAddWithoutValidation("Authorization", options.UploadAuthorizationToken);
			request.Headers.Add("X-Bz-File-Name", fileName.b2UrlEncode());
			request.Headers.Add("X-Bz-Content-Sha1", hash);
			// File Info headers
			if (fileInfo != null && fileInfo.Count > 0)
			{
				foreach (var info in fileInfo.Take(10))
				{
					request.Headers.Add($"X-Bz-Info-{info.Key}", info.Value);
				}
			}
			// TODO last modified
			//request.Headers.Add("X-Bz-src_last_modified_millis", hash);

			request.Content.Headers.ContentType = new MediaTypeHeaderValue("b2/x-auto");

			return request;
		}

		public static HttpRequestMessage GetUploadUrl(B2Options options, string bucketId) {
            var json = JsonConvert.SerializeObject(new { bucketId });
            return BaseRequestGenerator.PostRequest(Endpoints.GetUploadUrl, json, options);
		}
	}
}
