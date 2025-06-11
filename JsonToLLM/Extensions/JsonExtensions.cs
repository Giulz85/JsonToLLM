using Newtonsoft.Json.Linq;

namespace JsonToLLM.Extensions
{
    public static class JsonExstensions
    {
        //public static bool IsNullOrEmpty(this JObject jobject)
        //{
        //    return jobject == null || jobject.Count == 0 || jobject.Type == JTokenType.Null;
        //}

        public static bool IsNullOrEmpty(this JToken? token)
        {
            return token == null ||
                   token.Type == JTokenType.Array && !token.HasValues ||
                   token.Type == JTokenType.Object && !token.HasValues ||
                   token.Type == JTokenType.Date && token.ToString() == string.Empty ||
                   token.Type == JTokenType.String && token.ToString() == string.Empty ||
                   token.Type == JTokenType.Null;
        }

    }
}
