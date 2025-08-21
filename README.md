# JsonToLLM

JsonToLLM is a C# library that transforms standard JSON into a format optimized for interpretation by Large Language Models (LLMs). It allows developers to generate dynamic templates based on an input JSON object, making structured data easier to understand and process in AI workflows.

## Table of Contents
1. [Overview & Use Cases](#overview--use-cases)
2. [Getting Started](#getting-started)
3. [Integration Guide](#integration-guide)
4. [Running & Testing](#running--testing)
5. [Usage Examples](#usage-examples)
6. [Architecture Overview](#architecture-overview)
7. [Tech Stack](#tech-stack)
8. [Contributing](#contributing)
9. [License](#license)

## Overview & Use Cases

### Overview
JsonToLLM enables you to transform JSON data into a format that is more easily interpreted by LLMs. It supports dynamic value substitution, expression evaluation, and advanced operators for building complex, data-driven templates. This makes it ideal for generating structured, human-readable outputs or prompts for AI systems.

### Use Cases
- **Evaluate Expressions**: Dynamically resolve expressions within JSON templates, such as formatting dates or computing values.
- **Substitute Values**: Replace placeholders in templates with values from input JSON, supporting both flat and nested structures.
- **Build Dynamic Responses**: Generate structured outputs for APIs, chatbots, or other systems based on input data.
- **Iterate Collections**: Use operators like `each` to process arrays and generate lists or tables.
- **Custom Operators**: Extend the engine with your own operators for domain-specific logic.

## Getting Started

### Prerequisites
- .NET SDK 8.0 or later
- Git

### Setup

1. **Clone the repository:** git clone https://github.com/your-repo/JsonToLLM.git
   cd JsonToLLM
2. **Build the project:** dotnet build
3. **Run tests to ensure everything is working:** dotnet test
## Integration Guide

### Installing the Package
To use JsonToLLM in your project, add the NuGet package:dotnet add package JsonToLLM
### Using the TemplateEngine
Here’s an example of how to use the `TemplateEngine` to process a JSON template:
using JsonToLLM;

```csharp
var template = "{ \"greeting\": \"Hello, @value(name)!\" }";
var input = "{ \"name\": \"World\" }";

var engine = new TemplateEngine();
var result = engine.Process(template, input);

Console.WriteLine(result); // Output: { "greeting": "Hello, World!" }
```

## Running & Testing

To run the application, use the following command: `dotnet run`
To execute the test suite, use: `dotnet test`

## Usage Examples
This section provides a series of examples demonstrating how to use JsonToLLM for various tasks, from simple value substitutions to more complex operations involving expressions and collections.

### Simple Value Substitution
Replace a placeholder with a value from the input JSON.

**Input JSON**
```json
{
  "name": "World"
}
```
**Template**
```json
{
  "greeting": "Hello, @value(name)!"
}
```
**Output**
```json
{
  "greeting": "Hello, World!"
}
```
---

### Multiple Value Substitutions
Combine multiple values in a single field.

**Input JSON**
```json
{
  "firstName": "John",
  "lastName": "Doe"
}
```
**Template**
```json
{
  "fullName": "@value(firstName) @value(lastName)"
}
```
**Output**
```json
{
  "fullName": "John Doe"
}
```
---

### Expression Evaluation: Format Date
Use the `@formatdate` function to reformat a date string.

**Input JSON**
```json
{
  "date": "2023-01-01"
}
```
**Template**
```json
{
  "formattedDate": "@formatdate(@value(date), 'MMMM dd, yyyy')"
}
```
**Output**
```json
{
  "formattedDate": "January 01, 2023"
}
```
---

### Iterating Collections with `each` Operator
Generate a list from an array in the input JSON.

**Input JSON**
```json
{
  "categories": {
    "fruits": [
        {"name": "Apple", "price": 12, "status": "available"},
        {"name": "Banana", "price": 8, "status": "not-available"},
        {"name": "Cherry", "price": 10, "status": "available"}
    ]
  }
}
```
**Template**
```json
{
  "output": {
    "@operator": "each",
    "@path": "categories.fruits",
    "@filter": "@.status == 'available'",
    "@element": "@value(name) item is available for @value(price) dollars."
  }
}
```
**Output**
```json
{
  "output": [
    "Apple item is available for 12 dollars.",
    "Cherry item is available for 10 dollars."
  ]
}
```
---

### Advanced: Nested Operators
Process nested arrays and format values.

**Input JSON**
```json
{
  "categories": [
    {
      "name": "Fruits",
      "items": [
        {"name": "Apple"}, 
        {"name": "Banana"}
      ]
    },
    {
      "name": "Vegetables",
      "items": [
        {"name": "Carrot"}, 
        {"name": "Broccoli"}
      ]
    }
  ]
}
```
**Template**
```json
{
  "output": {
    "@operator": "each",
    "@path": "categories",
    "@element": {
      "category": "@value(name)",
      "items": {
        "@operator": "each",
        "@path": "items",
        "@element": "@value(name)"
      }
    }
  }
}
```
**Output**
```json
{
  "output": [
    {
      "category": "Fruits",
      "items": ["Apple", "Banana"]
    },
    {
      "category": "Vegetables",
      "items": ["Carrot", "Broccoli"]
    }
  ]
}
```
---

### Using the `switch` Expression
Map an input value to a label.

**Input JSON**
```json
{
  "status": "1"
}
```
**Template**
```json
{
  "statusLabel": "switch(@value(status), {'1': 'Active', '2': 'Inactive'}, 'Unknown')"
}
```
**Output**
```json
{
  "statusLabel": "Active"
}
```
---

### Handling Free Text with Expressions
Mix static text and dynamic values.

**Input JSON**
```json
{
  "user": "Alice",
  "action": "logged in"
}
```
**Template**
```json
{
  "message": "User @value(user) has @value(action)."
}
```
**Output**
```json
{
  "message": "User Alice has logged in."
}
```
---

### Error Handling Example
If a function is malformed or a value is missing, the engine will throw an exception, leave the placeholder unchanged or use default value.

**Input JSON**
```json
{
  "client-name": "John"
}
```
**Template**
```json
{
  "greeting": "Hello, @value(name)! Today is @formatdate(@value(undefinedDate), 'MMMM dd, yyyy')."
}
```
**Output**
```json
{
  "greeting": "Hello, John! Today is @formatdate(null, 'MMMM dd, yyyy' ."
}
```
*Malformed or unresolved functions are not replaced and remain as-is in the output.*

---

For more advanced scenarios and troubleshooting, see the [Integration Guide](#integration-guide) or the test cases in the repository.

## Architecture Overview

JsonToLLM is built around the following core components:

1. **TemplateEngine**: The main engine that processes templates and resolves expressions.
2. **ExpressionEngine**: Handles dynamic expressions within templates.
3. **Operators**: A set of built-in operators for common tasks like iteration.
4. **Extensibility**: Allows developers to add custom operators and extend functionality.

The library is designed to be modular and extensible, making it easy to integrate into various workflows.

## Tech Stack

- **C#**: The primary programming language.
- **.NET SDK 8.0**: Framework for building and running the library.
- **xUnit**: For unit testing.
- **Newtonsoft.Json**: For JSON parsing and manipulation.

## Contributing

We welcome contributions! To get started:

1. Fork the repository and create a new branch for your feature or bugfix.
2. Follow the existing code style and conventions.
3. Write tests for your changes and ensure all tests pass.
4. Submit

## License

JsonToLLM is licensed under the MIT License.

- **Permissions:**  
  Commercial use, modification, distribution, private use, and sublicensing are permitted.

- **Conditions:**  
  The license and copyright notice must be included in all copies or substantial portions of the Software.

- **Limitations:**  
  The software is provided "as is", without warranty of any kind, express or implied.  
  The authors are not liable for any damages or other liability arising from its use.

See the [LICENSE](LICENSE) file in the repository for the full legal text.