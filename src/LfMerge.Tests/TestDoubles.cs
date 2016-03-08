﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Chorus.Model;
using IniParser.Model;
using LfMerge.LanguageForge.Model;
using LfMerge.LanguageForge.Config;
using LfMerge.FieldWorks;
using LfMerge.MongoConnector;
using LfMerge.Settings;
using LibFLExBridgeChorusPlugin.Infrastructure;
using LibTriboroughBridgeChorusPlugin.Infrastructure;
using Palaso.Progress;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Moq;

namespace LfMerge.Tests
{
	public class ProcessingStateFactoryDouble: IProcessingStateDeserialize
	{
		public ProcessingStateDouble State { get; set; }
		private LfMergeSettingsIni Settings { get; set; }

		public ProcessingStateFactoryDouble(LfMergeSettingsIni settings)
		{
			Settings = settings;
		}

		#region IProcessingStateDeserialize implementation
		public ProcessingState Deserialize(string projectCode)
		{
			if (State == null)
				State = new ProcessingStateDouble(projectCode, Settings);
			return State;
		}
		#endregion
	}

	public class ProcessingStateDouble: ProcessingState
	{
		public List<ProcessingState.SendReceiveStates> SavedStates;

		public ProcessingStateDouble(string projectCode, LfMergeSettingsIni settings): base(projectCode, settings)
		{
			SavedStates = new List<ProcessingState.SendReceiveStates>();
		}

		protected override void SetProperty<T>(ref T property, T value)
		{
			property = value;

			if (SavedStates.Count == 0 || SavedStates[SavedStates.Count - 1] != SRState)
				SavedStates.Add(SRState);
		}

		public void ResetSavedStates()
		{
			SavedStates.Clear();
		}
	}

	public class LanguageForgeProjectAccessor: LanguageForgeProject
	{
		protected LanguageForgeProjectAccessor(LfMergeSettingsIni settings): base(settings, null)
		{
		}

		public static void Reset()
		{
			LanguageForgeProject.DisposeProjectCache();
		}
	}

	public class LfMergeSettingsDouble: LfMergeSettingsIni
	{
		static LfMergeSettingsDouble()
		{
			ConfigDir = Path.GetRandomFileName();
		}

		public LfMergeSettingsDouble(string replacementBaseDir) : base()
		{
			var replacementConfig = new IniData(ParsedConfig);
			replacementConfig.Global["BaseDir"] = replacementBaseDir;
			Initialize(replacementConfig);
			CommitWhenDone = false;
		}
	}

	public class MongoConnectionDouble: IMongoConnection
	{
		public static void Initialize()
		{
			// Just as with MongoConnection.Initialize(), we need to set up BSON serialization conventions
			// so that the "fake" connection can deserialize the sample JSON identically to how the real DB does it.
			Console.WriteLine("Initializing FAKE Mongo connection...");

			// Serialize Boolean values permissively
			BsonSerializer.RegisterSerializationProvider(new BooleanSerializationProvider());

			// Use CamelCaseName conversions between Mongo and our mapping classes
			var pack = new ConventionPack();
			pack.Add(new CamelCaseElementNameConvention());
			ConventionRegistry.Register(
				"My Custom Conventions",
				pack,
				t => t.FullName.StartsWith("LfMerge."));

			// Register class mappings before opening first connection
			new LfMerge.LanguageForge.Config.MongoRegistrarForLfConfig().RegisterClassMappings();
		}

		private Dictionary<string, LfInputSystemRecord> _storedInputSystems = new Dictionary<string, LfInputSystemRecord>();
		private Dictionary<Guid, LfLexEntry> _storedLfLexEntries = new Dictionary<Guid, LfLexEntry>();
		private Dictionary<string, LfOptionList> _storedLfOptionLists = new Dictionary<string, LfOptionList>();

		// For use in unit tests that want to verify what was placed into Mongo
		public Dictionary<string, LfInputSystemRecord> StoredInputSystems { get { return _storedInputSystems; } }
		public Dictionary<Guid, LfLexEntry> StoredLfLexEntries { get { return _storedLfLexEntries; } }
		public Dictionary<string, LfOptionList> StoredLfOptionLists { get { return _storedLfOptionLists; } }

		public void Reset()
		{
			_storedInputSystems.Clear();
			_storedLfLexEntries.Clear();
			_storedLfOptionLists.Clear();
		}

		public Dictionary<string, LfInputSystemRecord> GetInputSystems(ILfProject project)
		{
			return StoredInputSystems;
		}

		public bool SetInputSystems(ILfProject project, Dictionary<string, LfInputSystemRecord> inputSystems, bool initialClone = false, string vernacularWs = "", string analysisWs = "")
		{
			foreach (var ws in inputSystems.Keys)
				_storedInputSystems[ws] = inputSystems[ws];

			if (initialClone)
			{
				// TODO: Update field input systems too?
			}
			return true;
		}

		public void UpdateMockLfLexEntry(BsonDocument mockData)
		{
			LfLexEntry data = BsonSerializer.Deserialize<LfLexEntry>(mockData);
			UpdateMockLfLexEntry(data);
		}

