// Copyright (c) 2011-2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

using CommandLine;
using NUnit.Framework;

namespace LfMerge.Tests
{
	[TestFixture]
	public class OptionsTests
	{
		[OneTimeSetUp]
		public void FixtureSetUp()
		{
			Options.ParserInstance = new Parser(with => with.HelpWriter = null);
		}

		[TestCase(new string[0], null, TestName = "No arguments")]
		[TestCase(new[] { "-p", "ProjA" }, "ProjA", TestName = "Prio project specified")]
		public void ParseArgs(string[] args,string expectedPrioProj)
		{
			var sut = Options.ParseCommandLineArgs(args);

			// Verify
			Assert.That(sut.ProjectCode, Is.EqualTo(expectedPrioProj));
		}

		[TestCase(new[] {"-q", "NotAQueue"}, null, TestName = "Invalid args")]
		public void ParseInvalidArgs(string[] args, string expectedOptions)
		{
			var sut = Options.ParseCommandLineArgs(args);

			// Verify
			Assert.That(sut, Is.EqualTo(expectedOptions));
		}

		[TestCase(new string[0],
			null, TestName = "No arguments")]
		[TestCase(new[] { "-p", "ProjA" },
			"ProjA", TestName = "Prio project specified")]
		public void FirstProjectAndStopAfterFirstProject(string[] args,
			string expectedFirstProject)
		{
			var sut = Options.ParseCommandLineArgs(args);

			// Verify
			Assert.That(sut.ProjectCode, Is.EqualTo(expectedFirstProject));
		}

	}
}
