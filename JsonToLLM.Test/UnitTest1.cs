using HandlebarsDotNet;
using Newtonsoft.Json;
using System.Collections;
using System.Text.Json;
using HandlebarsDotNet.Collections;
using HandlebarsDotNet.Extension.NewtonsoftJson;
using HandlebarsDotNet.Features;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.PathStructure;
using Newtonsoft.Json.Linq;

namespace JsonToLLM.Test
{
    public class UnitTest1
    {
        public class EnvGenerator : IEnumerable<object[]>
        {
            private readonly List<IHandlebars> _data = new List<IHandlebars>
            {
                Handlebars.Create(new HandlebarsConfiguration().UseNewtonsoftJson())
            };

            public IEnumerator<object[]> GetEnumerator() => _data.Select(o => new object[] { o }).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [InlineData("1")]
        [InlineData("22474.1")]
        [InlineData("2247483647")]
        [InlineData("79228162514264337593543950336")]
        [InlineData("340282346638528859811704183484516925440")]
        [InlineData("\"2020-12-13T19:23:33.9408700\"")]
        [InlineData("\"8C82D441-EE53-47C6-9400-3B5045A4DF71\"")]
        public void ValueTypes(string value)
        {
            var model = JsonConvert.DeserializeObject("{ \"value\": " + value + " }");

            var source = "{{this.value}}";

            var handlebars = Handlebars.Create();
            handlebars.Configuration.UseNewtonsoftJson();
            var template = handlebars.Compile(source);

            var output = template(model).ToUpper();

            Assert.Equal(value.Trim('"'), output);
        }


        [Fact]
        public void JsonTestObjects()
        {
            JObject model = JObject.Parse(File.ReadAllText(@".\json\dxl-response.json"));


            var source = @"
            Credito:
            Il credito residuo è di {{Liv1_credito.balance}} euro
            
            Ricariche:
            {{#each Liv1_ricariche}}Ricarica effettuata di {{importo}} {{unit}} in data {{date}} 
            {{/each}}
            ";

            var handlebars = Handlebars.Create();
            handlebars.Configuration.UseNewtonsoftJson();

            var template = handlebars.Compile(source);

            var output = template(model);

           // Assert.Equal("Key1Val1Key2Val2", output);
        }
    }
}