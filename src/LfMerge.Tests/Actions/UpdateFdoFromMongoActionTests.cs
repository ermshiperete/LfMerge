﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using Autofac;
using LfMerge.Actions;
using LfMerge.MongoConnector;
using LfMerge.Tests;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using System;

namespace LfMerge.Tests.Actions
{
	public class UpdateFdoFromMongoActionTests
	{
		public const string testProjectCode = "TestLangProj";
		private TestEnvironment _env;
		private MongoConnectionDouble _conn;
		private MongoProjectRecordFactory _recordFactory;
		private UpdateFdoFromMongoDbAction sut;

		[SetUp]
		public void Setup()
		{
			//_env = new TestEnvironment();
			_env = new TestEnvironment(testProjectCode: testProjectCode);
			_conn = MainClass.Container.Resolve<IMongoConnection>() as MongoConnectionDouble;
			if (_conn == null)
				throw new AssertionException("Fdo->Mongo tests need a mock MongoConnection in order to work.");
			_recordFactory = MainClass.Container.Resolve<MongoProjectRecordFactory>() as MongoProjectRecordFactoryDouble;
			if (_recordFactory == null)
				throw new AssertionException("Fdo->Mongo tests need a mock MongoProjectRecordFactory in order to work.");
			// TODO: If creating our own Mocks would be better than getting them from Autofac, do that instead.

			sut = new UpdateFdoFromMongoDbAction(
				_env.Settings,
				_env.Logger,
				_conn,
				_recordFactory
			);
		}

		[TearDown]
		public void TearDown()
		{
			_env.Dispose();
		}

		[Test]
		public void Action_Should_UpdateDefinitions()
		{
			// Setup
			var lfProj = LanguageForgeProject.Create(_env.Settings, testProjectCode);
			var data = new SampleData();
			string newDefinition = "New definition for this unit test";
			data.bsonTestData["senses"][0]["definition"]["en"]["value"] = newDefinition;

			_conn.AddToMockData(data.bsonTestData);

			// Exercise
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			sut.Run(lfProj);
			stopwatch.Stop();
			Console.WriteLine("Running test took {0} ms", stopwatch.ElapsedMilliseconds);

			// Verify
			stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			FdoCache cache = lfProj.FieldWorksProject.Cache;
			stopwatch.Stop();
			Console.WriteLine("Creating cache after running test took {0} ms", stopwatch.ElapsedMilliseconds);
			// TODO: Get expected data programmatically from SampleData instead of hardcoding it here
			string expectedGuidStr = "1a705846-a814-4289-8594-4b874faca6cc";
			string expectedShortName = "ztestmain";
			Guid expectedGuid = Guid.Parse(expectedGuidStr);

			var entry = cache.ServiceLocator.GetObject(expectedGuid) as ILexEntry;
			Assert.IsNotNull(entry);
			Assert.That(entry.Guid, Is.EqualTo(expectedGuid));
			Assert.That(entry.ShortName, Is.EqualTo(expectedShortName));
			Assert.That(entry.SensesOS[0].DefinitionOrGloss.BestAnalysisAlternative.Text, Is.EqualTo(newDefinition));
		}
	}
}

