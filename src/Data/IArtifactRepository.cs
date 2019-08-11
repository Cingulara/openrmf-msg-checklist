using openrmf_msg_checklist.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace openrmf_msg_checklist.Data {
    public interface IArtifactRepository
    {        
        // get the checklist and all its metadata in a record from the DB
        Task<Artifact> GetArtifact(string id);

        // return checklist records for a given system
        Task<IEnumerable<Artifact>> GetSystemArtifacts(string system);
    }
}