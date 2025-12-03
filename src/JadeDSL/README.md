# JadeDSL

**JadeDSL** is a lightweight, expressive Domain Specific Language (DSL) parser and evaluator for building complex LINQ-compatible filters in C#.  

It allows you to write intuitive filter expressions and apply them directly to `IQueryable<T>` collections, including EF Core queries.

---

### URL Query Filtering

You can use JadeDSL to translate URL query parameters into powerful filters. For example:

```curl
http://example.com?search=(name:"Alice"&age>=30|documents.title:"MOU")|(status:"active"&createdDate~2023-01-01..2023-12-31)
```

## Features

- Parse DSL filters like `(name:"John"&age>30)`  
- Supports logical operators: AND (`&`) and OR (`|`)  
- Supports comparison operators: `=`, `!=`, `>`, `>=`, `<`, `<=`, `:`, `*`, `**`, `~`, `[]`  
- Nested expressions and grouping with parentheses  
- Alias resolution (e.g. `@alias` → `name`)  
- Expression validation and sanitization  
- Expression-to-LINQ conversion (`Expression<Func<T, bool>>`)  
- Safe from common injection attacks (OWASP Top 10)

---

## Installation

Install via NuGet:

```bash
dotnet add package JadeDSL
```

Or specify a version:

```bash
dotnet add package JadeDSL --version x.y.z
```

---

## Usage

### 1. Create a DSL filter using the builder pattern

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

### 2. Apply the filter to EF Core

```csharp
var results = dbContext.Users
    .WhereDsl(dsl)
    .ToList();
```

---

## Real-world Examples

### Filtering with a related collection

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(name:\"Alice\"&documents.name:\"MOU\")")
    .ConfigureOptions(opts => opts.AddAllowedFields("name", "documents.name"))
    .Build();

var results = dbContext.Users
    .Include(u => u.Documents)
    .WhereDsl(dsl)
    .ToList();
```

### Filtering nested properties in a child collection

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(name:\"Alice\"&documents.types.name:png)")
    .ConfigureOptions(opts => opts.AddAllowedFields("name", "documents.types.name"))
    .Build();

var results = dbContext.Users
    .Include(u => u.Documents)
        .ThenInclude(d => d.Types)
    .WhereDsl(dsl)
    .ToList();
```

### Combining DSL with manual LINQ filters

```csharp
var dsl = new FilterBuilder()
    .WithExpression("(age>=18)")
    .ConfigureOptions(opts => opts.AddAllowedFields("age"))
    .Build();

var results = dbContext.Address
    .Where(a => a.UserId == 1)
    .WhereDsl(dsl)
    .ToList();
```

---

## Example Expressions

```dsl
name:"John"
@aliasAge>=30
price~100..500
(city:"NYC"|city:"LA")
(name:"Alice"&lastname:"Smith")
Name**"Pro"          # Contains "Pro"
Name*"Prod"          # Starts with "Prod"
Name[]"Prod","Dev","Test"  # IN list
```

---

## Supported Operators

| Symbol | Description                     |
| ------ | ------------------------------- |
| `=`    | Equal                           |
| `!=`   | Not Equal                       |
| `>`    | Greater Than                    |
| `>=`   | Greater Than or Equal           |
| `<`    | Less Than                       |
| `<=`   | Less Than or Equal              |
| `:`    | Exact Text Match                |
| `*`    | Like / StartsWith               |
| `**`   | Like / Contains (both sides)    |
| `~`    | Between (range)                 |
| `[]`   | IN (list of values)             |

---

## Security

JadeDSL is designed with security in mind and protects against common injection attacks:

- Token sanitization  
- Structural validation  
- Node limit enforcement  
- Operator allow-listing  

---

## License

This project is licensed under the MIT License.

---

## Download

You can download the latest release from the GitHub repository:

[Download JadeDSL.zip](https://github.com/srburton/JadeDSL/releases/latest/download/JadeDSL.zip)

Or clone the repository:

```bash
git clone https://github.com/srburton/JadeDSL.git
```

---

## Contributing

Contributions are welcome! Please open an issue first to discuss major changes. Pull requests should include tests and follow existing coding style.

---

## Maintainer

* [@srburton](https://github.com/srburton)