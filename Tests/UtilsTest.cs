﻿using System;
using System.Collections.Generic;
using System.Text;
using MbUnit.Framework;
using dnGREP;
using System.IO;
using System.Linq;
using dnGREP.Common;

namespace Tests
{
	[TestFixture]
	public class UtilsTest : TestBase
	{
		string sourceFolder;
		string destinationFolder;

        [FixtureSetUp]
		public void Initialize()
		{
			sourceFolder = GetDllPath() + "\\Files";
		}

		[SetUp]
		public void CreateTempFolder()
		{
			destinationFolder = Path.GetTempPath() + Guid.NewGuid().ToString();
			Directory.CreateDirectory(destinationFolder);
		}

		[TearDown]
		public void DeleteTempFolder()
		{
			if (Directory.Exists(destinationFolder))
				Utils.DeleteFolder(destinationFolder);
		}

		[Test]
		[Row("Hello world", "Hello world", 2, 1)]
		[Row("Hi", "Hi", 2, 1)]
		[Row("Hi\r\n\r\nWorld", "", 4, 2)]
		[Row("Hi\r\n\r\nWorld", "World", 6, 3)]
		[Row(null, null, 6, -1)]
		public void TestGetLine(string body, string line, int index, int lineNumber)
		{
			int returnedLineNumber = -1;
			string returnedLine = Utils.GetLine(body, index, out returnedLineNumber);
			Assert.AreEqual(returnedLine, line);
			Assert.AreEqual(returnedLineNumber, lineNumber);
		}

