﻿// Copyright (c) 2015 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using LfMerge.LanguageForge.Config;

namespace LfMerge
{
	public class MongoProjectRecord
	{
		public const string ProjectsCollectionName = "projects";

		/// <summary>
		/// Factory function to retrieve the corresponding project record from MongoDB and create a MongoProjectRecord instance.
		/// </summary>
		/// <param name="project">LfProject instance whose data should be fetched from MongoDB.</param>
		public static MongoProjectRecord Create(ILfProject project)
		{
			if (project == null) return null;
			if (MongoConnection.Default == null) return null; // TODO: Figure out better way to deal with MongoConnections during unit testing
			var code = project.LfProjectCode;
			IMongoDatabase db = MongoConnection.Default.GetMainDatabase();
			IMongoCollection<MongoProjectRecord> coll = db.GetCollection<MongoProjectRecord>(ProjectsCollectionName);
			MongoProjectRecord record =
				coll.Find(proj => proj.ProjectCode == project.LfProjectCode)
				.Limit(1).FirstOrDefaultAsync().Result;
			return record;
		}

		public ObjectId Id { get; set; }
		public string ProjectCode { get; set; }
		public string ProjectName { get; set; }
		public LfProjectConfig Config { get; set; }
	}
}

/* Mongo project records have the following fields, but we don't need to map all of them:
{ "_id" : "_id", "value" : null }
{ "_id" : "allowAudioDownload", "value" : null }
{ "_id" : "allowInviteAFriend", "value" : null }
{ "_id" : "appName", "value" : null }
{ "_id" : "config", "value" : null }
{ "_id" : "dateCreated", "value" : null }
{ "_id" : "dateModified", "value" : null }
{ "_id" : "featured", "value" : null }
{ "_id" : "inputSystems", "value" : null }
{ "_id" : "interfaceLanguageCode", "value" : null }
{ "_id" : "isArchived", "value" : null }
{ "_id" : "language", "value" : null }
{ "_id" : "languageCode", "value" : null }
{ "_id" : "liftFilePath", "value" : null }
{ "_id" : "ownerRef", "value" : null }
{ "_id" : "projectCode", "value" : null }
{ "_id" : "projectName", "value" : null }
{ "_id" : "siteName", "value" : null }
{ "_id" : "userJoinRequests", "value" : null }
{ "_id" : "userProperties", "value" : null }
*/

