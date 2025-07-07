<p align="center">
  <img src="assets/jadeDSL.png" alt="JadeDSL logo" width="300"/>
</p>

[![Build](https://github.com/srburton/JadeDSL/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/srburton/JadeDSL/actions)
[![Tests](https://github.com/srburton/JadeDSL/actions/workflows/tests.yml/badge.svg)](https://github.com/srburton/JadeDSL/actions)
[![NuGet](https://img.shields.io/nuget/v/JadeDSL.svg)](https://www.nuget.org/packages/JadeDSL)
[![NuGet Downloads](https://img.shields.io/nuget/dt/JadeDSL.svg)](https://www.nuget.org/packages/JadeDSL)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![Last Commit](https://img.shields.io/github/last-commit/srburton/JadeDSL)

**JadeDSL** is a lightweight, expressive Domain Specific Language (DSL) parser and evaluator for building complex LINQ-compatible filters in C#.

---

## ✨ Features

- Parse DSL filters like `(name:"John"&age>30)`
- Supports logical operators: AND (`&`) and OR (`|`)
- Supports comparison operators: `=`, `!=`, `>`, `>=`, `<`, `<=`, `:`, `%`, `~`
- Nested expressions and grouping
- Alias resolution (e.g. `@aliasName` → `name`)
- Expression validation and sanitization
- Expression-to-LINQ (`Expression<Func<T, bool>>`) builder
- Safe from OWASP Top 10 injection attacks

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

## 🔧 Usage

### 1. Configure your DSL options

```csharp
var options = new Options();
options.AddAllowedFields("name", "lastname", "age", "city", "price", "address.street", "documents.name", "documents.types.name");
options.AddAlias("@aliasName", "name");
options.AddAlias("@aliasAge", "age");
```

---

### 2. Create a DSL filter using the builder pattern

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(name:\"Alice\"&@aliasAge>=30)")
    .ConfigureOptions(opts =>
    {
        opts.AddAllowedFields("name", "age");
        opts.AddAlias("@aliasAge", "age");
    })
    .Build();
```

---

### 3. Apply the filter to EF Core

```csharp
var results = dbContext.Users
    .WhereDsl(dsl)
    .ToList();
```

---

## 💡 Real-world Examples

### — Filtering with a related collection

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(name:\"Alice\"&documents.name:\"MOU\")")
    .ConfigureOptions(options => {
        options.AddAllowedFields("name", "documents.name");
    })
    .Build();

var results = dbContext.Users
    .Include(u => u.Documents)
    .WhereDsl(dsl)
    .ToList();
```

### — Filtering nested properties in a child collection

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(name:\"Alice\"&documents.types.name:%Img)")
    .ConfigureOptions(options => {
        options.AddAllowedFields("name", "documents.types.name");
    })
    .Build();

var results = dbContext.Users
    .Include(u => u.Documents)
        .ThenInclude(d => d.Types)
    .WhereDsl(dsl)
    .ToList();
```

### — Combining with manual LINQ filters

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(age>=18)")
    .ConfigureOptions(options => options.AddAllowedFields("age"))
    .Build();

var results = dbContext.Address
    .Where(a => a.UserId == 1)
    .WhereDsl(dsl)
    .ToList();
```

---

## 📊 Example Expressions

```dsl
name:"John"
@aliasAge>=30
price~100..500
(city:"NYC"|city:"LA")
(name:"Alice"&lastname:"Smith")
```

---

## ✅ Supported Operators

| Symbol | Description               |
|--------|---------------------------|
| `=`    | Equal                     |
| `!=`   | Not Equal                 |
| `>`    | Greater Than              |
| `>=`   | Greater Than or Equal     |
| `<`    | Less Than                 |
| `<=`   | Less Than or Equal        |
| `:`    | Exact Text Match          |
| `%`    | Like / Contains           |
| `~`    | Between (range)           |

---

## ⚠️ Security

JadeDSL is designed with OWASP Top 10 in mind and includes:

- Token sanitization
- Structural validation
- Node limit enforcement
- Operator allow-listing

---

## 📟 License

This project is licensed under the [MIT License](LICENSE).

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you’d like to change.

---

## 📘 Maintainer

- [@srburton](https://github.com/srburton)