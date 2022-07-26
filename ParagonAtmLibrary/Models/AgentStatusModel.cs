namespace ParagonAtmLibrary.Models;

public class AgentStatusModel
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("agentStatus")]
    public string AgentStatus { get; set; }

    [JsonPropertyName("webftStatus")]
    public string WebFastStatus { get; set; }

    [JsonPropertyName("proxyStatus")]
    public string ProxyStatus { get; set; }

    [JsonPropertyName("currentUser")]
    public string CurrentUser { get; set; }

    [JsonPropertyName("cpuUsage")]
    public int CpuUsage { get; set; }

    [JsonPropertyName("memoryUsage")]
    public int MemoryUsage { get; set; }

    [JsonPropertyName("isScreenLocked")]
    public bool IsScreenLocked { get; set; }
}
