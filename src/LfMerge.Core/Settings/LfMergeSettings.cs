﻿// Copyright (c) 2016-2018 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Text;
using IniParser;
using IniParser.Exceptions;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
using LfMerge.Core.Queues;
using SIL.LCModel;

namespace LfMerge.Core.Settings
{
	public class LfMergeSettings
	{
		public static string ConfigDir { get; set; }

		public static string ConfigFile {
			get { return Path.Combine(ConfigDir, "sendreceive.conf"); }
		}

		static LfMergeSettings()
		{
			if (string.IsNullOrEmpty(ConfigDir))
				ConfigDir = "/etc/languageforge/conf/";
		}

		public LfMergeSettings()
		{
			LcmDirectorySettings = new LcmDirectories();

			// Save parsed config for easier persisting in SaveSettings()
			ParsedConfig = this.ParseFiles(DefaultLfMergeSettings.DefaultIniText, ConfigFile);
			Initialize(ParsedConfig);
		}

		protected IniData ParsedConfig { get; set; }

		public void Initialize(IniData parsedConfig)
		{
//			if (Current != null)
//				return;

			KeyDataCollection main = parsedConfig.Global ?? new KeyDataCollection();
			string baseDir = main["BaseDir"] ?? "/var/lib/languageforge/lexicon/sendreceive";
			string webworkDir = main["WebworkDir"] ?? "webwork";
			string templatesDir = main["TemplatesDir"] ?? "Templates";
			string mongoHostname = main["MongoHostname"] ?? "localhost";
			string mongoPort = main["MongoPort"] ?? "27017";
			int mongoPortAsInt;
			if (Int32.TryParse(mongoPort, out mongoPortAsInt))
				{ } // No need to do anything if Mongo port parses correctly as an int
			else
				mongoPortAsInt = 27017; // Default to Mongo's default port if settings value can't parse
			string mongoMainDatabaseName = main["MongoMainDatabaseName"] ?? "scriptureforge";
			string mongoDatabaseNamePrefix = main["MongoDatabaseNamePrefix"] ?? "sf_";
			string verboseProgress = main["VerboseProgress"] ?? "";
			string phpSourcePath = main["PhpSourcePath"] ?? "/var/www/languageforge.org/htdocs";

			SetAllMembers(baseDir, webworkDir, templatesDir, mongoHostname, mongoPortAsInt,
				mongoDatabaseNamePrefix, mongoMainDatabaseName, verboseProgress, phpSourcePath);

			LanguageDepotRepoUri = main["LanguageDepotRepoUri"]; // optional

			// TODO: Should this CreateDirectories() call live somewhere else?
			Queue.CreateQueueDirectories(this);
		}

		private string[] QueueDirectories { get; set; }

		private void SetAllMembers(string baseDir, string webworkDir, string templatesDir,
			string mongoHostname, int mongoPort, string mongoDatabaseNamePrefix,
			string mongoMainDatabaseName, string verboseProgress, string phpSourcePath)
		{
			LcmDirectorySettings.SetProjectsDirectory(Path.IsPathRooted(webworkDir) ? webworkDir : Path.Combine(baseDir, webworkDir));
			LcmDirectorySettings.SetTemplateDirectory(Path.IsPathRooted(templatesDir) ? templatesDir : Path.Combine(baseDir, templatesDir));
			StateDirectory = Path.Combine(baseDir, "state");

			CommitWhenDone = true;
			VerboseProgress = LanguageForge.Model.ParseBoolean.FromString(verboseProgress);

			var queueCount = Enum.GetValues(typeof(QueueNames)).Length;
			QueueDirectories = new string[queueCount];
			QueueDirectories[(int)QueueNames.None] = null;
			QueueDirectories[(int)QueueNames.Edit] = Path.Combine(baseDir, "editqueue");
			QueueDirectories[(int)QueueNames.Synchronize] = Path.Combine(baseDir, "syncqueue");

			MongoDatabaseNamePrefix = mongoDatabaseNamePrefix;
			MongoDbHostNameAndPort = String.Format("{0}:{1}", mongoHostname, mongoPort.ToString());
			MongoDbHostName = mongoHostname;
			MongoDbPort = mongoPort;
			MongoMainDatabaseName = mongoMainDatabaseName;

			PhpSourcePath = phpSourcePath;
		}

		public bool CommitWhenDone { get; internal set; }

		public bool VerboseProgress { get; protected set; }

		public string PhpSourcePath { get; protected set; }

		public string LanguageDepotRepoUri { get; protected set; }

		#region Equality and GetHashCode

		public override bool Equals(object obj)
		{
			var other = obj as LfMergeSettings;
			if (other == null)
				return false;
			bool ret =
				other.CommitWhenDone == CommitWhenDone &&
				other.LcmDirectorySettings.DefaultProjectsDirectory == LcmDirectorySettings.DefaultProjectsDirectory &&
				other.MongoDatabaseNamePrefix == MongoDatabaseNamePrefix &&
				other.MongoDbHostNameAndPort == MongoDbHostNameAndPort &&
				other.MongoDbHostName == MongoDbHostName &&
				other.MongoDbPort == MongoDbPort &&
				other.MongoMainDatabaseName == MongoMainDatabaseName &&
				other.LcmDirectorySettings.ProjectsDirectory == LcmDirectorySettings.ProjectsDirectory &&
				other.StateDirectory == StateDirectory &&
				other.LcmDirectorySettings.TemplateDirectory == LcmDirectorySettings.TemplateDirectory &&
				other.VerboseProgress == VerboseProgress &&
				other.WebWorkDirectory == WebWorkDirectory;
			foreach (QueueNames queueName in Enum.GetValues(typeof(QueueNames)))
			{
				ret = ret && other.GetQueueDirectory(queueName) == GetQueueDirectory(queueName);
			}
			return ret;
		}

