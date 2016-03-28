﻿// Copyright (c) 2011-2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System.Collections.Generic;
using Autofac;
using NUnit.Framework;
using LfMerge.Actions;
using LfMerge.Queues;

namespace LfMerge.Tests.Actions
{
	[TestFixture]
	public class ActionTests
	{
		private TestEnvironment _env;

		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			// Force setting of Options.Current
			new Options();
		}

		private ProcessingStateDouble ProcessState
		{
			get
			{
				var factory = MainClass.Container.Resolve<ProcessingStateFactoryDouble>();
				return factory.State;
			}
		}

		private ProcessingStateFactoryDouble Factory
		{
			get
			{
				return MainClass.Container.Resolve<ProcessingStateFactoryDouble>();
			}
		}

		[SetUp]
		public void Setup()
		{
			_env = new TestEnvironment();
		}

		[TearDown]
		public void TearDown()
		{
			_env.Dispose();
		}

		[TestCase(QueueNames.Edit, new[] { ActionNames.Edit })]
		[TestCase(QueueNames.Synchronize, new[] { ActionNames.Synchronize })]
		public void NextAction(QueueNames queueName, ActionNames[] expectedActionNames)
		{
			var actions = new List<ActionNames>();
			for (var sut = Queue.GetQueue(queueName).CurrentAction;
				sut != null;
				sut = sut.NextAction)
			{
				actions.Add(sut.Name);
			}

			Assert.That(actions, Is.EquivalentTo(expectedActionNames));
		}

		[TestCase(ActionNames.TransferMongoToFdo, ProcessingState.SendReceiveStates.SYNCING)]
		[TestCase(ActionNames.Commit, ProcessingState.SendReceiveStates.SYNCING)]
		[TestCase(ActionNames.Edit, ProcessingState.SendReceiveStates.SYNCING)]
		[TestCase(ActionNames.TransferFdoToMongo, ProcessingState.SendReceiveStates.SYNCING)]
		public void State(ActionNames actionName, ProcessingState.SendReceiveStates expectedState)
		{
			// Setup
			var lfProj = LanguageForgeProject.Create(_env.Settings, "proja");
			var sut = Action.GetAction(actionName);

			// Exercise
			sut.Run(lfProj);

			// Verify
			Assert.That(ProcessState.SavedStates, Is.EqualTo(new[] { expectedState }));
			Assert.That(lfProj.State.SRState, Is.EqualTo(expectedState));
		}

		[TestCase(ActionNames.TransferMongoToFdo)]
		[TestCase(ActionNames.Commit)]
		[TestCase(ActionNames.Synchronize)]
		[TestCase(ActionNames.Edit)]
		[TestCase(ActionNames.TransferFdoToMongo)]
		public void State_SkipsHoldState(ActionNames actionName)
		{
			// Setup
			var lfProj = LanguageForgeProject.Create(_env.Settings, "proja");
			var state = Factory.Deserialize("proja") as ProcessingStateDouble;
			state.SRState = ProcessingState.SendReceiveStates.HOLD;
			state.ResetSavedStates();
			Factory.State = state;
			var sut = Action.GetAction(actionName);

			// Exercise
			sut.Run(lfProj);

			// Verify
			Assert.That(ProcessState.SavedStates, Is.Empty);
		}
	}
}
