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

Configure your DSL options and apply filters in a few simple steps:

1. Define allowed fields  
   Specify which properties can be queried to prevent unauthorized access.

   ```csharp
   var options = new JadeDSLOptions
   {
       AllowedFields = new[]
       {
           "name",
           "lastname",
           "age",
           "city",
           "price",
           "address.street",
           "documents.name",
           "documents.types.name"
       }
   };
   ```

2. Create a DSL instance  
   Pass your DSL string and the options object.

   ```csharp
   var dsl = new JadeDSL("(name:\"Alice\"&age>=30)", options);
   ```

3. Apply the filter to an EF Core query  
   Use the `WhereDsl` extension just like `Where`, with optional `Include` for navigation properties.

   ```csharp
   var results = dbContext.Users
       .WhereDsl(dsl)
       .ToList();
   ```

### Examples

— Filtering with a related collection  

```csharp
var dsl = new JadeDSL("(name:\"Alice\"&documents.name:\"MOU\")", options);

var results = dbContext.Users
    .Include(u => u.Documents)
        .ThenInclude(d => d.Types)
    .WhereDsl(dsl)
    .ToList();
```

— Filtering nested properties in a child collection  
```csharp
var dsl = new JadeDSL("(name:\"Alice\"&documents.name:\"MOU\"&documents.types.name:%Img)", options);

var results = dbContext.Users
    .Include(u => u.Documents)
        .ThenInclude(d => d.Types)
    .WhereDsl(dsl)
    .ToList();
```

— Combining `WhereDsl` with other predicates  
```csharp
var dsl = new JadeDSL("(age>=18)", options);

var results = dbContext.Address
    .Where(a => a.UserId == 1)
    .WhereDsl(dsl)
    .ToList();
```


---

## 📊 Example Expressions

```dsl
name:"John"
age>=30
price~100..500
(city:"NYC"|city:"LA")
(name:"Alice"&lastname:"Smith")
```

---

## ✅ Supported Operators

| Symbol | Description               |
|-------:|---------------------------|
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

- Structural parser validation

- Maximum node limits

- Operator white-listing

---

## 📟 License

This project is licensed under the [MIT License](LICENSE).

---

## 🤝 Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you’d like to change.

---

## 📘 Maintainers

- [@srburton](https://github.com/srburton)
