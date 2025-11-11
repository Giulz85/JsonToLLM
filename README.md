# JsonToLLM  
Convert structured JSON data into an LLM-friendly format

## 📘 Overview  
**JsonToLLM** is a lightweight C# library that transforms traditional JSON structures into text or simplified JSON formats easier for **Large Language Models (LLMs)** to understand.  

It bridges the gap between structured data and natural language, making it ideal for integrating backend systems with AI models that need to reason over structured information.

---

## ✨ Key Features  
- 🧩 Converts JSON into readable, LLM-optimized formats  
- ⚙️ Compatible with **.NET 6+** and **.NET 8**  
- 🧠 Template-based transformation system for flexible customization  
- 🪶 Minimal dependencies (only `Newtonsoft.Json`)  
- 🔄 Ideal for AI pipelines, data-to-text generation, and prompt construction  

---

## 🚀 Why JsonToLLM  
Raw JSON can be difficult for LLMs to interpret.  
JsonToLLM restructures and simplifies JSON data so that models like GPT can focus on key information and reasoning rather than syntax.

Typical use cases include:
- Converting backend or API data into LLM-readable text  
- Preparing structured input for generative AI workflows  
- Building clear and consistent prompt structures  

---

## 📦 Installation  
Install from NuGet:

```bash
dotnet add package JsonToLLM
```

Or add the source to your project and import the namespace:

```csharp
using JsonToLLM;
```

---

## 🧠 Basic Usage

```csharp
using JsonToLLM;
using Newtonsoft.Json.Linq;

string json = @"{
  'user': {
    'name': 'Alice',
    'age': 31,
    'subscriptions': ['Internet', 'TV']
  }
}";

// Parse JSON
var token = JToken.Parse(json);

// Convert to an LLM-friendly text format
var llmText = JsonToLLMConverter.Convert(token);

Console.WriteLine(llmText);
```

### 🧾 Output Example
```
User:
- Name: Alice
- Age: 31
- Subscriptions: Internet, TV
```

---

## 🧩 Advanced Examples

### Example 1 – Including nested objects
```csharp
string json = @"{
  'order': {
    'id': 1234,
    'customer': { 'name': 'John Doe', 'email': 'john@doe.com' },
    'items': [
      { 'product': 'Laptop', 'price': 999.99 },
      { 'product': 'Mouse', 'price': 25.50 }
    ],
    'status': 'Delivered'
  }
}";

var llmText = JsonToLLMConverter.Convert(JToken.Parse(json));

Console.WriteLine(llmText);
```

**Output:**
```
Order:
- Id: 1234
- Customer:
  - Name: John Doe
  - Email: john@doe.com
- Items:
  - Product: Laptop
    Price: 999.99
  - Product: Mouse
    Price: 25.50
- Status: Delivered
```

---

### Example 2 – Simplified JSON output
```csharp
var options = new JsonToLLMOptions
{
    Mode = OutputMode.Json, // produce simplified JSON instead of text
    Indent = true
};

var llmJson = JsonToLLMConverter.Convert(JToken.Parse(json), options);

Console.WriteLine(llmJson);
```

**Output:**
```json
{
  "order": {
    "id": 1234,
    "customer": "John Doe",
    "items": ["Laptop", "Mouse"],
    "status": "Delivered"
  }
}
```

---

### Example 3 – Real-world scenario  
Suppose your backend returns a user profile and account summary, and you want to give an LLM a readable representation:

```csharp
var userData = @"{
  'name': 'Marco Rossi',
  'balance': 24.8,
  'last_topup': '2025-10-05',
  'offers': [
    { 'name': 'Giga Unlimited', 'price': 9.99 },
    { 'name': 'Voice Premium', 'price': 5.00 }
  ]
}";

Console.WriteLine(JsonToLLMConverter.Convert(JToken.Parse(userData)));
```

**Output:**
```
Customer Information:
- Name: Marco Rossi
- Balance: 24.8 €
- Last Top-Up: 5 October 2025
- Active Offers:
  - Giga Unlimited (9.99 €)
  - Voice Premium (5.00 €)
```

Perfect for use in a prompt like:
> “Given the customer data below, summarize the current offers and suggest the best upgrade option.”

---

## ⚙️ Project Structure  
```
/src        → core library  
/tests      → unit and integration tests  
/examples   → sample usage  
README.md   → documentation  
```

---

## 💡 Tips & Best Practices  
- Clean your JSON before conversion to remove irrelevant fields  
- Use semantic labels to improve comprehension  
- Avoid passing very large JSON blobs directly to LLMs  
- Combine JsonToLLM with well-designed prompts for best performance  

---

## 🤝 Contributing  
Contributions are welcome!  

1. Fork the repository  
2. Create a feature or fix branch  
3. Add tests for your changes  
4. Open a pull request describing your improvements  

---

## 📄 License  
This project is licensed under the **MIT License** — see the [LICENSE](./LICENSE) file for details.

---

## 🙏 Acknowledgements  
Thanks to the open-source community and all developers improving the interaction between structured data and AI models.
