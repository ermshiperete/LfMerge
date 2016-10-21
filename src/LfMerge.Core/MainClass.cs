// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using LfMerge.Core.Actions.Infrastructure;
using LfMerge.Core.LanguageForge.Infrastructure;
using LfMerge.Core.Logging;
using LfMerge.Core.MongoConnector;
using LfMerge.Core.Queues;
using LfMerge.Core.Reporting;
using LfMerge.Core.Settings;
using Palaso.IO;
using Palaso.Progress;
using SIL.FieldWorks.FDO;

namespace LfMerge.Core
{
	public static class MainClass
	{
		public static IContainer Container { get; internal set; }
		public static ILogger Logger { get; set; }

		static MainClass()
		{
			if (Container == null)
				Container = RegisterTypes().Build();

			Logger = Container.Resolve<ILogger>();
		}

		internal static ContainerBuilder RegisterTypes()
		{
			string programName = null;
			var assembly = Assembly.GetEntryAssembly(); // will be null when running unit tests in MD
			if (assembly != null)
			{
				var attributes = assembly.GetCustomAttributes<AssemblyTitleAttribute>().ToArray();
				if (attributes.Length > 0)
					programName = attributes[0].Title;
			}
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterType<LfMergeSettings>().SingleInstance().AsSelf();
			containerBuilder.RegisterType<SyslogLogger>().SingleInstance().As<ILogger>()
				.WithParameter(new TypedParameter(typeof(string), programName));
			containerBuilder.RegisterType<LanguageDepotProject>().As<ILanguageDepotProject>();
			containerBuilder.RegisterType<ProcessingState.Factory>().As<IProcessingStateDeserialize>();
			containerBuilder.RegisterType<ChorusHelper>().SingleInstance().AsSelf();
			containerBuilder.RegisterType<MongoConnection>().SingleInstance().As<IMongoConnection>().ExternallyOwned();
			containerBuilder.RegisterType<MongoProjectRecordFactory>().AsSelf();
			containerBuilder.RegisterType<EntryCounts>().AsSelf();
			containerBuilder.RegisterType<SyslogProgress>().As<IProgress>();
			containerBuilder.RegisterType<LanguageForgeProxy>().As<ILanguageForgeProxy>();
			Actions.Action.Register(containerBuilder);
			Queue.Register(containerBuilder);
			return containerBuilder;
		}

		public static string GetModelSpecificDirectory(string modelVersion)
		{
			var dir = FileLocator.DirectoryOfTheApplicationExecutable;
			if (dir.IndexOf(FdoCache.ModelVersion, StringComparison.Ordinal) >= 0)
				return dir.Replace(FdoCache.ModelVersion, modelVersion);

			// fall back: append model version. This at least prevents an infinite loop
			return Path.Combine(dir, modelVersion);
		}

		public static string ModelVersion
		{
			// We're exposing the model version as property so that LfMerge doesn't need a
			// reference to FDO. This simplifies dealing with multiple model versions.
			get { return FdoCache.ModelVersion; }
		}

		public static bool CheckSetup()
		{
			var settings = MainClass.Container.Resolve<LfMergeSettings>();
			var homeFolder = Environment.GetEnvironmentVariable("HOME") ?? "/var/www";
			string[] folderPaths = { Path.Combine(homeFolder, ".local"),
				Path.GetDirectoryName(settings.WebWorkDirectory) };
			foreach (string folderPath in folderPaths)
			{
				if (!Directory.Exists(folderPath))
				{
					MainClass.Logger.Notice("Folder '{0}' doesn't exist", folderPath);
					return false;
				}
			}

			return true;
		}

	}
}

