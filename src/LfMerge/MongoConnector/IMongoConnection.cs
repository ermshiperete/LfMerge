﻿// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using LfMerge.LanguageForge.Model;

namespace LfMerge.MongoConnector
{
	public interface IMongoConnection
	{
		IMongoDatabase GetProjectDatabase(ILfProject project);
		IMongoDatabase GetMainDatabase();
		IEnumerable<TDocument> GetRecords<TDocument>(ILfProject project, string collectionName);
		LfOptionList GetLfOptionListByCode(ILfProject project, string listCode);
		bool UpdateRecord(ILfProject project, LfLexEntry data);
		bool UpdateRecord(ILfProject project, LfOptionList data, string listCode);
		bool RemoveRecord(ILfProject project, Guid guid);
		Dictionary<string, LfInputSystemRecord>GetInputSystems(ILfProject project);
		bool SetInputSystems(ILfProject project, Dictionary<string, LfInputSystemRecord> inputSystems,
			bool initialClone = false, string vernacularWs = "", string analysisWs = "");
		bool SetCustomFieldConfig(ILfProject project);
	}
}