		public override int GetHashCode()
		{
			var hash = CommitWhenDone.GetHashCode() ^
				LcmDirectorySettings.DefaultProjectsDirectory.GetHashCode() ^
				MongoDatabaseNamePrefix.GetHashCode() ^
				MongoDbHostNameAndPort.GetHashCode() ^
				MongoDbHostName.GetHashCode() ^
				MongoDbPort.GetHashCode() ^
				MongoMainDatabaseName.GetHashCode() ^
				LcmDirectorySettings.ProjectsDirectory.GetHashCode() ^
				StateDirectory.GetHashCode() ^
				LcmDirectorySettings.TemplateDirectory.GetHashCode() ^
				VerboseProgress.GetHashCode() ^
				WebWorkDirectory.GetHashCode();
			foreach (QueueNames queueName in Enum.GetValues(typeof(QueueNames)))
			{
				var dir = GetQueueDirectory(queueName);
				if (dir != null)
					hash ^= dir.GetHashCode();
			}
			return hash;
		}

		#endregion

		public LcmDirectories LcmDirectorySettings { get; private set; }

		public class LcmDirectories: ILcmDirectories
		{
			public void SetProjectsDirectory(string value)
			{
				ProjectsDirectory = value;
			}

			public void SetTemplateDirectory(string value)
			{
				TemplateDirectory = value;
			}

			#region ILcmDirectories implementation

			public string ProjectsDirectory { get; private set; }

			public string DefaultProjectsDirectory {
				get { return ProjectsDirectory; }
			}

			public string TemplateDirectory { get; private set; }

			#endregion
		}

		public string StateDirectory { get; private set; }

		public string LockFile
		{
			get
			{
				var path = "/var/run";
				const string filename = "lfmerge.pid";

				var attributes = File.GetAttributes(path);
				if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
				{
					// XDG_RUNTIME_DIR is /run/user/<userid>, and /var/run is symlink'ed to /run. See http://serverfault.com/a/727994/246397
					path = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") ?? "/tmp/run";
					if (!Directory.Exists(path)) {
						Directory.CreateDirectory(path);
					}
				}

				return Path.Combine(path, filename);
			}
		}

		public string GetQueueDirectory(QueueNames queue)
		{
			return QueueDirectories[(int)queue];
		}

		public string WebWorkDirectory { get { return LcmDirectorySettings.ProjectsDirectory; } }

		/// <summary>
		/// Gets the name of the state file. If necessary the state directory is also created.
		/// </summary>
		/// <returns>The state file name.</returns>
		/// <param name="projectCode">Project code.</param>
		public string GetStateFileName(string projectCode)
		{
			Directory.CreateDirectory(StateDirectory);
			return Path.Combine(StateDirectory, projectCode + ".state");
		}

		public string MongoDbHostNameAndPort { get; private set; }

		public string MongoDbHostName { get; private set; }

		public int MongoDbPort { get; private set; }

		public string MongoMainDatabaseName { get; private set; }

		/// <summary>
		/// The prefix prepended to project codes to get the Mongo database name.
		/// </summary>
		public string MongoDatabaseNamePrefix { get; private set; }

		#region Serialization/Deserialization

		// we don't call this method from our production code since LfMerge doesn't directly
		// change the option.
		public void SaveSettings()
		{
			SaveSettings(ConfigFile);
		}

		private static IniParserConfiguration CreateIniParserConfiguration()
		{
			return new IniParserConfiguration {
				// ThrowExceptionsOnError = false,
				CommentString = "#",
				SkipInvalidLines = true
			};
		}

		public void SaveSettings(string fileName)
		{
			if (ParsedConfig == null)
				ParsedConfig = new IniData();
			// Note that this will persist the merged global & user configurations, not the original global config.
			// Since we don't call this method from production code, this is not an issue.
			var parserConfig = CreateIniParserConfiguration();
			var parser = new IniDataParser(parserConfig);
			var fileParser = new FileIniDataParser(parser);
			var utf8 = new UTF8Encoding(false);
			fileParser.WriteFile(fileName, ParsedConfig, utf8);
		}

		public virtual IniData ParseFiles(string defaultConfig, string globalConfigFilename)
		{
			var utf8 = new UTF8Encoding(false);
			var parserConfig = CreateIniParserConfiguration();

			var parser = new IniDataParser(parserConfig);
			IniData result = parser.Parse(DefaultLfMergeSettings.DefaultIniText);

			string globalIni = File.Exists(globalConfigFilename) ? File.ReadAllText(globalConfigFilename, utf8) : "";
			if (String.IsNullOrEmpty(globalIni))
			{
				// Can't use the log yet, so report warnings to Console.WriteLine
				Console.WriteLine("LfMerge: Warning: no global configuration found. Will use default settings.");
			}
			IniData globalConfig;
			try
			{
				globalConfig = parser.Parse(globalIni);
			}
			catch (ParsingException e)
			{
				Console.WriteLine("LfMerge: Warning: Error parsing global configuration file. Will use default settings.");
				Console.WriteLine("Error follows: {0}", e.ToString());
				globalConfig = null; // Merging null is perfectly acceptable to IniParser
			}
			result.Merge(globalConfig);

			foreach (KeyData item in result.Global)
			{
				// Special-case. Could be replaced with a more general regex if we end up using more variables, but YAGNI.
				if (item.Value.Contains("${HOME}"))
				{
					item.Value = item.Value.Replace("${HOME}", Environment.GetEnvironmentVariable("HOME"));
				}
			}

			return result;
		}

		#endregion

	}
}

