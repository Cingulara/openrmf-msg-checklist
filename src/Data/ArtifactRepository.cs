using openrmf_msg_checklist.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;

namespace openrmf_msg_checklist.Data {
    public class ArtifactRepository : IArtifactRepository
    {
        private readonly ArtifactContext _context = null;

        public ArtifactRepository(Settings settings)
        {
            _context = new ArtifactContext(settings);
        }

        private ObjectId GetInternalId(string id)
        {
            ObjectId internalId;
            if (!ObjectId.TryParse(id, out internalId))
                internalId = ObjectId.Empty;

            return internalId;
        }

        // query after Id or InternalId (BSonId value)
        //
        public async Task<Artifact> GetArtifact(string id)
        {
            try
            {
                return await _context.Artifacts.Find(artifact => artifact.InternalId == GetInternalId(id)).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<IEnumerable<Artifact>> GetSystemArtifacts(string systemGroupId)
        {
            try
            {
                var query = await _context.Artifacts.FindAsync(artifact => artifact.systemGroupId == systemGroupId);
                return query.ToList().OrderBy(x => x.title);
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }
    }
}