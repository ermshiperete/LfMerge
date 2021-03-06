﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using LfMerge.Core.LanguageForge.Infrastructure;
using System.Collections.Generic;

namespace LfMerge.Core.Tests
{
	public class LanguageForgeProxyMock: ILanguageForgeProxy
	{
		#region ILanguageForgeProxy implementation

		public string UpdateCustomFieldViews(string projectCode, List<CustomFieldSpec> customFieldSpecs)
		{
			return UpdateCustomFieldViews(projectCode, customFieldSpecs, false);
		}

		public string UpdateCustomFieldViews(string projectCode, List<CustomFieldSpec> customFieldSpecs, bool isTest)
		{
			return "true";
		}

		public string ListUsers()
		{
			return null;
		}

		#endregion
	}
}

