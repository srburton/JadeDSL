# JadeDSL

[![NuGet](https://img.shields.io/nuget/v/JadeDSL.svg)](https://www.nuget.org/packages/JadeDSL)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

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

```csharp
var dsl = new JadeDSL("(name:\"Alice\"&age>=30)");

var predicate = dsl.Predicate<Person>();

var results = people.Where(predicate);
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
