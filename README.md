# JsonToLLM

**JsonToLLM** is a lightweight and flexible .NET library designed to transform JSON structures into formats suitable for Generative AI (GenAI) applications.
Often, APIs are not originally designed with GenAI in mind — they expose data in complex, verbose formats, include technical fields irrelevant for language models, or use naming conventions that are not natural or conversational.

**JsonToLLM** provides a simple and declarative way to reshape these API responses into **LLM-friendly formats**, using a template syntax that makes transformations readable, maintainable, and transparent.

With JsonToLLM, you can:

* Remove unnecessary or verbose data fields
* Rename fields to natural, descriptive names
* Flatten nested JSON structures
* Reformat and localize values (dates, units, etc.)
* Build readable data structures that LLMs can easily understand

---

## 🚀 Features

* Transform JSON using a declarative template syntax
* Support for embedded expressions like `@value(path)` and `@formatdate()`
* Powerful iteration through `@operator: each`
* Works with nested objects and arrays
* Easily extensible through a clean architecture (`IExpressionEngine`, `IFactoryOperator`)

---

## 🧹 Installation

```bash
dotnet add package JsonToLLM
```

or include the project in your solution and reference it directly.

---

## ⚙️ Usage

### Basic transformation

```csharp
using Newtonsoft.Json.Linq;
using JsonToLLM;
using JsonToLLM.Model;

// Create source and template
var source = JObject.Parse(@"{ \"foo\": \"bar\" }");
var template = JObject.Parse(@"{ \"result\": \"@value(foo)\" }");

// Create context
var ctx = TemplateContext.Create(source, source);

// Transform
var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output: { "result": "bar" }
```

---

### Combine multiple values

```csharp
var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'value2' }");
var template = JObject.Parse(@"{ 'result': '@value(prop1)@value(prop2)' }");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output: { "result": "value1value2" }
```

---

### Access nested fields

```csharp
var source = JObject.Parse(@"{ 'prop1':'value1', 'object1': { 'prop2':'value2'} }");
var template = JObject.Parse(@"{ 'result': '@value($.prop1)@value($.object1.prop2)' }");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output: { "result": "value1value2" }
```

---

### Format dates

```csharp
var source = JObject.Parse(@"{ 'originalDate':'29-05-2025'}");
var template = JObject.Parse(@"{ 'formatedDate': '@formatdate(@value($.originalDate),dd-MM-yyyy,dd/MM/yyyy)' }");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output: { "formatedDate": "29/05/2025" }
```

---

### Combine text and expressions

```csharp
var source = JObject.Parse(@"{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'} }");
var template = JObject.Parse(@"{ 'result1': 'The customer @value($.name) @value($.secondName) lives in @value($.address.city)' }");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output: { "result1": "The customer giuliano arru lives in saronno" }
```

---

### Iterate over arrays with `@operator: each`

#### Objects as elements

```csharp
var source = JObject.Parse(@"{ 'array': [ { 'prop':'value'}, { 'prop':'value1'}, { 'prop':'value2'} ]}");
var template = JObject.Parse(@"{ 'result': { '@operator':'each','@path':'array','@element':{ 'field': '@value(prop)' } } }");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output:
// {
//   "result": [
//     { "field": "value" },
//     { "field": "value1" },
//     { "field": "value2" }
//   ]
// }
```

#### Strings as elements

```csharp
var source = JObject.Parse(@"{ 'customers': [ { 'name':'giuliano', 'secondName':'arru'}, { 'name':'mario', 'secondName':'rossi'} ]}");
var template = JObject.Parse(@"{ 'result': { '@operator':'each','@path':'customers','@element': 'Customer @value(name) @value(secondName)' } }");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output:
// {
//   "result": [
//     "Customer giuliano arru",
//     "Customer mario rossi"
//   ]
// }
```

---

### Nested `each` operators

```csharp
var source = JObject.Parse(@"{ 'customers': [ { 'name':'mario', 'secondName':'rossi', 'counters':[{'amount':3, 'unit':'euro', 'date': '29-05-2025'}, {'amount':4, 'unit':'dollar', 'date': '29-05-2025'}] } ]}");
var template = JObject.Parse(@"{ 'result': {
    '@operator':'each',
    '@path':'customers',
    '@element': {
        'customer':'@value(name) @value(secondName)',
        'counters': {
            '@operator':'each',
            '@path':'counters',
            '@element': 'Speso @value(amount) @value(unit) in data @formatdate(@value(date),dd-MM-yyyy,dd/MM/yyyy)'
        }
    }
}} ");
var ctx = TemplateContext.Create(source, source);

var transformer = new TemplateEngine(new ExpressionEngine(), new FactoryOperator());
var result = transformer.Transform(template, ctx);

// Output:
// {
//   "result": [
//     {
//       "customer": "mario rossi",
//       "counters": [
//         "Speso 3 euro in data 29/05/2025",
//         "Speso 4 dollar in data 29/05/2025"
//       ]
//     }
//   ]
// }
```

---

## 🧠 Architecture Overview

* **TemplateEngine** – the main orchestrator that parses the template and executes transformations.
* **ExpressionEngine** – evaluates inline expressions such as `@value()` and `@formatdate()`.
* **FactoryOperator** – handles higher-level operators such as `@operator: each`.
* **TemplateContext** – carries the input and contextual data during the transformation.

---

## 🤪 Running Tests

```bash
dotnet test
```

---

## 📄 License

MIT License – see the [LICENSE](LICENSE) file for details.
