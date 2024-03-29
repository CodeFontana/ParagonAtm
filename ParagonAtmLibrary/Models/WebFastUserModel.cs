﻿namespace ParagonAtmLibrary.Models;

public class WebFastUserModel
{
    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    [JsonPropertyName("groupId")]
    public int GroupId { get; set; }

    [JsonIgnore]
    public string SessionToken { get; set; }

    [JsonIgnore]
    public List<WebFastUserGroupModel> WebFastGroups { get; set; }
}
