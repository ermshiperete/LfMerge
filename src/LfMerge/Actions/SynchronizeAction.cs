﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Reflection;
using Autofac;
using LibFLExBridgeChorusPlugin.Infrastructure;
using LibTriboroughBridgeChorusPlugin;
using LfMerge.Settings;
using Chorus.sync;
using Chorus.VcsDrivers;
using Palaso.Progress;
using Palaso.Network;

namespace LfMerge.Actions
{
	public class SynchronizeAction: Action
	{
		public SynchronizeAction(LfMergeSettingsIni settings, LfMerge.Logging.ILogger logger) : base(settings, logger) {}

		protected override ProcessingState.SendReceiveStates StateForCurrentAction
		{
			get { return ProcessingState.SendReceiveStates.RECEIVING; }
		}

		protected override void DoRun(ILfProject project)
		{
			using (var scope = MainClass.Container.BeginLifetimeScope())
			{
				GetAction(ActionNames.TransferMongoToFdo).Run(project);
				LanguageForgeProject.DisposeProjectCache(project.ProjectCode);

				// Syncing of a new repo is not currently supported.
				// For implementation, look in ~/fwrepo/flexbridge/src/FLEx-ChorusPlugin/Infrastructure/ActionHandlers/SendReceiveActionHandler.cs
				Logger.Notice("Syncing");
				string applicationName = Assembly.GetExecutingAssembly().GetName().Name;
				string applicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				var repoPath = RepositoryAddress.Create("Language Depot", getSyncUri(project));
				var syncOptions = new SyncOptions();
				syncOptions.CheckinDescription = "[" + applicationName  + ": " + applicationVersion + "] sync";
				syncOptions.RepositorySourcesToTry.Add(repoPath);
				string projectFolderPath = Path.Combine(Settings.WebWorkDirectory, project.ProjectCode);
				var projectConfig = new ProjectFolderConfiguration(projectFolderPath);
				FlexFolderSystem.ConfigureChorusProjectFolder(projectConfig);
				var synchroniser = Synchronizer.FromProjectConfiguration(projectConfig, Progress);
				string fwdataPathname = Path.Combine(projectFolderPath, project.ProjectCode + SharedConstants.FwXmlExtension);
				synchroniser.SynchronizerAdjunct = new LfMergeSychronizerAdjunct(fwdataPathname, MagicStrings.FwFixitAppName, true); // Settings.VerboseProgress);
				SyncResults syncResult = synchroniser.SyncNow(syncOptions);
				if (!syncResult.Succeeded)
				{
					Logger.Error("Sync failed - {0}", syncResult.ErrorEncountered);
					return;
				}
				if (syncResult.DidGetChangesFromOthers)
					Logger.Notice("Received changes from others");
				else
					Logger.Notice("No changes from others");

				GetAction(ActionNames.TransferFdoToMongo).Run(project);
			}
		}

		protected override ActionNames NextActionName
		{
			get { return ActionNames.None; }
		}

		private string getSyncUri(ILfProject project)
		{
			string uri = project.LanguageDepotProjectUri;
			string serverPath = uri.StartsWith("http://") ? uri.Replace("http://", "") : uri;
			return "http://" +
				HttpUtilityFromMono.UrlEncode(project.LanguageDepotProject.Username) + ":" +
				HttpUtilityFromMono.UrlEncode(project.LanguageDepotProject.Password) + "@" + serverPath + "/" +
				HttpUtilityFromMono.UrlEncode(project.LanguageDepotProject.Identifier);
		}

	}
}
