using JsonToLLM.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonToLLM.Extensions;

namespace JsonToLLM.Helpers
{
    internal static class JsonHelper
    {
        /// <summary>
        /// Naviga attraverso i nodi cercando delle date
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        public static void WalkByIsoDate(this JToken node, Action<string, string, JValue, DateTime> action)
        {
            if (node.Type == JTokenType.Object)
            {
                foreach (var child in node.Children<JProperty>())
                    child.Value.WalkByIsoDate(action);
            }
            else if (node.Type == JTokenType.Array)
            {
                foreach (var child in node.Children())
                    child.WalkByIsoDate(action);
            }
            else
            {
                if (node.Type == JTokenType.Date)
                {
                    var times = (DateTime?)node;
                    if (times.HasValue)
                    {
                        action(node.Parent is JProperty ? ((JProperty)node.Parent).Name : node.Path, node.Path, (JValue)node, times.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Naviga attraverso i nodi verificando la condition se la condition è verificata esegue l'action
        /// </summary>
        /// <param name="node"></param>
        /// <param name="action"></param>
        public static void WalkByCondition(this JToken node, Func<string,JToken,bool> condition, Action<string, string, JToken> action)
        {
            var propertyName = node.Parent is JProperty property ? property.Name : node.Path;
            if (condition(propertyName, node))
            {
                action(propertyName, node.Path, node);
            }
            else if (node.Type == JTokenType.Object)
            {
                foreach (var child in node.Children<JProperty>())
                    child.Value.WalkByCondition(condition, action);
            }
            else if (node.Type == JTokenType.Array)
            {
                foreach (var child in node.Children())
                    child.WalkByCondition(condition, action);
            }
           
        }


        public static void RestoreOriginalJson(this JToken node)
        {
            Func<string, JToken, bool> checkIsRedisDate = (key, token) => token is JObject obj && obj.ContainsKey(RedisDateTime.TypeProperty) 
            && (obj.Value<string>(RedisDateTime.TypeProperty)?.Equals(RedisDateTime.TypeValue) ?? false);
            node.WalkByCondition(checkIsRedisDate,
                       (name, path, node) =>
                       {
                           var objDate = (JObject)node;
                           var date = objDate.Value<DateTime>(RedisDateTime.IsoDateProperty);
                           node.Replace(new JValue(date));
                       });
        }


        public static void UpdateJsonWithRedisDateTime(this JToken node)
        {
            node.WalkByCondition(
                    (key, token) => token.Type == JTokenType.Date,
                    (name, path, node) =>
                    {
                        var times = (DateTime?)node;
                        if (times.HasValue)
                        {
                            var unixDate = times?.ToUnixTime();
                            node.Replace(JToken.FromObject(new RedisDateTime()
                            {
                                ISODate = times!.Value,
                                UnixDate = unixDate
                            }));
                        }
                    });

        }
    }

    internal class RedisDateTime
    {
        internal const string TypeValue = "redisDate";
        internal const string IsoDateProperty = "isoDate";
        internal const string UnixDateProperty = "unixDate";
        internal const string TypeProperty = "$type";

        public RedisDateTime()
        {

        }

        [JsonProperty(TypeProperty)]
        public string Type { get; } = TypeValue;

        [JsonProperty(IsoDateProperty)]
        public DateTime ISODate { get; init; }

        [JsonProperty(UnixDateProperty)]
        public long? UnixDate { get; init; }
    }
}