		public void UpdateMockLfLexEntry(LfLexEntry mockData)
		{
			Guid guid = mockData.Guid ?? Guid.Empty;
			_storedLfLexEntries[guid] = mockData;
		}

		public void UpdateMockOptionList(BsonDocument mockData)
		{
			LfOptionList data = BsonSerializer.Deserialize<LfOptionList>(mockData);
			UpdateMockOptionList(data);
		}

		public void UpdateMockOptionList(LfOptionList mockData)
		{
			string listCode = mockData.Code ?? string.Empty;
			_storedLfOptionLists[listCode] = mockData;
		}

		public IEnumerable<LfLexEntry> GetLfLexEntries()
		{
			return _storedLfLexEntries.Values;
		}

		public IEnumerable<LfOptionList> GetLfOptionLists()
		{
			return _storedLfOptionLists.Values;
		}

		public IEnumerable<TDocument> GetRecords<TDocument>(ILfProject project, string collectionName)
		{
			switch (collectionName)
			{
			case MagicStrings.LfCollectionNameForLexicon:
				return (IEnumerable<TDocument>)GetLfLexEntries();
			case MagicStrings.LfCollectionNameForOptionLists:
				return (IEnumerable<TDocument>)GetLfOptionLists();
			default:
				List<TDocument> empty = new List<TDocument>();
				return empty.AsEnumerable();
			}
		}

		public LfOptionList GetLfOptionListByCode(ILfProject project, string listCode)
		{
			LfOptionList result;
			if (!_storedLfOptionLists.TryGetValue(listCode, out result))
				result = null;
			return result;
		}

		public IMongoDatabase GetProjectDatabase(ILfProject project)
		{
			var mockDb = new Mock<IMongoDatabase>(); // SO much easier than implementing the 9 public methods for a manual stub of IMongoDatabase!
			// TODO: Add appropriate mock functions if needed
			return mockDb as IMongoDatabase;
		}

		public IMongoDatabase GetMainDatabase()
		{
			var mockDb = new Mock<IMongoDatabase>(); // SO much easier than implementing the 9 public methods for a manual stub of IMongoDatabase!
			// TODO: Add appropriate mock functions if needed
			return mockDb as IMongoDatabase;
		}

		public bool UpdateRecord(ILfProject project, LfLexEntry data)
		{
			_storedLfLexEntries[data.Guid ?? Guid.Empty] = data;
			return true;
		}

		public bool UpdateRecord(ILfProject project, LfOptionList data, string listCode)
		{
			_storedLfOptionLists[listCode ?? string.Empty] = data;
			return true;
		}

		public bool RemoveRecord(ILfProject project, Guid guid)
		{
			_storedLfLexEntries.Remove(guid);
			return true;
		}
	}

	public class MongoProjectRecordFactoryDouble: MongoProjectRecordFactory
	{
		public MongoProjectRecordFactoryDouble(IMongoConnection connection) : base(connection)
		{
		}

		public override MongoProjectRecord Create(ILfProject project)
		{
			var sampleConfig = BsonSerializer.Deserialize<LfProjectConfig>(SampleData.jsonConfigData);

			// TODO: Could we use a Mock to do this instead?
			return new MongoProjectRecord {
				Id = new ObjectId(),
				InputSystems = new Dictionary<string, LfInputSystemRecord>() {
					{"en", new LfInputSystemRecord {
							Abbreviation = "en",
							Tag = "en",
							LanguageName = "English",
							IsRightToLeft = false } },
					{"fr", new LfInputSystemRecord {
							Abbreviation = "fr",
							Tag = "fr",
							LanguageName = "French",
							IsRightToLeft = false } },
				},
				InterfaceLanguageCode = "en",
				LanguageCode = "fr",
				ProjectCode = project.LfProjectCode,
				ProjectName = project.FwProjectCode,
				Config = sampleConfig
			};
		}
	}

	class LanguageDepotProjectDouble: ILanguageDepotProject
	{
		#region ILanguageDepotProject implementation
		public void Initialize(string lfProjectCode)
		{
			Identifier = lfProjectCode;
		}

		public string Username { get; set; }
		public string Password { get; set; }
		public string Identifier { get; set; }
		public string Repository { get; set; }
		#endregion
	}

	class InternetCloneSettingsModelDouble: InternetCloneSettingsModel
	{
		public override void DoClone()
		{
			Directory.CreateDirectory(TargetDestination);
			Directory.CreateDirectory(Path.Combine(TargetDestination, ".hg"));
			File.WriteAllText(Path.Combine(TargetDestination, ".hg", "hgrc"), "blablabla");
		}
	}

	class UpdateBranchHelperFlexDouble: UpdateBranchHelperFlex
	{
		public override bool UpdateToTheCorrectBranchHeadIfPossible(string desiredBranchName,
			ActualCloneResult cloneResult, string cloneLocation)
		{
			cloneResult.FinalCloneResult = FinalCloneResult.Cloned;
			return true;
		}
	}

	class FlexHelperDouble: FlexHelper
	{
		public override void PutHumptyTogetherAgain(IProgress progress, bool verbose, string mainFilePathname)
		{
		}
	}
}
