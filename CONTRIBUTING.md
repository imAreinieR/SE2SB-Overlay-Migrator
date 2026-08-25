# Contributing to SE2SB Overlay Migrator

First off, thanks for taking the time to contribute! 🎉 This project is open source, and contributions of all kinds — bug fixes, new features, documentation improvements, or even just reporting issues — are welcome.

## Before You Start

Please take a moment to read our [Code of Conduct](CODE_OF_CONDUCT.md). We expect everyone participating in this project to follow it.

## How Can I Contribute?

### Reporting Bugs

If you've found a bug:

1. Check the [Issues](../../issues) tab to see if it's already been reported.
2. If not, open a new issue and include:
   - A clear, descriptive title
   - Steps to reproduce the problem
   - What you expected to happen vs. what actually happened
   - Your OS/version and any relevant logs or screenshots

> ⚠️ **Security vulnerabilities should not be reported as public issues.** See our [Security Policy](SECURITY.md) for how to report those privately.

### Suggesting Features

Feature requests are welcome! Open an issue describing:

- The problem you're trying to solve
- Your proposed solution (if you have one in mind)
- Any alternatives you've considered

### Submitting Pull Requests

1. **Fork** the repository and create your branch from `main`.
2. **Make your changes.** Keep commits focused and reasonably scoped — smaller, well-described PRs are much easier to review than large ones.
3. **Test your changes** locally to make sure nothing else breaks.
4. **Write a clear PR description** explaining what the change does and why. Link any related issues (e.g. `Fixes #12`).
5. **Submit the PR.** 

All pull requests are reviewed before merging. I'll try to get to reviews in a reasonable time, but as this is a side project, response times may vary. Don't be discouraged if it takes a bit — I'll leave feedback if changes are needed, or merge it if it's good to go.

### Pull Request Guidelines

- Keep changes focused on a single issue or feature where possible.
- Match the existing code style used throughout the project.
- Update the `README.md` if your change affects setup, usage, or supported formats.
- If you're adding support for a new StreamerBot event or SE event mapping, please double-check it against the existing event table in the README for consistency.
- Avoid introducing new third-party dependencies unless necessary — if you do, mention why in your PR description.

## Development Notes

This is a desktop utility for converting StreamElements overlay widgets to run locally with StreamerBot. If you're working on the bridge logic (`streamerBotApiAndEventBridge.js` generation, `SE_API` shims, event listeners, etc.), please test against a real widget end-to-end where possible, since a lot of behavior depends on how StreamElements widgets actually call `window.SE_API` and `fetch()`.

## License

By contributing, you agree that your contributions will be licensed under the same [MIT License](LICENSE.txt) that covers this project.

## Questions?

If anything here is unclear, feel free to open an issue and ask — happy to clarify.