		[Test]
		public void TestGetContextLines()
		{
			string test = "Hi\r\nmy\r\nWorld\r\nMy name is Denis\r\nfor\r\nloop";
            
            List<GrepSearchResult.GrepMatch> bodyMatches = new List<GrepSearchResult.GrepMatch>();
            List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 9, 2));
                lines = Utils.GetLinesEx(reader, bodyMatches, 2, 2);
            }
            Assert.AreEqual(lines.Count, 5);
			Assert.AreEqual(lines[0].LineNumber, 1);
			Assert.AreEqual(lines[0].LineText, "Hi");
			Assert.AreEqual(lines[0].IsContext, true);
            Assert.AreEqual(lines[1].IsContext, true);
            Assert.AreEqual(lines[2].IsContext, false);
			Assert.AreEqual(lines[3].LineNumber, 4);
			Assert.AreEqual(lines[3].LineText, "My name is Denis");
			Assert.AreEqual(lines[3].IsContext, true);
			Assert.AreEqual(lines[4].LineNumber, 5);
			Assert.AreEqual(lines[4].LineText, "for");
			Assert.AreEqual(lines[4].IsContext, true);


            bodyMatches = new List<GrepSearchResult.GrepMatch>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 9, 2));
                lines = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }
			Assert.AreEqual(lines.Count, 1);

            bodyMatches = new List<GrepSearchResult.GrepMatch>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 4, 1));
                lines = Utils.GetLinesEx(reader, bodyMatches, 10, 0);
            }
            Assert.AreEqual(lines.Count, 2);
			Assert.AreEqual(lines[0].LineNumber, 1);
			Assert.AreEqual(lines[0].LineText, "Hi");
			Assert.AreEqual(lines[0].IsContext, true);
            Assert.AreEqual(lines[1].LineText, "my");
            Assert.AreEqual(lines[1].IsContext, false);

            bodyMatches = new List<GrepSearchResult.GrepMatch>();
            using (StringReader reader = new StringReader(test))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 34, 1));
                lines = Utils.GetLinesEx(reader, bodyMatches, 1, 10);
            }

            Assert.AreEqual(lines.Count, 3);
            Assert.AreEqual(lines[0].LineNumber, 4);
			Assert.AreEqual(lines[1].LineNumber, 5);
			Assert.AreEqual(lines[1].LineText, "for");
            Assert.AreEqual(lines[1].IsContext, false);
			Assert.AreEqual(lines[2].LineNumber, 6);
			Assert.AreEqual(lines[2].LineText, "loop");
			Assert.AreEqual(lines[2].IsContext, true);			
		}

        [Test]
        public void TestDefaultSettings()
        {
            var type = GrepSettings.Instance.Get<SearchType>(GrepSettings.Key.TypeOfSearch);
            Assert.AreEqual<SearchType>(type, SearchType.Regex);
        }

		[Test]
		[Row("hello\rworld", "hello\r\nworld")]
		[Row("hello\nworld", "hello\r\nworld")]
		[Row("hello\rworld\r", "hello\r\nworld\r")]
		public void TestCleanLineBreaks(string input, string output)
		{
			string result = Utils.CleanLineBreaks(input);
			Assert.AreEqual(result, output);
		}

        [Test]
		[Row("\\Files\\TestCase1\\test-file-code.cs", "\\Files\\TestCase1")]
		[Row("\\Files\\TestCase1", "\\Files\\TestCase1")]
		[Row("\\Files\\TestCas\\", null)]
		[Row("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt", "\\Files\\TestCase1")]
		public void TestGetBaseFolder(string relativePath, string result)
		{
			StringBuilder sb = new StringBuilder();
			string pathToDll = GetDllPath();
			string[] rPaths = relativePath.Split(';');
			foreach (string rPath in rPaths)
				sb.Append(pathToDll + rPath + ";");

			if (result == null)
				Assert.AreEqual(Utils.GetBaseFolder(sb.ToString()), null);
			else
				Assert.AreEqual(Utils.GetBaseFolder(sb.ToString()), pathToDll + result);
		}

		[Test]
		[Row("\\Files\\TestCase7\\Test;Folder", "\\Files\\TestCase7\\Test;Folder")]
		[Row("\\Files\\TestCase7\\Test,Folder", "\\Files\\TestCase7\\Test,Folder")]
		public void TestGetBaseFolderWithColons(string relativePath, string result)
		{
			string pathToDll = GetDllPath();
			relativePath = pathToDll + relativePath;

			if (result == null)
				Assert.AreEqual(Utils.GetBaseFolder(relativePath), null);
			else
				Assert.AreEqual(Utils.GetBaseFolder(relativePath), pathToDll + result);
		}

        [Test]
		[Row("\\Files\\TestCase1\\test-file-code.cs", true)]
		[Row("\\Files\\TestCase1\\test-file-code2.cs", false)]
		[Row("\\Files\\TestCase1\\", true)]
		[Row("\\Files\\TestCase1", true)]
		[Row("\\Files\\TestCas\\", false)]
		[Row("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt", true)]
		[Row("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt;\\Files\\TestCase1", true)]
		[Row("\\Files\\TestCase1\\test11-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt;\\Files\\TestCase1", false)]
		[Row("\\Files\\TestCase1\\test-file-code.cs;\\Files\\TestCase1\\test-file-plain.txt;\\Files1\\TestCase1", false)]
		public void TestIsPathValied(string relativePath, bool result)
		{
			StringBuilder sb = new StringBuilder();
			string pathToDll = GetDllPath();
			string[] rPaths = relativePath.Split(';');
			foreach (string rPath in rPaths)
				sb.Append(pathToDll + rPath + ";");

			Assert.AreEqual(result, Utils.IsPathValid(sb.ToString()));

			foreach (string rPath in rPaths)
				sb.Append(pathToDll + rPath + ",");

			Assert.AreEqual(result, Utils.IsPathValid(sb.ToString()));
		}

		[Test]
		[Row("\\Files\\TestCase7\\Test;Folder\\issue-10.txt", true)]
		[Row("\\Files\\TestCase7\\Test,Folder\\issue-10.txt", true)]
		[Row("\\Files\\TestCase1\\test-file-code.cs", true)]
		public void TestIsPathValiedWithColon(string relativePath, bool result)
		{
			string pathToDll = GetDllPath();
			relativePath = pathToDll + relativePath;

			Assert.AreEqual(result, Utils.IsPathValid(relativePath));
		}

		[Test]
		public void TestIsPathValidWithoutCollon()
		{
			StringBuilder sb = new StringBuilder();
			string pathToDll = GetDllPath();
			sb.Append(pathToDll + "\\Files\\TestCase1");

			Assert.AreEqual(Utils.IsPathValid(sb.ToString()), true);
		}

		[Test]
		public void TestMatchCount()
		{
            string pathToDll = GetDllPath();
            string filePath = pathToDll + "\\Files\\TestCase2\\test-file-plain-big.txt";
            GrepSearchResult result = new GrepSearchResult(filePath, new List<GrepSearchResult.GrepMatch>());
			List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
			matches.Add(new GrepSearchResult.GrepMatch(1, 2, 3));
			matches.Add(new GrepSearchResult.GrepMatch(1, 5, 2));
			result.SearchResults.Add(new GrepSearchResult.GrepLine(1, "test", true, matches));
            result.SearchResults.Add(new GrepSearchResult.GrepLine(2, "test2", false, null));
            result.SearchResults.Add(new GrepSearchResult.GrepLine(3, "test3", false, null));
			result.SearchResults.Add(new GrepSearchResult.GrepLine(1, "test1", false, matches));
			Assert.AreEqual(4, Utils.MatchCount(result));
			Assert.AreEqual(0, Utils.MatchCount(null));
            result = new GrepSearchResult(filePath, new List<GrepSearchResult.GrepMatch>());
			Assert.AreEqual(0, Utils.MatchCount(result));
            result = new GrepSearchResult(filePath, null);
			Assert.AreEqual(0, Utils.MatchCount(result));
		}

		[Test]
		public void TestCleanResults()
		{
			List<GrepSearchResult.GrepLine> results =  new List<GrepSearchResult.GrepLine>();
            results.Add(new GrepSearchResult.GrepLine(1, "test", true, null));
            results.Add(new GrepSearchResult.GrepLine(3, "test3", false, null));
            results.Add(new GrepSearchResult.GrepLine(2, "test2", false, null));
            results.Add(new GrepSearchResult.GrepLine(1, "test1", false, null));
			Utils.CleanResults(ref results);

			Assert.AreEqual(results.Count, 3);
			Assert.AreEqual(results[0].IsContext, false);
			Assert.AreEqual(results[0].LineNumber, 1);
			Assert.AreEqual(results[2].IsContext, false);
			Assert.AreEqual(results[2].LineNumber, 3);

			results = null;
			Utils.CleanResults(ref results);
			results = new List<GrepSearchResult.GrepLine>();
			Utils.CleanResults(ref results);
		}


        [Test]
		[Row("0.9.1", "0.9.2", true)]
		[Row("0.9.1", "0.9.2.5556", true)]
		[Row("0.9.1.5554", "0.9.1.5556", true)]
		[Row("0.9.0.5557", "0.9.1.5550", true)]
		[Row("0.9.1", "0.9.0.5556", false)]
		[Row("0.9.5.5000", "0.9.0.5556", false)]
		[Row(null, "0.9.0.5556", false)]
		[Row("0.9.5.5000", "", false)]
		[Row("0.9.5.5000", null, false)]
		[Row("xyz", "abc", false)]
		public void CompareVersions(string v1, string v2, bool result)
		{
			Assert.IsTrue(PublishedVersionExtractor.IsUpdateNeeded(v1, v2) == result);
		}

		[Test]
		public void GetLines_Returns_Correct_Line()
		{
			string text = "Hello world" + Environment.NewLine + "My tests are good" + Environment.NewLine + "How about yours?";
			List<int> lineNumbers = new List<int>();
            List<GrepSearchResult.GrepMatch> matches = new List<GrepSearchResult.GrepMatch>();
			List<string> lines = Utils.GetLines(text, 3, 2, out matches, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "Hello world");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 1);

            lines = Utils.GetLines(text, 14, 2, out matches, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "My tests are good");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 2);

            lines = Utils.GetLines(text, 3, 11, out matches, out lineNumbers);
			Assert.AreEqual(lines.Count, 2);
			Assert.AreEqual(lines[0], "Hello world");
			Assert.AreEqual(lines[1], "My tests are good");
			Assert.AreEqual(lineNumbers.Count, 2);
			Assert.AreEqual(lineNumbers[0], 1);
			Assert.AreEqual(lineNumbers[1], 2);

            lines = Utils.GetLines(text, 3, 30, out matches, out lineNumbers);
			Assert.AreEqual(lines.Count, 3);
			Assert.AreEqual(lines[0], "Hello world");
			Assert.AreEqual(lines[1], "My tests are good");
			Assert.AreEqual(lines[2], "How about yours?");
			Assert.AreEqual(lineNumbers.Count, 3);
			Assert.AreEqual(lineNumbers[0], 1);
			Assert.AreEqual(lineNumbers[1], 2);
			Assert.AreEqual(lineNumbers[2], 3);

            lines = Utils.GetLines("test", 2, 2, out matches, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "test");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 1);

            lines = Utils.GetLines("test", 0, 2, out matches, out lineNumbers);
			Assert.AreEqual(lines.Count, 1);
			Assert.AreEqual(lines[0], "test");
			Assert.AreEqual(lineNumbers.Count, 1);
			Assert.AreEqual(lineNumbers[0], 1);

            lines = Utils.GetLines("test", 10, 2, out matches, out lineNumbers);
			Assert.IsNull(lines);
			Assert.IsNull(lineNumbers);

            lines = Utils.GetLines("test", 2, 10, out matches, out lineNumbers);
			Assert.IsNull(lines);
			Assert.IsNull(lineNumbers);
		}

        [Test]
        public void GetLinesEx_Returns_Correct_Line()
        {
            string text = "Hello world" + Environment.NewLine + "My tests are good" + Environment.NewLine + "How about yours?";
            List<int> lineNumbers = new List<int>();
            List<GrepSearchResult.GrepMatch> bodyMatches = new List<GrepSearchResult.GrepMatch>();
            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>(); 
            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 3, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.AreEqual(results.Count(l=>l.IsContext == false), 1);
            Assert.AreEqual(results[0].LineText, "Hello world");
            Assert.AreEqual(results[0].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 1);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 14, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }


            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].LineText, "My tests are good");
            Assert.AreEqual(results[0].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 2);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 3, 11));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.AreEqual(results.Count, 2);
            Assert.AreEqual(results[0].LineText, "Hello world");
            Assert.AreEqual(results[1].LineText, "My tests are good");
            Assert.AreEqual(results[0].Matches.Count, 1);
            Assert.AreEqual(results[1].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 1);
            Assert.AreEqual(results[1].LineNumber, 2);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 3, 30));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.AreEqual(results.Count, 3);
            Assert.AreEqual(results[0].LineText, "Hello world");
            Assert.AreEqual(results[1].LineText, "My tests are good");
            Assert.AreEqual(results[2].LineText, "How about yours?");
            Assert.AreEqual(results[0].Matches.Count, 1);
            Assert.AreEqual(results[1].Matches.Count, 1);
            Assert.AreEqual(results[2].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 1);
            Assert.AreEqual(results[1].LineNumber, 2);
            Assert.AreEqual(results[2].LineNumber, 3);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 2, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].LineText, "test");
            Assert.AreEqual(results[0].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 1);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 0, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.AreEqual(results.Count, 1);
            Assert.AreEqual(results[0].LineText, "test");
            Assert.AreEqual(results[0].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 1);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 10, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.IsEmpty(results);

            using (StringReader reader = new StringReader("test"))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 2, 10));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.IsEmpty(results);

            using (StringReader reader = new StringReader(text))
            {
                bodyMatches.Clear();
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 3, 2));
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 6, 2));
                bodyMatches.Add(new GrepSearchResult.GrepMatch(0, 14, 2));
                results = Utils.GetLinesEx(reader, bodyMatches, 0, 0);
            }

            Assert.AreEqual(results.Count, 2);
            Assert.AreEqual(results[0].LineText, "Hello world");
            Assert.AreEqual(results[1].LineText, "My tests are good");
            Assert.AreEqual(results[0].Matches.Count, 2);
            Assert.AreEqual(results[1].Matches.Count, 1);
            Assert.AreEqual(results[0].LineNumber, 1);
            Assert.AreEqual(results[1].LineNumber, 2);            
        }

        [Test]
        public void TestMergeResultsHappyPath()
        {
            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            List<GrepSearchResult.GrepLine> context = new List<GrepSearchResult.GrepLine>();
            results.Add(new GrepSearchResult.GrepLine(3, "text3", false, null));
            results.Add(new GrepSearchResult.GrepLine(5, "text5", false, null));
            results.Add(new GrepSearchResult.GrepLine(6, "text6", false, new List<GrepSearchResult.GrepMatch>()));
            context.Add(new GrepSearchResult.GrepLine(1, "text1", true, null));
            context.Add(new GrepSearchResult.GrepLine(2, "text2", true, null));
            context.Add(new GrepSearchResult.GrepLine(3, "text3", true, null));
            Utils.MergeResults(ref results, context);
            Assert.AreEqual(5, results.Count);
            Assert.AreEqual("text1", results[0].LineText);
            Assert.AreEqual("text2", results[1].LineText);
            Assert.AreEqual(true, results[1].IsContext);
            Assert.AreEqual("text3", results[2].LineText);
            Assert.AreEqual(false, results[2].IsContext);
        }

        [Test]
        public void TestMergeResultsBorderCases()
        {
            List<GrepSearchResult.GrepLine> results = new List<GrepSearchResult.GrepLine>();
            Utils.MergeResults(ref results, null);
            Assert.AreEqual(0, results.Count);

            List<GrepSearchResult.GrepLine> context = new List<GrepSearchResult.GrepLine>();
            context.Add(new GrepSearchResult.GrepLine(1, "text1", true, null));
            context.Add(new GrepSearchResult.GrepLine(2, "text2", true, null));
            context.Add(new GrepSearchResult.GrepLine(3, "text3", true, null));

            Utils.MergeResults(ref results, context);
            Assert.AreEqual(3, results.Count);
            
            results.Add(new GrepSearchResult.GrepLine(3, "text3", false, null));
            results.Add(new GrepSearchResult.GrepLine(5, "text5", false, null));
            results.Add(new GrepSearchResult.GrepLine(6, "text6", false, new List<GrepSearchResult.GrepMatch>()));

            results = new List<GrepSearchResult.GrepLine>();
            results.Add(new GrepSearchResult.GrepLine(3, "text3", false, null));
            results.Add(new GrepSearchResult.GrepLine(5, "text5", false, null));
            results.Add(new GrepSearchResult.GrepLine(6, "text6", false, new List<GrepSearchResult.GrepMatch>()));
            Utils.MergeResults(ref results, null);
            Assert.AreEqual(3, results.Count);

            results = new List<GrepSearchResult.GrepLine>();
            results.Add(new GrepSearchResult.GrepLine(3, "text3", false, null));
            results.Add(new GrepSearchResult.GrepLine(5, "text5", false, null));
            context.Add(new GrepSearchResult.GrepLine(20, "text20", true, null));
            context.Add(new GrepSearchResult.GrepLine(30, "text30", true, null));

            Utils.MergeResults(ref results, context);
            Assert.AreEqual(6, results.Count);
            Assert.AreEqual("text30", results[5].LineText);
        }

        [Test]
        public void TestTextReaderReadLine()
        {
            string text = "Hello world" + Environment.NewLine + "My tests are good\nHow about \ryours?\n";
            int lineNumber = 0;
            using (StringReader reader = new StringReader(text))
            {
                while (reader.Peek() > 0)
                {
                    lineNumber++;
                    var line = reader.ReadLine(true);
                    if (lineNumber == 1)
                        Assert.AreEqual("Hello world" + Environment.NewLine, line);
                    if (lineNumber == 2)
                        Assert.AreEqual("My tests are good\n", line);
                    if (lineNumber == 3)
                        Assert.AreEqual("How about \r", line);
                    if (lineNumber == 4)
                        Assert.AreEqual("yours?\n", line);
                }
            }
            Assert.AreEqual(lineNumber, 4);
            text = "Hello world";
            lineNumber = 0;
            using (StringReader reader = new StringReader(text))
            {
                while (reader.Peek() > 0)
                {
                    lineNumber++;
                    var line = reader.ReadLine(true);
                    Assert.AreEqual("Hello world", line);
                }
            }
            Assert.AreEqual(lineNumber, 1);
        }

        [Test]
		[Row(null,null,2)]
		[Row("", "", 2)]
		[Row(null, ".*\\.cs", 1)]
		[Row(".*\\.txt", null, 1)]
		public void TestCopyFiles(string includePattern, string excludePattern, int numberOfFiles)
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase1", destinationFolder, includePattern, excludePattern);
			Assert.AreEqual(Directory.GetFiles(destinationFolder).Length, numberOfFiles);
		}

        [Test]
		[Row(null, null, 2)]
		public void TestCopyFilesToNonExistingFolder(string includePattern, string excludePattern, int numberOfFiles)
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase1", destinationFolder + "\\123", includePattern, excludePattern);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\123").Length, numberOfFiles);
		}

		[Test]
		public void TestCopyFilesWithSubFolders()
		{
			Utils.CopyFiles(sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", ".*", null);
			Assert.AreEqual(Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length, 4);
			Assert.IsTrue(Directory.Exists(destinationFolder + "\\TestCase3\\SubFolder"));
			Utils.DeleteFolder(destinationFolder + "\\TestCase3");
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\SubFolder\\test-file-plain-hidden.txt", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase3\\test-file-code.cs", null));
			Utils.CopyFiles(source, sourceFolder + "\\TestCase3", destinationFolder + "\\TestCase3", true);
			Assert.AreEqual(Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length, 2);
			Assert.IsTrue(Directory.Exists(destinationFolder + "\\TestCase3\\SubFolder"));			
		}

		[Test]
		public void TestCopyResults()
		{
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", null));
			Utils.CopyFiles(source, sourceFolder, destinationFolder, false);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\TestCase1").Length, 2);
			source.Add(new GrepSearchResult(sourceFolder + "\\issue-10.txt", null));
			Utils.CopyFiles(source, sourceFolder, destinationFolder, true);
			Assert.AreEqual(Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length, 3);
			try
			{
				Utils.CopyFiles(source, sourceFolder, destinationFolder, false);
				Assert.Fail("Not supposed to get here");
			}
			catch (IOException ex)
			{
				//OK
			}
			Assert.AreEqual(Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length, 3);
			Utils.CopyFiles(source, sourceFolder, destinationFolder + "\\123", false);
			Assert.AreEqual(Directory.GetFiles(destinationFolder, "*.*", SearchOption.AllDirectories).Length, 6);
		}

		[Test]
		public void TestCanCopy()
		{
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\TestCase1\\test-file-plain2.txt", null));
			Assert.IsFalse(Utils.CanCopyFiles(source, sourceFolder + "\\TestCase1"));
			Assert.IsFalse(Utils.CanCopyFiles(source, sourceFolder + "\\TestCase1\\"));
			Assert.IsTrue(Utils.CanCopyFiles(source, sourceFolder));
			Assert.IsFalse(Utils.CanCopyFiles(source, sourceFolder + "\\TestCase1\\TestCase1"));
			Assert.IsFalse(Utils.CanCopyFiles(null, null));
			Assert.IsFalse(Utils.CanCopyFiles(source, null));
			Assert.IsFalse(Utils.CanCopyFiles(null, sourceFolder));
		}

		[Test]
		public void WriteToCsvTest()
		{
			File.WriteAllText(destinationFolder + "\\test.csv", "hello");
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			List<GrepSearchResult.GrepLine> lines = new List<GrepSearchResult.GrepLine>();
            lines.Add(new GrepSearchResult.GrepLine(12, "hello", false, null));
            lines.Add(new GrepSearchResult.GrepLine(13, "world", true, null));
			List<GrepSearchResult.GrepLine> lines2 = new List<GrepSearchResult.GrepLine>();
            lines2.Add(new GrepSearchResult.GrepLine(11, "and2", true, null));
            lines2.Add(new GrepSearchResult.GrepLine(12, "hel\"lo2", false, null));
            lines2.Add(new GrepSearchResult.GrepLine(13, "world2", true, null));
            Assert.Fail();
			//source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", lines));
			//source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", lines2));
			Utils.SaveResultsAsCSV(source, destinationFolder + "\\test.csv");
			string[] stringLines = File.ReadAllLines(destinationFolder + "\\test.csv");
			Assert.AreEqual(stringLines.Length, 3, "CSV file should contain only 3 lines");
			Assert.AreEqual(stringLines[0].Split(',')[0].Trim(), "File Name");
			Assert.AreEqual(stringLines[1].Split(',')[1].Trim(), "12");
			Assert.AreEqual(stringLines[2].Split(',')[2].Trim(), "\"hel\"\"lo2\"");
		}

		[Test]
		public void DeleteFilesTest()
		{
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", null));
			Utils.CopyFiles(source, sourceFolder, destinationFolder, false);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\TestCase1\\").Length, 2);
			List<GrepSearchResult> source2 = new List<GrepSearchResult>();
			source2.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-code.cs", null));
			Utils.DeleteFiles(source2);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\TestCase1\\").Length, 1);
			source2.Add(new GrepSearchResult(destinationFolder + "\\test-file-code.cs", null));
			Utils.DeleteFiles(source2);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\TestCase1\\").Length, 1);
			source2.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-plain.txt", null));
			Utils.DeleteFiles(source2);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\TestCase1\\").Length, 0);
		}

		[Test]
		public void TestCopyFileInNonExistingFolder()
		{
			Utils.CopyFile(sourceFolder + "\\TestCase1\\test-file-code.cs", destinationFolder + "\\Test\\test-file-code2.cs", false);
			Assert.IsTrue(File.Exists(destinationFolder + "\\Test\\test-file-code2.cs"));
		}

		[Test]
		public void DeleteFolderTest()
		{
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", null));
			Utils.CopyFiles(source, sourceFolder, destinationFolder, false);
			Assert.AreEqual(Directory.GetFiles(destinationFolder + "\\TestCase1").Length, 2);
			File.SetAttributes(destinationFolder + "\\TestCase1\\test-file-code.cs", FileAttributes.ReadOnly);
			Utils.DeleteFolder(destinationFolder);
			Assert.IsFalse(Directory.Exists(destinationFolder));
		}

        [Test]
		[Row("*.*", false, true, true, 0, 0, 5)]
		[Row("*.*", false, true, false, 0, 0, 4)]
		[Row("*.*", false, true, false, 0, 40, 3)]
		[Row("*.*", false, true, false, 1, 40, 1)]
		[Row(".*\\.txt", true, true, true, 0, 0, 3)]
		[Row(".*\\.txt", true, false, true, 0, 0, 2)]
		[Row(null, true, false, true, 0, 0, 0)]
		[Row("", true, true, true, 0, 0, 0)]
		public void GetFileListTest(string namePattern, bool isRegex, bool includeSubfolders, bool includeHidden, int sizeFrom, int sizeTo, int result)
		{
			DirectoryInfo di = new DirectoryInfo(sourceFolder + "\\TestCase2\\HiddenFolder");
			di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            Assert.AreEqual(result, Utils.GetFileList(sourceFolder + "\\TestCase2", namePattern, null, isRegex, includeSubfolders, includeHidden, true, sizeFrom, sizeTo).Length);
		}

		[Test]
		public void GetFileListTestWithMultiplePaths()
		{
			string dllPath = GetDllPath();
			string path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase2\\excel-file.xls";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", "", false, false, false, true, 0, 0).Length, 4);

			path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase3\\test-file-code.cs";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", "", false, false, false, true, 0, 0).Length, 5);

			path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase2";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", "", false, false, false, true, 0, 0).Length, 5);

			path = sourceFolder + "\\TestCase2;" + sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", null, false, false, false, true, 0, 0).Length, 6);

			path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", null, false, false, false, true, 0, 0).Length, 2);

			path = sourceFolder + "\\TestCase3\\test-file-code.cs;" + sourceFolder + "\\TestCase3\\test-file-plain.txt;";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", null, false, false, false, true, 0, 0).Length, 2);

			path = sourceFolder + "\\TestCase3\\test-file-code.cs," + sourceFolder + "\\TestCase3\\test-file-plain.txt,";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", null, false, false, false, true, 0, 0).Length, 2);

			path = sourceFolder + "\\TestCase3\\test-file-code.cs," + sourceFolder + "\\TestCase3\\test-file-plain.txt";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", null, false, false, false, true, 0, 0).Length, 2);
		}

		[Test]
		public void GetFileListWithExcludes()
		{
			string dllPath = GetDllPath();
			string path = sourceFolder + "\\TestCase2";
			Assert.AreEqual(Utils.GetFileList(path, "*.*", "*.xls", false, false, false, true, 0, 0).Length, 3);
			Assert.AreEqual(Utils.GetFileList(path, "excel*.*", "*.xls", false, false, false, true, 0, 0).Length, 0);
			Assert.AreEqual(Utils.GetFileList(path, "excel*.*", "*.xs", false, false, false, true, 0, 0).Length, 1);
			Assert.AreEqual(Utils.GetFileList(path, "t[a-z]st-file-*.*", "*.cs", false, false, false, true, 0, 0).Length, 2);
			Assert.AreEqual(Utils.GetFileList(path, "t[ea]st-file-*.*", "*.cs", false, false, false, true, 0, 0).Length, 2);
		}

		[Test]
		public void GetFileListFromNonExistingFolderReturnsEmptyString()
		{
			Assert.AreEqual(Utils.GetFileList(sourceFolder + "\\NonExisting", "*.*", null, false, true, true, true, 0, 0).Length, 0);
		}

        [Test]
        public void GetFileListPerformance()
        {
            string path = sourceFolder + @"\..\..\..\..\";
            DateTime start = DateTime.Now;
            int size = Utils.GetFileList(path, "*.*", null, false, false, false, true, 0, 0).Length;
            var duration = DateTime.Now.Subtract(start).Duration().TotalMilliseconds;

            path = sourceFolder + @"..\..\..\";
            start = DateTime.Now;
            size = Utils.GetFileList(path, @".*\..*", null, true, true, true, true, 0, 0).Length;
            duration = DateTime.Now.Subtract(start).Duration().TotalMilliseconds;

            start = DateTime.Now;
            DirectoryInfo dir = new DirectoryInfo(path);
            var size2 = dir.GetFiles("*.*", SearchOption.AllDirectories).Length;
            var duration2 = DateTime.Now.Subtract(start).Duration().TotalMilliseconds;
            
            Assert.AreEqual(size, size2);
            Assert.GreaterThan(duration, duration2);
        }

        [Test]
		[Row("", 1, 1)]
		[Row("5", 0, 5)]
		[Row(" 12", 1, 12)]
		[Row("", int.MinValue, int.MinValue)]
		[Row(null, int.MinValue, int.MinValue)]
		[Row(" 22 ", int.MinValue, 22)]
		public void ParseIntTest(string text, int defaultValue, int result)
		{
			if (defaultValue != int.MinValue)
				Assert.AreEqual(Utils.ParseInt(text, defaultValue), result);
			else
				Assert.AreEqual(Utils.ParseInt(text), result);
		}

		[Test]
		public void GetReadOnlyFilesTest()
		{			
			List<GrepSearchResult> source = new List<GrepSearchResult>();
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-code.cs", null));
			source.Add(new GrepSearchResult(sourceFolder + "\\TestCase1\\test-file-plain.txt", null));

			List<GrepSearchResult> destination = new List<GrepSearchResult>();
			destination.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-code.cs", null));
			destination.Add(new GrepSearchResult(destinationFolder + "\\TestCase1\\test-file-plain.txt", null));

			Utils.CopyFiles(source, sourceFolder + "\\TestCase1", destinationFolder + "\\TestCase1", true);
			File.SetAttributes(destinationFolder + "\\TestCase1\\test-file-code.cs", FileAttributes.ReadOnly);
			Assert.AreEqual(Utils.GetReadOnlyFiles(destination).Count, 1);
			File.SetAttributes(destinationFolder + "\\TestCase1\\test-file-plain.txt", FileAttributes.ReadOnly);
			Assert.AreEqual(Utils.GetReadOnlyFiles(destination).Count, 2);

			Assert.AreEqual(Utils.GetReadOnlyFiles(null).Count, 0);
			Assert.AreEqual(Utils.GetReadOnlyFiles(new List<GrepSearchResult>()).Count, 0);
		}

		[Test]
		[Row("\\TestCase6\\test.rar", true)]
		[Row("\\TestCase6\\test_file.txt", false)]
		[Row("\\TestCase5\\big-word-document.doc", true)]
		public void TestIsBinaryFile(string file, bool isBinary)
		{
			Assert.AreEqual<bool>(Utils.IsBinary(sourceFolder + file), isBinary);
		}

        public IEnumerable<object> TestGetPaths_Source()
        {
            yield return new object[] { sourceFolder + "\\TestCase5\\big-word-document.doc", 1 };
            yield return new object[] { sourceFolder + "\\TestCase7;" + sourceFolder + "\\TestCase7", 2 };
            yield return new object[] { sourceFolder + "\\TestCase5;" + sourceFolder + "\\TestCase7", 2 };
            yield return new object[] { sourceFolder + "\\TestCase7\\Test,Folder\\;" + sourceFolder + "\\TestCase7", 2 };
            yield return new object[] { sourceFolder + "\\TestCase7\\Test;Folder\\;" + sourceFolder + "\\TestCase7", 2 };
            yield return new object[] { sourceFolder + "\\TestCase7\\Test;Folder\\;" + sourceFolder + "\\TestCase7;" + sourceFolder + "\\TestCase7\\Test;Folder\\", 3 };
            yield return new object[] { null, 0 };
            yield return new object[] { "", 0 };
        }

        [Test, Factory("TestGetPaths_Source")]
        public void TestGetPathsCount(string source, int? count)
        {
            string[] result = Utils.SplitPath(source);
            if (result == null)
                Assert.IsNull(count);
            else
                Assert.AreEqual(count, result.Length);
        }

        [Test]
        public void TestGetPathsContent()
        {
            string[] result = Utils.SplitPath(sourceFolder + "\\TestCase7\\Test;Folder\\;" + sourceFolder + "\\TestXXXX;" + sourceFolder + "\\TestCase7\\Test;Fo;lder\\;" + sourceFolder + "\\TestCase7\\Test,Folder\\;");
            Assert.AreEqual(sourceFolder + "\\TestCase7\\Test;Folder\\", result[0]);
            Assert.AreEqual(sourceFolder + "\\TestXXXX", result[1]);
            Assert.AreEqual(sourceFolder + "\\TestCase7\\Test;Fo;lder\\", result[2]);
            Assert.AreEqual(sourceFolder + "\\TestCase7\\Test,Folder\\", result[3]);
        }

        [Test]
        public void TestTrimEndOfString()
        {
            string text = "test\r\n";
            Assert.AreEqual("test", text.TrimEndOfLine());
            text = "test\r";
            Assert.AreEqual("test", text.TrimEndOfLine());
            text = "test\n";
            Assert.AreEqual("test", text.TrimEndOfLine());
            text = "test";
            Assert.AreEqual("test", text.TrimEndOfLine());
            text = "";
            Assert.AreEqual("", text.TrimEndOfLine());
        }
	}
}
