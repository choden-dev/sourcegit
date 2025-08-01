using System.Text.Json.Serialization;

namespace SourceGit.Models
{
    public class NodeMapping
    {
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; } = string.Empty;

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [JsonPropertyName("remoteDirectory")]
        public string RemoteDirectory { get; set; } = string.Empty;

        public NodeMapping() { }

        public NodeMapping(string nodeId, string hostname, string remoteDirectory)
        {
            NodeId = nodeId;
            Hostname = hostname;
            RemoteDirectory = remoteDirectory;
        }

        public override bool Equals(object obj)
        {
            if (obj is NodeMapping other)
            {
                return NodeId == other.NodeId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return NodeId.GetHashCode();
        }

        public override string ToString()
        {
            return $"NodeMapping(Id: {NodeId}, Host: {Hostname}, Dir: {RemoteDirectory})";
        }
    }
}
