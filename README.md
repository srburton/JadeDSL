<p align="center">
  <img src="assets/jadeDSL.png" alt="JadeDSL logo" width="300"/>
</p>

[![Build](https://github.com/srburton/JadeDSL/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/srburton/JadeDSL/actions)
[![Tests](https://github.com/srburton/JadeDSL/actions/workflows/tests.yml/badge.svg)](https://github.com/srburton/JadeDSL/actions)
[![NuGet](https://img.shields.io/nuget/v/JadeDSL.svg)](https://www.nuget.org/packages/JadeDSL)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JadeDSL.svg)](https://www.nuget.org/packages/JadeDSL)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Last Commit](https://img.shields.io/github/last-commit/srburton/JadeDSL)

**JadeDSL** is a lightweight parser and evaluator for building complex LINQ-compatible filters in C#.

---

## ✨ Features

* Parse filters like `(name:"John" & age>30)`
* Logical operators: AND (`&`), OR (`|`) and grouping with parentheses `()`
* Comparison operators: `=`, `!=`, `>`, `>=`, `<`, `<=`, `:`, `%`, `%%`, `~`
* Supports deeply nested expressions and collections
* Alias resolution for reusable filters (e.g. `@aliasName` → `name`)
* Converts to LINQ-compatible `Expression<Func<T, bool>>`
* Safe against common injection attacks (OWASP Top 10)

---

## 📦 Installation

Install via NuGet:

```bash
dotnet add package JadeDSL
```

Or specify a version:

```bash
dotnet add package JadeDSL --version x.y.z
```

---

## ⚡ Quick Example with `curl`

```bash
curl "http://localhost:5000/api/products?filter=(name:%Laptop|price~1000..3000)&sort=price"
```

Back-end:

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(name:%Laptop|price~1000..3000)")
    .ConfigureOptions(opts =>
    {
        opts.AddAllowedFields("name", "price");
    })
    .Build();

var results = dbContext.Products.WhereDsl(dsl).ToList();
```

---

## 🔧 Usage

### 1. Configure DSL options

```csharp
var options = new Options();
options.AddAllowedFields("name", "price", "location.city", "items.tags.name");
options.AddAlias("@productName", "name");
options.AddAlias("@city", "location.city");
```

### 2. Parse a filter expression

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(@productName:%Phone&@city:NYC)")
    .ConfigureOptions(o => {
        o.AddAllowedFields("name", "location.city");
        o.AddAlias("@productName", "name");
        o.AddAlias("@city", "location.city");
    })
    .Build();
```

### 3. Apply to a query

```csharp
var results = dbContext.Users.WhereDsl(dsl).ToList();
```

---

## 💡 Real-World Examples

### — Nested collections and alias

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(@tagName:Featured&items.tags.name:%Promo)")
    .ConfigureOptions(options => {
        options.AddAllowedFields("items.tags.name");
        options.AddAlias("@tagName", "items.tags.name");
    })
    .Build();
```

### — Deep nesting with multiple OR/AND groups

```csharp
var dsl = new FilterBuilder()
    .WithExpression("((category:\"Electronics\"|category:\"Computers\")&(price~500..1500|name:%Gaming))")
    .ConfigureOptions(opts =>
    {
        opts.AddAllowedFields("category", "price", "name");
    })
    .Build();
```

### — Combine with custom conditions

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(status:active&@city:Chicago)")
    .ConfigureOptions(opts => {
        opts.AddAllowedFields("status", "location.city");
        opts.AddAlias("@city", "location.city");
    })
    .Build();

var result = dbContext.Customers
    .Where(c => c.JoinDate >= DateTime.UtcNow.AddYears(-1))
    .WhereDsl(dsl)
    .ToList();
```

---

## 📊 DSL Expression Examples

```dsl
name:"John"
@productName:%Phone
price~100..500
(city:"NYC"|city:"LA")
(name:"Alice"&lastname:"Smith")
Name%%"Prod"        // contains both sides
Name%"Prod"         // starts with
```

---

## ✅ Supported Operators

| Symbol | Description              |
| ------ | ------------------------ |
| `=`    | Equal                    |
| `!=`   | Not Equal                |
| `>`    | Greater Than             |
| `>=`   | Greater Than or Equal    |
| `<`    | Less Than                |
| `<=`   | Less Than or Equal       |
| `:`    | Exact Text Match         |
| `%`    | Like / StartsWith (left) |
| `%%`   | Like / Contains (both)   |
| `~`    | Between (range)          |

---

## 🔐 Security

JadeDSL is designed with OWASP Top 10 in mind:

* Token sanitization
* Structural validation
* Node count limits
* Operator allow-lists

---

## 📜 License

Licensed under the [MIT License](LICENSE).

---

## 🤝 Contributing

Pull requests are welcome! For major changes, open an issue first to discuss your idea.

---

## 👤 Maintainer

* [@srburton](https://github.com/srburton)
