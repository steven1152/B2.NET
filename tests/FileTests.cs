﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2Net.Http;
using B2Net.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace B2Net.Tests {
	[TestClass]
	public class FileTests : BaseTest {
		B2Bucket TestBucket = new B2Bucket();
		B2Client Client = null;

		[TestInitialize]
		public void Initialize() {
			Client = new B2Client(Options);
			Options = Client.Authorize().Result;
			TestBucket = Client.Buckets.Create("B2NETTestingBucket", BucketTypes.allPrivate).Result;
		}

		[TestMethod]
		public void GetListTest() {
			var fileName = "B2Test.txt";
			var fileData = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
			var file = Client.Files.Upload(fileData, fileName, TestBucket.BucketId).Result;

			var list = Client.Files.GetList(bucketId: TestBucket.BucketId).Result.Files;

			// Delete file
			var deletedFile = Client.Files.Delete(file.FileId, file.FileName).Result;
			
			Assert.AreEqual(1, list.Count, list.Count + " files found.");
		}

		[TestMethod]
		public void HideFileTest() {
			var fileName = "B2Test.txt";
			var fileData = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
			string hash = Utilities.GetSHA1Hash(fileData);
			var file = Client.Files.Upload(fileData, fileName, TestBucket.BucketId).Result;

			Assert.AreEqual(hash, file.ContentSHA1, "File hashes did not match.");

			// Clean up. We have to delete the file before we can delete the bucket
			var hiddenFile = Client.Files.Hide(file.FileName, TestBucket.BucketId).Result;

			Assert.IsTrue(hiddenFile.Action == "hide");

			// Clean up. We have to delete the file before we can delete the bucket
			var deletedFile = Client.Files.Delete(hiddenFile.FileId, hiddenFile.FileName).Result;
		}

		[TestMethod]
		public void FileUploadTest() {
			var fileName = "B2Test.txt";
			var fileData = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
			string hash = Utilities.GetSHA1Hash(fileData);
			var file = Client.Files.Upload(fileData, fileName, TestBucket.BucketId).Result;

			Assert.AreEqual(hash, file.ContentSHA1, "File hashes did not match.");

			// Clean up. We have to delete the file before we can delete the bucket
			var deletedFile = Client.Files.Delete(file.FileId, file.FileName).Result;
		}

		[TestMethod]
		public void FileDeleteTest() {
			var fileName = "B2Test.txt";
			var fileData = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
			string hash = Utilities.GetSHA1Hash(fileData);
			var file = Client.Files.Upload(fileData, fileName, TestBucket.BucketId).Result;

			Assert.AreEqual(hash, file.ContentSHA1, "File hashes did not match.");

			// Clean up. We have to delete the file before we can delete the bucket
			var deletedFile = Client.Files.Delete(file.FileId, file.FileName).Result;

			Assert.AreEqual(file.FileId, deletedFile.FileId);
		}

		[TestMethod]
		public void ListVersionsTest() {
			var fileName = "B2Test.txt";
			var fileData = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
			string hash = Utilities.GetSHA1Hash(fileData);
			var file = Client.Files.Upload(fileData, fileName, TestBucket.BucketId).Result;

			Assert.AreEqual(hash, file.ContentSHA1, "File hashes did not match.");
			
			var versions = Client.Files.GetVersions(file.FileName, file.FileId, bucketId: TestBucket.BucketId).Result;

			// Clean up. We have to delete the file before we can delete the bucket
			var deletedFile = Client.Files.Delete(file.FileId, file.FileName).Result;

			Assert.AreEqual(1, versions.Files.Count);
		}

		[TestMethod]
		public void GetInfoTest() {
			var fileName = "B2Test.txt";
			var fileData = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
			string hash = Utilities.GetSHA1Hash(fileData);
			var file = Client.Files.Upload(fileData, fileName, TestBucket.BucketId).Result;

			Assert.AreEqual(hash, file.ContentSHA1, "File hashes did not match.");

			var info = Client.Files.GetInfo(file.FileId).Result;

			// Clean up. We have to delete the file before we can delete the bucket
			var deletedFile = Client.Files.Delete(file.FileId, file.FileName).Result;

			Assert.AreEqual(file.UploadTimestamp, info.UploadTimestamp);
		}

		[TestCleanup]
		public void Cleanup() {
			var deletedBucket = Client.Buckets.Delete(TestBucket.BucketId).Result;
		}
	}
}
