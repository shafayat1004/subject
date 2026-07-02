# Welcome to EggShell

EggShell is a full-stack **F#** application framework. Both the backend and the frontend are written in
F#, and the domain types are shared between them:

* **F#** — a functional-first language with a powerful type system.
* **Fable** — an F# to JavaScript compiler (currently Fable 5, on the .NET 10 SDK).
* **react-native-web** — the frontend renders through React Native for Web (web) and React Native
  (native). EggShell recently migrated the primitive layer off the archived `@chaldal/reactxp` fork;
  see [ReactXP to RNW](./modernization/reactxp-to-rnw.md).
* **Orleans** — the backend is a set of state-machine "lifecycles" hosted as Microsoft Orleans grains.

**UI code:** new work uses pure F# components (`[<Component>]`). The old RenderDSL is retired; see
[Legacy](./legacy/index.md).

> **For LLMs / tools:** a machine-readable index of this documentation lives at
> [`/docs/llms.txt`](./llms.txt) (the [llmstxt.org](https://llmstxt.org) convention).
>
> **Changing code or docs?** [Keeping Code and Docs in Sync](./maintaining-docs.md) is the single map of
> which docs to read and update for any change, and how to keep the docs consistent.

## Documentation map

* [Architecture](./architecture/index.md) — how the framework works: lifecycles, Orleans, shared types
  and codecs, the test harness, the frontend, the toolchain.
* [Modernization & Current Status](./modernization/index.md) — the authoritative status dashboard, goals,
  the phased plan, and the ReactXP to react-native-web migration.
* [Runbooks](./runbooks/index.md) — follow-it-literally procedures: the dev loop, per-platform
  debugging, the audit toolkit, troubleshooting, and migration execution.
* [Accessibility](./accessibility/index.md) — the mandatory, default-on accessibility model: principles,
  the full spectrum, recipes, and the backlog.
* [Knowledge Base](./knowledge-base/index.md) — the engineering log, app-structure conventions, and the
  dependency baseline.
* [Subjects](./subject/index.md) — the backend lifecycle programming guide.

## Getting started

* [Getting Started](./basics/getting-started.md)
* [Dev Experience](./basics/dev-experience.md)
* [Components](./fsharp/component.md)
* [Styling](./fsharp/styling.md)
* [Themeing](./fsharp/themeing.md)
* [Formatting](./fsharp/formatting.md)
* [Legacy Interop](./fsharp/legacy.md)
* [What are all these libraries?](./basics/libraries.md)
* [Native Development](./basics/native.md)

## Tools

* [Intro](./tools/index.md)
* [The eggshell CLI](./tools/cli.md)
* [Snippets](./tools/snippets.md)

## How To

* [Intro](./how-to/index.md)
* [FAQ, aka "How do I do this in EggShell?"](./how-to/faq.md)
* [Where do I find examples — existing projects](./how-to/projects.md)
* [Responsive components](./how-to/responsive.md)
* [Scrolling](./how-to/scrolling.md)

## Housekeeping

* [Change log](./basics/changelog.md)
* [Roadmap](./modernization/goals-and-roadmap.md)

## Unsorted

* [Background Info and Design Decisions](./unsorted/background.md)
* [Understanding icons](./unsorted/icons.md)
* [Notes on various component types](./unsorted/component-design.md)
* [EggShell-specific F# Good Coding Practices](./unsorted/eggshell-specific-fsharp-good-practices.md)
* [XmlDocs](./unsorted/xmldocs.md)

## Legacy

* [Legacy Introduction](./legacy/index.md)
