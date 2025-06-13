using Newtonsoft.Json.Linq;
using System.Linq.Dynamic.Core.Tokenizer;

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



        /// <summary>
        /// Try to get a specific value from a JToken by key. Return false if the key does not exist or the value is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryToGetSpecificValue<T>(this JToken? token, string key, out T? value)
        {
            value = default;
            var ret = false;
           
           if(token?.Type == JTokenType.Object)
            {
                value = token.Value<T>(key);
                ret = value is null ? false : true ;
            }
            return ret;
         
       }

    }
}
