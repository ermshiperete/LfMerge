﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;
using System.Linq;
using System.Text.RegularExpressions;
using LfMerge.DataConverters;
using NUnit.Framework;
using SIL.FieldWorks.FDO;

namespace LfMerge.Tests.Fdo.DataConverters
{
	public class ConvertMongoToFdoTsStringsTests // : FdoTestBase
	{
		// *****************
		//     Test data
		// *****************
		private string twoLangs  = "foo<span lang=\"grc\">σπιθαμή</span>bar<span lang=\"fr\">portée</span>baz";
		private string noSpans   = "fooσπιθαμήbarportéebaz";
		private string twoStyles = "this has <span class=\"styleName_Bold\">bold</span> and <span class=\"styleName_Italic\">italic</span> text";
		private string twoGuids  = "this has <span class=\"guid_01234567-1234-4321-89ab-0123456789ab\">two</span> different <span class=\"guid_98765432-1234-4321-89ab-0123456789ab\">guid</span> classes, but no language spans";
		private string oneGuidOneStyle  = "this has <span class=\"styleName_Bold\">bold</span> and <span class=\"guid_01234567-1234-4321-89ab-0123456789ab\">guid-containing</span> text";
		private string twoGuidsOneStyle = "this has <span class=\"guid_01234567-1234-4321-89ab-0123456789ab styleName_Bold\">two</span> different <span class=\"guid_98765432-1234-4321-89ab-0123456789ab\">guid</span> classes, and the first is bold, but there are no language spans";
		private string twoGuidsTwoStylesNoLangs = "this has <span class=\"guid_01234567-1234-4321-89ab-0123456789ab styleName_Bold\">two (B)</span> different <span class=\"guid_98765432-1234-4321-89ab-0123456789ab styleName_Italic\">guid (I)</span> classes, and two styles, but there are no language spans";
		private string twoGuidsTwoStylesOneLang = "this has <span class=\"guid_01234567-1234-4321-89ab-0123456789ab styleName_Bold\">two (B)</span> different <span lang=\"fr\" class=\"guid_98765432-1234-4321-89ab-0123456789ab styleName_Italic\">guid (I,fr)</span> classes, and two styles, but there are no language spans";
		private string twoGuidsTwoStylesTwoLangs = "this has <span lang=\"grc\" class=\"guid_01234567-1234-4321-89ab-0123456789ab styleName_Bold\">two (B,grc)</span> different <span class=\"guid_98765432-1234-4321-89ab-0123456789ab styleName_Italic\" lang=\"fr\">guid (I,fr)</span> classes, and two styles, but there are no language spans";

		private Guid firstGuid  = Guid.Parse("01234567-1234-4321-89ab-0123456789ab");
		private Guid secondGuid = Guid.Parse("98765432-1234-4321-89ab-0123456789ab");

		public ConvertMongoToFdoTsStringsTests()
		{
		}

