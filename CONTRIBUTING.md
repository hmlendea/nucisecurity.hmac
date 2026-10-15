# Contributing to NuciSecurity.HMAC

This document covers the guidelines and processes for contributing to this project, including how to report issues, suggest enhancements, submit code changes, and follow project standards.

## 📑 Table of Contents

- [How to Contribute](#-how-to-contribute)
- [Code of Conduct](#-code-of-conduct)
- [Security](#-security)
- [License](#-license)
- [Getting Assistance](#-getting-assistance)

## 🤝 How to Contribute

### Reporting Issues

- Search existing issues first.
- Use the issue templates if available.
- Provide clear reproduction steps.
- Include environment details.

### Suggesting Enhancements

- Check the roadmap and existing discussions.
- Explain the use case and expected behaviour.
- Consider implementation complexity.

### Code Contributions

#### Prerequisites

- .NET 10.0 SDK or newer

#### Development Setup

```bash
# Clone the repository
git clone https://github.com/hmlendea/nucisecurity.git
cd nucisecurity

# Install dependencies
dotnet restore
```

#### Making Changes

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes.
4. Run tests: `dotnet test --no-build --verbosity normal`
5. Commit with clear and descriptive messages.
6. Push to your fork.
7. Open a Pull Request.

#### Pull Request Guidelines

- Target the `master` branch.
- Keep PRs focused and atomic.
- Revise documentation if applicable.
- Include tests for any new functionality.
- Ensure the CI checks pass.

### Code Style

Follow the project's coding standards:
- C# coding conventions as defined in the repository instructions
- Run `dotnet build` before committing to verify compilation

### Testing

```bash
# Run all tests
dotnet test --no-build --verbosity normal
```

### Documentation

- Revise relevant docs for changes.
- Follow the documentation style guide.
- Preview changes locally if possible.

## 📋 Code of Conduct

All participants are expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## 🔒 Security

Report security vulnerabilities per the [Security Policy](docs/security.md).

## 📄 License

By contributing, you agree that your contributions will be licensed under the GNU General Public License v3.0 or later.

## ❓ Getting Assistance

- Open a GitHub issue for questions or problems
- Check existing discussions and documentation first