// Copyright (c) Cingulara 2019. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007 license. See LICENSE file in the project root for full license information.
using MongoDB.Driver;
using openrmf_msg_checklist.Models;

namespace openrmf_msg_checklist.Data
{
    public class ArtifactContext
    {
        private readonly IMongoDatabase _database = null;

        public ArtifactContext(Settings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            if (client != null)
                _database = client.GetDatabase(settings.Database);
        }

        public IMongoCollection<Artifact> Artifacts
        {
            get
            {
                return _database.GetCollection<Artifact>("Artifacts");
            }
        }
    }
}