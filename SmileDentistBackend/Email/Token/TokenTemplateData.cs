using Newtonsoft.Json;

namespace SmileDentistBackend.Email.Token
{
    public class TokenTemplateData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
