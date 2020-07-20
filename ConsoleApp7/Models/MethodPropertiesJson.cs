using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleApp7.Models
{
    //Optimized for tdlib
    public class MethodPropertiesJson
    {
        public MethodPropertiesJson(string initial, DateTimeOffset now, SmalltalkProperty[] properties)
        {
            var intialString = $"{initial} {now.ToString("g")}";
            foreach(var property in properties)
            {
                Instance.Add(property.Name, intialString);
            }
        }
        // Setup with default values
        [JsonPropertyName("class")]
        public object Class { get; set; } = new object(); // Always blank

        // Setup with isntance vars values
        [JsonPropertyName("instance")]
        public Dictionary<string, string> Instance { get; } = new Dictionary<string, string>();
    }
}
