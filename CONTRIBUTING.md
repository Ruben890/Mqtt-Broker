# Contributing to Mqtt-Broker (Beginner Friendly)

Thank you for your interest in contributing to **Mqtt-Broker**!  
This project is a MQTT broker built in .NET for IoT and smart devices.  
Whether you want to report a bug, add a feature, or fix a typo, this guide will help you get started quickly.

---

## Table of Contents
- [Good First Issues](#good-first-issues)
- [Reporting a Bug](#reporting-a-bug)
- [Suggesting a Feature](#suggesting-a-feature)
- [Contributing Code](#contributing-code)
- [Testing Your Changes](#testing-your-changes)
- [Pull Request Process](#pull-request-process)
- [Code Style](#code-style)
- [Community Guidelines](#community-guidelines)

---

## Good First Issues

We welcome beginners! Look for issues labeled **good first issue**. These are small, well-defined tasks suitable for first-time contributors.  

**Example beginner tasks:**
- Fix minor typos in the code or documentation
- Add logging for specific MQTT events
- Improve topic normalization or add small unit tests

---

## Reporting a Bug

If you find a bug:

1. Open an **issue** on GitHub.
2. Include:
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Any relevant logs or screenshots

**Example title:**
```
[BUG] MQTT messages not processed when topic contains underscores
```

---

## Suggesting a Feature

If you have an idea:

1. Open an **issue** and describe:
   - What you want to add
   - Why it is useful
   - Optional: how you would implement it
2. Wait for discussion before starting a PR.

**Example title:**
```

[FEATURE] Add persistent message storage for offline clients
````

## Contributing Code

Follow this simple workflow:

1. Fork the repository and create a branch:
```bash
   git checkout -b feature/my-new-feature
````

2. Make your changes.
3. Run tests locally (see next section).
4. Commit your changes:

```bash
   git commit -m "Add support for case-insensitive topics"
```
5. Push your branch:

```bash
   git push origin feature/my-new-feature
```
6. Open a Pull Request.

---

## Testing Your Changes

We use **unit tests** to make sure everything works.

To run tests:

```bash
dotnet test
```

* If adding a new feature, please include a **unit test** demonstrating it works.
* If fixing a bug, please add a **test case** that fails before your fix and passes after.

---

## Pull Request Process

1. Make sure your branch is up to date with `main`.
2. Open a PR with a clear title and description.
3. Reference related issues (e.g., `Fixes #123`).
4. Wait for review and CI checks.
5. Make changes if requested.
6. Once approved, your PR will be merged.

---

## Code Style

* **PascalCase** for classes and methods
* **camelCase** for local variables and parameters
* Keep lines < 120 characters
* Write XML documentation for public methods
* Follow existing project structure

---

## Community Guidelines

* Be respectful and supportive
* Stay on topic in issues and PRs
* Follow the [Contributor Covenant Code of Conduct](https://www.contributor-covenant.org/)

---
Thank you for helping make **Mqtt-Broker** better!

```

Si quieres, puedo **hacer también un ejemplo de `Good First Issues` listo para GitHub**, con 3-5 tareas reales de tu proyecto para que cualquier principiante pueda empezar a contribuir de inmediato.  

¿Quieres que haga eso?
```
