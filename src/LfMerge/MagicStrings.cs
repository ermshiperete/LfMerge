﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;

namespace LfMerge
{
	public static class MagicStrings
	{
		public const string LfOptionListCodeForGrammaticalInfo = "grammatical-info";
		public const string LfOptionListNameForGrammaticalInfo = "Part of Speech";

		// Collections found in individual project DBs
		public const string LfCollectionNameForLexicon = "lexicon";
		public const string LfCollectionNameForOptionLists = "optionlists";
		public const string UnknownString = "***";

		// Collections found in main database
		public const string LfCollectionNameForProjectRecords = "projects";

		public const string WSFolder = "WritingSystemStore";

		// For Flex v9.0+
		//public const string WSFolder = "SharedSettings";

		public const string FDOModelVersion = "7000068";
	}
}

