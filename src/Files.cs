﻿using B2Net.Http;
using B2Net.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace B2Net
{
  public class Files : IFiles
  {
		private B2Options _options;
		private HttpClient _client;
	  private string _api = "Files";
		
		public Files(B2Options options) {
			_options = options;
			_client = HttpClientFactory.CreateHttpClient(options.RequestTimeout);
		}

		/// <summary>
		/// Lists the names of all non-hidden files in a bucket, starting at a given name.
		/// </summary>
		/// <param name="bucketId"></param>
		/// <param name="startFileName"></param>
		/// <param name="maxFileCount"></param>
		/// <param name="cancelToken"></param>
		/// <returns></returns>
		public async Task<B2FileList> GetList(string startFileName = "", int? maxFileCount = null, string bucketId = "", CancellationToken cancelToken = default(CancellationToken)) {
		    return await GetListWithPrefixOrDemiliter(startFileName, "", "", maxFileCount, bucketId, cancelToken);
		}

        /// <summary>
        /// BETA: Lists the names of all non-hidden files in a bucket, starting at a given name. With an optional file prefix or delimiter.
        /// See here for more details: https://www.backblaze.com/b2/docs/b2_list_file_names.html
        /// </summary>
        /// <param name="startFileName"></param>
        /// <param name="prefix"></param>
        /// <param name="delimiter"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="bucketId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
	    public async Task<B2FileList> GetListWithPrefixOrDemiliter(string startFileName = "", string prefix = "", string delimiter = "", int? maxFileCount = null, string bucketId = "", CancellationToken cancelToken = default(CancellationToken)) {
	        var operationalBucketId = Utilities.DetermineBucketId(_options, bucketId);

	        var requestMessage = FileMetaDataRequestGenerators.GetList(_options, operationalBucketId, startFileName, maxFileCount, prefix, delimiter);
	        var response = await _client.SendAsync(requestMessage, cancelToken);

	        return await ResponseParser.ParseResponse<B2FileList>(response, _api);
	    }

        /// <summary>
        /// Lists all of the versions of all of the files contained in one bucket,
        /// in alphabetical order by file name, and by reverse of date/time uploaded
        /// for versions of files with the same name.
        /// </summary>
        /// <param name="startFileName"></param>
        /// <param name="startFileId"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="bucketId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<B2FileList> GetVersions(string startFileName = "", string startFileId = "", int? maxFileCount = null, string bucketId = "", CancellationToken cancelToken = default(CancellationToken)) {
            return await GetVersionsWithPrefixOrDelimiter(startFileName, startFileId, "", "", maxFileCount, bucketId, cancelToken);
        }

        /// <summary>
        /// BETA: Lists all of the versions of all of the files contained in one bucket,
        /// in alphabetical order by file name, and by reverse of date/time uploaded
        /// for versions of files with the same name. With an optional file prefix or delimiter.
        /// See here for more details: https://www.backblaze.com/b2/docs/b2_list_file_versions.html
        /// </summary>
        /// <param name="startFileName"></param>
        /// <param name="startFileId"></param>
        /// <param name="prefix"></param>
        /// <param name="delimiter"></param>
        /// <param name="maxFileCount"></param>
        /// <param name="bucketId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
	    public async Task<B2FileList> GetVersionsWithPrefixOrDelimiter(string startFileName = "", string startFileId = "", string prefix = "", string delimiter = "", int? maxFileCount = null, string bucketId = "", CancellationToken cancelToken = default(CancellationToken)) {
	        var operationalBucketId = Utilities.DetermineBucketId(_options, bucketId);

	        var requestMessage = FileMetaDataRequestGenerators.ListVersions(_options, operationalBucketId, startFileName, startFileId, maxFileCount, prefix, delimiter);
	        var response = await _client.SendAsync(requestMessage, cancelToken);

	        return await ResponseParser.ParseResponse<B2FileList>(response, _api);
	    }

        /// <summary>
        /// Gets information about one file stored in B2.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<B2File> GetInfo(string fileId, CancellationToken cancelToken = default(CancellationToken)) {
			var requestMessage = FileMetaDataRequestGenerators.GetInfo(_options, fileId);
			var response = await _client.SendAsync(requestMessage, cancelToken);

			return await ResponseParser.ParseResponse<B2File>(response, _api);
        }

        /// <summary>
        /// get an upload url for use with one Thread.
        /// </summary>
        /// <param name="bucketId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<B2UploadUrl> GetUploadUrl(string bucketId = "", CancellationToken cancelToken = default(CancellationToken)) {
            var operationalBucketId = Utilities.DetermineBucketId(_options, bucketId);

            // send the request.
            var uploadUrlRequest = FileUploadRequestGenerators.GetUploadUrl(_options, operationalBucketId);
            var uploadUrlResponse = await _client.SendAsync(uploadUrlRequest, cancelToken);

            // parse response and return it.
            var uploadUrl = await ResponseParser.ParseResponse<B2UploadUrl>(uploadUrlResponse);

            // Set the upload auth token
            _options.UploadAuthorizationToken = uploadUrl.AuthorizationToken;

            return uploadUrl;
        }

        /// <summary>
        /// Uploads one file to B2, returning its unique file ID. Filename will be URL Encoded.
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="fileName"></param>
        /// <param name="bucketId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<B2File> Upload(byte[] fileData, string fileName, B2UploadUrl uploadUrl, string bucketId = "", Dictionary<string, string> fileInfo = null, CancellationToken cancelToken = default(CancellationToken)) {
            return await Upload(fileData, fileName, uploadUrl, false, bucketId, fileInfo, cancelToken);
        }

        /// <summary>
        /// Uploads one file to B2, returning its unique file ID. Filename will be URL Encoded. If auto retry
        /// is set true it will retry a failed upload once after 1 second.
        /// </summary>
        /// <param name="fileData"></param>
        /// <param name="fileName"></param>
        /// <param name="uploadUrl"></param>
        /// <param name="bucketId"></param>
        /// <param name="autoRetry">Retry a failed upload one time after waiting for 1 second.</param>
        /// <param name="fileInfo"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
	    public async Task<B2File> Upload(byte[] fileData, string fileName, B2UploadUrl uploadUrl, bool autoRetry, string bucketId = "", Dictionary<string, string> fileInfo = null, CancellationToken cancelToken = default(CancellationToken)) {
	        // Now we can upload the file
	        var requestMessage = FileUploadRequestGenerators.Upload(_options, uploadUrl.UploadUrl, fileData, fileName, fileInfo);

			return await Upload(requestMessage, autoRetry, cancelToken);
		}

		/// <summary>
		/// Uploads one file to B2, returning its unique file ID. Filename will be URL Encoded.
		/// </summary>
		/// <param name="fileData"></param>
		/// <param name="fileName"></param>
		/// <param name="bucketId"></param>
		/// <param name="cancelToken"></param>
		/// <returns></returns>
		public async Task<B2File> Upload(Stream fileData, string fileName, B2UploadUrl uploadUrl, string bucketId = "", Dictionary<string, string> fileInfo = null, CancellationToken cancelToken = default(CancellationToken))
		{
			return await Upload(fileData, fileName, uploadUrl, false, bucketId, fileInfo, cancelToken);
		}

		/// <summary>
		/// Uploads one file to B2, returning its unique file ID. Filename will be URL Encoded. If auto retry
		/// is set true it will retry a failed upload once after 1 second.
		/// </summary>
		/// <param name="fileData"></param>
		/// <param name="fileName"></param>
		/// <param name="uploadUrl"></param>
		/// <param name="bucketId"></param>
		/// <param name="autoRetry">Retry a failed upload one time after waiting for 1 second.</param>
		/// <param name="fileInfo"></param>
		/// <param name="cancelToken"></param>
		/// <returns></returns>
		public async Task<B2File> Upload(Stream fileData, string fileName, B2UploadUrl uploadUrl, bool autoRetry, string bucketId = "", Dictionary<string, string> fileInfo = null, CancellationToken cancelToken = default(CancellationToken))
		{
			// Now we can upload the file
			var requestMessage = FileUploadRequestGenerators.Upload(_options, uploadUrl.UploadUrl, fileData, fileName, fileInfo);

			return await Upload(requestMessage, autoRetry, cancelToken);
		}

		private async Task<B2File> Upload(HttpRequestMessage requestMessage, bool autoRetry, CancellationToken cancelToken = default(CancellationToken))
		{
			var response = await _client.SendAsync(requestMessage, cancelToken);
			// Auto retry
			if (autoRetry && (
					response.StatusCode == (HttpStatusCode)429 ||
					response.StatusCode == HttpStatusCode.RequestTimeout ||
					response.StatusCode == HttpStatusCode.ServiceUnavailable))
			{
				Task.Delay(1000, cancelToken).Wait(cancelToken);
				response = await _client.SendAsync(requestMessage, cancelToken);
			}

			return await ResponseParser.ParseResponse<B2File>(response, _api);
		}

		/// <summary>
		/// Downloads a file part by providing the name of the bucket and the name and byte range of the file.
		/// For use with the Larg File API.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="bucketName"></param>
		/// <param name="startBytes"></param>
		/// <param name="endBytes"></param>
		/// <param name="cancelToken"></param>
		/// <returns></returns>
		public async Task<B2File> DownloadByName(string fileName, string bucketName, int startByte, int endByte, bool useStream = false,
			CancellationToken cancelToken = default(CancellationToken)) {
            // Are we searching by name or id?
            HttpRequestMessage request;
            request = FileDownloadRequestGenerators.DownloadByName(_options, bucketName, fileName, $"{startByte}-{endByte}");

            // Send the download request
            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);

            // Create B2File from response
            return await ParseDownloadResponse(response, useStream);
        }

        /// <summary>
        /// Downloads one file by providing the name of the bucket and the name of the file.
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="fileName"></param>
        /// <param name="bucketId"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<B2File> DownloadByName(string fileName, string bucketName, bool useStream = false, CancellationToken cancelToken = default(CancellationToken)) {
			// Are we searching by name or id?
			HttpRequestMessage request;
			request = FileDownloadRequestGenerators.DownloadByName(_options, bucketName, fileName);

			// Send the download request
			var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);

			// Create B2File from response
			return await ParseDownloadResponse(response, useStream);
	    }

	    /// <summary>
	    /// Downloads a file from B2 using the byte range specified. For use with the Large File API.
	    /// </summary>
	    /// <param name="fileId"></param>
	    /// <param name="cancelToken"></param>
	    /// <returns></returns>
	    public async Task<B2File> DownloadById(string fileId, int startByte, int endByte, bool useStream = false, CancellationToken cancelToken = default(CancellationToken)) {
	        // Are we searching by name or id?
	        HttpRequestMessage request;
	        request = FileDownloadRequestGenerators.DownloadById(_options, fileId, $"{startByte}-{endByte}");

	        // Send the download request
	        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);

	        // Create B2File from response
	        return await ParseDownloadResponse(response, useStream);
	    }

	    /// <summary>
	    /// Downloads one file from B2.
	    /// </summary>
	    /// <param name="fileId"></param>
	    /// <param name="cancelToken"></param>
	    /// <returns></returns>
	    public async Task<B2File> DownloadById(string fileId, bool useStream = false, CancellationToken cancelToken = default(CancellationToken)) {
	        // Are we searching by name or id?
	        HttpRequestMessage request;
	        request = FileDownloadRequestGenerators.DownloadById(_options, fileId);

	        // Send the download request
	        var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);

	        // Create B2File from response
	        return await ParseDownloadResponse(response, useStream);
	    }

        /// <summary>
        /// Deletes the specified file version
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="fileName"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task<B2File> Delete(string fileId, string fileName, CancellationToken cancelToken = default(CancellationToken)) {
			var requestMessage = FileDeleteRequestGenerator.Delete(_options, fileId, fileName);
			var response = await _client.SendAsync(requestMessage, cancelToken);

			return await ResponseParser.ParseResponse<B2File>(response, _api);
		}


        /// <summary>
        /// EXPERIMENTAL: This functionality is not officially part of the Backblaze B2 API and may change or break at any time.
        /// This will return a friendly URL that can be shared to download the file. This depends on the Bucket that the file resides 
        /// in to be allPublic.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="bucketName"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
	    public string GetFriendlyDownloadUrl(string fileName, string bucketName, CancellationToken cancelToken = default(CancellationToken)) {
	        var downloadUrl = _options.DownloadUrl;
	        var friendlyUrl = "";
            if (!string.IsNullOrEmpty(downloadUrl)) {
                friendlyUrl = $"{downloadUrl}/file/{bucketName}/{fileName}";
            }
	        return friendlyUrl;
	    }

		/// <summary>
		/// Hides or Unhides a file so that downloading by name will not find the file,
		/// but previous versions of the file are still stored. See File
		/// Versions about what it means to hide a file.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="bucketId"></param>
		/// <param name="cancelToken"></param>
		/// <returns></returns>
		public async Task<B2File> Hide(string fileName, string bucketId = "", string fileId = "", CancellationToken cancelToken = default(CancellationToken)) {
			var operationalBucketId = Utilities.DetermineBucketId(_options, bucketId);

			var requestMessage = FileMetaDataRequestGenerators.HideFile(_options, operationalBucketId, fileName, fileId);
			var response = await _client.SendAsync(requestMessage, cancelToken);

			return await ResponseParser.ParseResponse<B2File>(response, _api);
		}

		/// <summary>
		/// Downloads one file from B2.
		/// </summary>
		/// <param name="fileId"></param>
		/// <param name="cancelToken"></param>
		/// <returns></returns>
		public async Task<B2DownloadAuthorization> GetDownloadAuthorization(string fileNamePrefix, int validDurationInSeconds, string bucketId = "", string b2ContentDisposition = "", CancellationToken cancelToken = default(CancellationToken)) {
			var operationalBucketId = Utilities.DetermineBucketId(_options, bucketId);

			var request = FileDownloadRequestGenerators.GetDownloadAuthorization(_options, fileNamePrefix, validDurationInSeconds, operationalBucketId, b2ContentDisposition);

			// Send the download request
			var response = await _client.SendAsync(request, cancelToken);

			// Create B2File from response
			return await ResponseParser.ParseResponse<B2DownloadAuthorization>(response, _api);
		}

		private async Task<B2File> ParseDownloadResponse(HttpResponseMessage response, bool useStream = false) {
			Utilities.CheckForErrors(response, _api);

			var file = new B2File();
			IEnumerable<string> values;
			if (response.Headers.TryGetValues("X-Bz-Content-Sha1", out values)) {
				file.ContentSHA1 = values.First();
			}
			if (response.Headers.TryGetValues("X-Bz-File-Name", out values)) {
				file.FileName = values.First();
				// Decode file name
				file.FileName = file.FileName.b2UrlDecode();
			}
			if (response.Headers.TryGetValues("X-Bz-File-Id", out values)) {
				file.FileId = values.First();
			}
            // File Info Headers
            var fileInfoHeaders = response.Headers.Where(h => h.Key.ToLower().Contains("x-bz-info"));
            var infoData = new Dictionary<string, string>();
            if (fileInfoHeaders.Count() > 0) {
                foreach (var fileInfo in fileInfoHeaders)
                {
                    // Substring to parse out the file info prefix.
                    infoData.Add(fileInfo.Key.Substring(10), fileInfo.Value.First());
                }
            }
            file.FileInfo = infoData;
            if (response.Content.Headers.ContentLength.HasValue) {
                file.Size = response.Content.Headers.ContentLength.Value;
            }
			if (useStream)
			{
				file.Content = response.Content;
			}
			else
			{
				file.FileData = await response.Content.ReadAsByteArrayAsync();
			}

			return await Task.FromResult(file);
		}
	}
}
