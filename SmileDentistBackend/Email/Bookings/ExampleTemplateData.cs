﻿using Newtonsoft.Json;

namespace SmileDentistBackend.Email.Bookings
{
    public class ExampleTemplateData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }
    }
}
