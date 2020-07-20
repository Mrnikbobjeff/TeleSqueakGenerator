using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleApp7.Models
{
    // Setup with default values
    public class ClassPropertiesJson
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }
        [JsonPropertyName("classinstvars")]
        public string[] ClassInstanceVariables { get; set; } = Array.Empty<string>();
        [JsonPropertyName("classvars")]
        public string[] ClassVariables { get; set; } = Array.Empty<string>();
        [JsonPropertyName("pools")]
        public string[] Pools { get; set; } = Array.Empty<string>();
        [JsonPropertyName("commentStamp")]
        public string CommentStamp { get; set; } = string.Empty;
        [JsonPropertyName("instvars")]
        public string[] InstanceVariables { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("super")]
        public string Super { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; } = "normal";
    }
}