		// *************
		//     Tests
		// *************
		[Test]
		public void CanDetectSpans()
		{
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(noSpans),   Is.EqualTo(0));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoLangs),  Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoStyles), Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoGuids),  Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(oneGuidOneStyle),  Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoGuidsOneStyle), Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoGuidsTwoStylesNoLangs),  Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoGuidsTwoStylesOneLang),  Is.EqualTo(2));
			Assert.That(ConvertMongoToFdoTsStrings.SpanCount(twoGuidsTwoStylesTwoLangs), Is.EqualTo(2));
		}

		[Test]
		public void CanExtractTextInsideSpans()
		{
			string[] textInZeroSpans = ConvertMongoToFdoTsStrings.GetSpanTexts(noSpans)  .ToArray();
			string[] textInTwoLangs  = ConvertMongoToFdoTsStrings.GetSpanTexts(twoLangs) .ToArray();
			string[] textInTwoStyles = ConvertMongoToFdoTsStrings.GetSpanTexts(twoStyles).ToArray();
			string[] textInTwoGuids  = ConvertMongoToFdoTsStrings.GetSpanTexts(twoGuids) .ToArray();
			string[] textInOneGuidOneStyle  = ConvertMongoToFdoTsStrings.GetSpanTexts(oneGuidOneStyle) .ToArray();
			string[] textInTwoGuidsOneStyle = ConvertMongoToFdoTsStrings.GetSpanTexts(twoGuidsOneStyle).ToArray();
			string[] textInTwoGuidsTwoStylesNoLangs  = ConvertMongoToFdoTsStrings.GetSpanTexts(twoGuidsTwoStylesNoLangs) .ToArray();
			string[] textInTwoGuidsTwoStylesOneLang  = ConvertMongoToFdoTsStrings.GetSpanTexts(twoGuidsTwoStylesOneLang) .ToArray();
			string[] textInTwoGuidsTwoStylesTwoLangs = ConvertMongoToFdoTsStrings.GetSpanTexts(twoGuidsTwoStylesTwoLangs).ToArray();

			Assert.That(textInZeroSpans.Length, Is.EqualTo(0));

			Assert.That(textInTwoLangs.Length, Is.EqualTo(2));
			Assert.That(textInTwoLangs[0], Is.EqualTo("σπιθαμή"));
			Assert.That(textInTwoLangs[1], Is.EqualTo("portée"));

			Assert.That(textInTwoStyles.Length, Is.EqualTo(2));
			Assert.That(textInTwoStyles[0], Is.EqualTo("bold"));
			Assert.That(textInTwoStyles[1], Is.EqualTo("italic"));

			Assert.That(textInTwoGuids.Length, Is.EqualTo(2));
			Assert.That(textInTwoGuids[0], Is.EqualTo("two"));
			Assert.That(textInTwoGuids[1], Is.EqualTo("guid"));

			Assert.That(textInOneGuidOneStyle.Length, Is.EqualTo(2));
			Assert.That(textInOneGuidOneStyle[0], Is.EqualTo("bold"));
			Assert.That(textInOneGuidOneStyle[1], Is.EqualTo("guid-containing"));

			Assert.That(textInTwoGuidsOneStyle.Length, Is.EqualTo(2));
			Assert.That(textInTwoGuidsOneStyle[0], Is.EqualTo("two"));
			Assert.That(textInTwoGuidsOneStyle[1], Is.EqualTo("guid"));

			Assert.That(textInTwoGuidsTwoStylesNoLangs.Length, Is.EqualTo(2));
			Assert.That(textInTwoGuidsTwoStylesNoLangs[0], Is.EqualTo("two (B)"));
			Assert.That(textInTwoGuidsTwoStylesNoLangs[1], Is.EqualTo("guid (I)"));

			Assert.That(textInTwoGuidsTwoStylesOneLang.Length, Is.EqualTo(2));
			Assert.That(textInTwoGuidsTwoStylesOneLang[0], Is.EqualTo("two (B)"));
			Assert.That(textInTwoGuidsTwoStylesOneLang[1], Is.EqualTo("guid (I,fr)"));

			Assert.That(textInTwoGuidsTwoStylesTwoLangs.Length, Is.EqualTo(2));
			Assert.That(textInTwoGuidsTwoStylesTwoLangs[0], Is.EqualTo("two (B,grc)"));
			Assert.That(textInTwoGuidsTwoStylesTwoLangs[1], Is.EqualTo("guid (I,fr)"));
		}

		[Test]
		public void CanClassifySpansByLanguage()
		{
			// Spans will look like: <span lang="en" class="guid_123-456 styleName_DefaultText"</span>
			string[] langsInZeroSpans = ConvertMongoToFdoTsStrings.GetSpanLanguages(noSpans)  .ToArray();
			string[] langsInTwoLangs  = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoLangs) .ToArray();
			string[] langsInTwoStyles = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoStyles).ToArray();
			string[] langsInTwoGuids  = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoGuids) .ToArray();
			string[] langsInOneGuidOneStyle  = ConvertMongoToFdoTsStrings.GetSpanLanguages(oneGuidOneStyle) .ToArray();
			string[] langsInTwoGuidsOneStyle = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoGuidsOneStyle).ToArray();
			string[] langsInTwoGuidsTwoStylesNoLangs  = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoGuidsTwoStylesNoLangs) .ToArray();
			string[] langsInTwoGuidsTwoStylesOneLang  = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoGuidsTwoStylesOneLang) .ToArray();
			string[] langsInTwoGuidsTwoStylesTwoLangs = ConvertMongoToFdoTsStrings.GetSpanLanguages(twoGuidsTwoStylesTwoLangs).ToArray();

			Assert.That(langsInZeroSpans.Length, Is.EqualTo(0));

			Assert.That(langsInTwoLangs.Length,  Is.EqualTo(2));
			Assert.That(langsInTwoLangs[0], Is.EqualTo("grc"));
			Assert.That(langsInTwoLangs[1], Is.EqualTo("fr"));

			Assert.That(langsInTwoStyles.Length, Is.EqualTo(0));
			Assert.That(langsInTwoGuids.Length,  Is.EqualTo(0));
			Assert.That(langsInOneGuidOneStyle.Length,  Is.EqualTo(0));
			Assert.That(langsInTwoGuidsOneStyle.Length, Is.EqualTo(0));

			Assert.That(langsInTwoGuidsTwoStylesNoLangs.Length,  Is.EqualTo(0));
			Assert.That(langsInTwoGuidsTwoStylesOneLang.Length,  Is.EqualTo(1));
			Assert.That(langsInTwoGuidsTwoStylesTwoLangs.Length, Is.EqualTo(2));

			Assert.That(langsInTwoGuidsTwoStylesOneLang[0],  Is.EqualTo("fr"));
			Assert.That(langsInTwoGuidsTwoStylesTwoLangs[0], Is.EqualTo("grc"));
			Assert.That(langsInTwoGuidsTwoStylesTwoLangs[1], Is.EqualTo("fr"));
		}

		[Test]
		public void CanExtractGuidsFromSpans()
		{
			Guid[] guidsInZeroSpans = ConvertMongoToFdoTsStrings.GetSpanGuids(noSpans)  .ToArray();
			Guid[] guidsInTwoLangs  = ConvertMongoToFdoTsStrings.GetSpanGuids(twoLangs) .ToArray();
			Guid[] guidsInTwoStyles = ConvertMongoToFdoTsStrings.GetSpanGuids(twoStyles).ToArray();
			Guid[] guidsInTwoGuids  = ConvertMongoToFdoTsStrings.GetSpanGuids(twoGuids) .ToArray();
			Guid[] guidsInOneGuidOneStyle  = ConvertMongoToFdoTsStrings.GetSpanGuids(oneGuidOneStyle) .ToArray();
			Guid[] guidsInTwoGuidsOneStyle = ConvertMongoToFdoTsStrings.GetSpanGuids(twoGuidsOneStyle).ToArray();
			Guid[] guidsInTwoGuidsTwoStylesNoLangs  = ConvertMongoToFdoTsStrings.GetSpanGuids(twoGuidsTwoStylesNoLangs) .ToArray();
			Guid[] guidsInTwoGuidsTwoStylesOneLang  = ConvertMongoToFdoTsStrings.GetSpanGuids(twoGuidsTwoStylesOneLang) .ToArray();
			Guid[] guidsInTwoGuidsTwoStylesTwoLangs = ConvertMongoToFdoTsStrings.GetSpanGuids(twoGuidsTwoStylesTwoLangs).ToArray();

			Assert.That(guidsInZeroSpans.Length, Is.EqualTo(0));
			Assert.That(guidsInTwoLangs.Length,  Is.EqualTo(0));
			Assert.That(guidsInTwoStyles.Length, Is.EqualTo(0));
			Assert.That(guidsInTwoGuids.Length,  Is.EqualTo(2));
			Assert.That(guidsInOneGuidOneStyle.Length,  Is.EqualTo(1));
			Assert.That(guidsInTwoGuidsOneStyle.Length, Is.EqualTo(2));
			Assert.That(guidsInTwoGuidsTwoStylesNoLangs.Length,  Is.EqualTo(2));
			Assert.That(guidsInTwoGuidsTwoStylesOneLang.Length,  Is.EqualTo(2));
			Assert.That(guidsInTwoGuidsTwoStylesTwoLangs.Length, Is.EqualTo(2));

			Assert.That(guidsInTwoGuids[0], Is.EqualTo(firstGuid));
			Assert.That(guidsInTwoGuids[1], Is.EqualTo(secondGuid));

			Assert.That(guidsInOneGuidOneStyle[0],  Is.EqualTo(firstGuid));
			Assert.That(guidsInTwoGuidsOneStyle[0], Is.EqualTo(firstGuid));
			Assert.That(guidsInTwoGuidsOneStyle[1], Is.EqualTo(secondGuid));

			Assert.That(guidsInTwoGuidsTwoStylesNoLangs[0],  Is.EqualTo(firstGuid));
			Assert.That(guidsInTwoGuidsTwoStylesNoLangs[1],  Is.EqualTo(secondGuid));
			Assert.That(guidsInTwoGuidsTwoStylesOneLang[0],  Is.EqualTo(firstGuid));
			Assert.That(guidsInTwoGuidsTwoStylesOneLang[1],  Is.EqualTo(secondGuid));
			Assert.That(guidsInTwoGuidsTwoStylesTwoLangs[0], Is.EqualTo(firstGuid));
			Assert.That(guidsInTwoGuidsTwoStylesTwoLangs[1], Is.EqualTo(secondGuid));
		}

		[Test]
		public void CanExtractStylesFromSpans()
		{
			string[] stylesInZeroSpans = ConvertMongoToFdoTsStrings.GetSpanStyles(noSpans)  .ToArray();
			string[] stylesInTwoLangs  = ConvertMongoToFdoTsStrings.GetSpanStyles(twoLangs) .ToArray();
			string[] stylesInTwoStyles = ConvertMongoToFdoTsStrings.GetSpanStyles(twoStyles).ToArray();
			string[] stylesInTwoGuids  = ConvertMongoToFdoTsStrings.GetSpanStyles(twoGuids) .ToArray();
			string[] stylesInOneGuidOneStyle  = ConvertMongoToFdoTsStrings.GetSpanStyles(oneGuidOneStyle) .ToArray();
			string[] stylesInTwoGuidsOneStyle = ConvertMongoToFdoTsStrings.GetSpanStyles(twoGuidsOneStyle).ToArray();
			string[] stylesInTwoGuidsTwoStylesNoLangs  = ConvertMongoToFdoTsStrings.GetSpanStyles(twoGuidsTwoStylesNoLangs) .ToArray();
			string[] stylesInTwoGuidsTwoStylesOneLang  = ConvertMongoToFdoTsStrings.GetSpanStyles(twoGuidsTwoStylesOneLang) .ToArray();
			string[] stylesInTwoGuidsTwoStylesTwoLangs = ConvertMongoToFdoTsStrings.GetSpanStyles(twoGuidsTwoStylesTwoLangs).ToArray();

			Assert.That(stylesInZeroSpans.Length, Is.EqualTo(0));
			Assert.That(stylesInTwoLangs.Length,  Is.EqualTo(0));
			Assert.That(stylesInTwoStyles.Length, Is.EqualTo(2));
			Assert.That(stylesInTwoGuids.Length,  Is.EqualTo(0));
			Assert.That(stylesInOneGuidOneStyle.Length,  Is.EqualTo(1));
			Assert.That(stylesInTwoGuidsOneStyle.Length, Is.EqualTo(1));
			Assert.That(stylesInTwoGuidsTwoStylesNoLangs.Length,  Is.EqualTo(2));
			Assert.That(stylesInTwoGuidsTwoStylesOneLang.Length,  Is.EqualTo(2));
			Assert.That(stylesInTwoGuidsTwoStylesTwoLangs.Length, Is.EqualTo(2));

			Assert.That(stylesInTwoStyles[0], Is.EqualTo("Bold"));
			Assert.That(stylesInTwoStyles[1], Is.EqualTo("Italic"));
			Assert.That(stylesInOneGuidOneStyle[0],  Is.EqualTo("Bold"));
			Assert.That(stylesInTwoGuidsOneStyle[0], Is.EqualTo("Bold"));
			Assert.That(stylesInTwoGuidsTwoStylesNoLangs[0],  Is.EqualTo("Bold"));
			Assert.That(stylesInTwoGuidsTwoStylesNoLangs[1],  Is.EqualTo("Italic"));
			Assert.That(stylesInTwoGuidsTwoStylesOneLang[0],  Is.EqualTo("Bold"));
			Assert.That(stylesInTwoGuidsTwoStylesOneLang[1],  Is.EqualTo("Italic"));
			Assert.That(stylesInTwoGuidsTwoStylesTwoLangs[0], Is.EqualTo("Bold"));
			Assert.That(stylesInTwoGuidsTwoStylesTwoLangs[1], Is.EqualTo("Italic"));
		}
	}
}

