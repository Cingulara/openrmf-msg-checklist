using openrmf_msg_checklist.Models;
using System.Threading.Tasks;

namespace openrmf_msg_checklist.Data {
    public interface IArtifactRepository
    {        
        // get the checklist and all its metadata in a record from the DB
        Task<Artifact> GetArtifact(string id);
    }
}