﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;
using System.Text.RegularExpressions;
using LfMerge.DataConverters;
using NUnit.Framework;
using SIL.FieldWorks.FDO;

namespace LfMerge.Tests.Fdo.DataConverters
{
	public class ConvertMongoToFdoTsStringsTests // : FdoTestBase
	{
		private string hasSpans = "foo<span ws=\"grc\">σπιθαμή</span>bar<span ws=\"fr\">portée</span>baz";
		private string noSpans = "fooσπιθαμήbarportéebaz";

		public ConvertMongoToFdoTsStringsTests()
		{
		}

		[Test]
		public void CanDetectSpans()
		{
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(hasSpans), Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(noSpans),  Is.EqualTo(0));
		}
	}
}

