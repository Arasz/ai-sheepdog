# Research: Dotnet LLM Interaction Packages & Agent Harness Anatomy

> Sources gathered 2026-08-29. Treat version numbers as current-at-time-of-writing.

## 1. The Anatomy of an Agent Harness (LangChain, March 2026)

**Source:** [blog.langchain.com/the-anatomy-of-an-agent-harness](https://blog.langchain.com/the-anatomy-of-an-agent-harness/)

**Core thesis:** `Agent = Model + Harness`. The model provides reasoning; the harness provides
everything the model needs to *act* on that reasoning — orchestration, tools, context, memory,
guardrails.

### Minimum harness components (distilled from the post)

The post enumerates harness capabilities in prose bullets (system prompts; tools, skills, MCPs;
bundled infrastructure; orchestration logic; hooks/middleware) — the table below is this project's
distillation, not the post's own wording.

| Layer | What it does |
|-------|-------------|
| **Agent loop** | Iterates: send prompt → model responds (text or tool call) → if tool call, execute and feed result back → repeat until done |
| **Tool registry & execution** | Register callable tools (functions, APIs, shell); parse tool-call syntax from model output; execute and return results |
| **Hooks / middleware** | Intercept model calls and tool calls to modify behavior (guardrails, logging, retries, redaction) |
| **Context management** | Windowing, summarization, offloading to files to stay within token limits |
| **Persistent state / memory** | Filesystem or DB that survives across turns — the agent's "working memory" |
| **Subagent spawning** | Spin up isolated child agents for independent subtasks, each with own context |
| **Self-verification / eval loops** | Prompt the model to self-evaluate or run tests after tool use; loop back on failure |
| **The "Ralph Loop" pattern** | A harness pattern that intercepts the model's exit attempt via a hook and reinjects the original prompt in a clean context window, forcing the agent to continue work against a completion goal |
| **Progressive skill loading** | Load capabilities only when relevant ("progressive disclosure" in the post's wording) — reduces prompt bloat |

### Key insight for ai-sheepdog

ai-sheepdog's harness needs *at minimum*: an agent loop, a tool registry, context management,
and persistent state. Hooks, subagents, and progressive skills are the next layers up.

---

## 2. .NET LLM Interaction Packages (August 2026)

### Microsoft.Extensions.AI (the foundation)

**Source:** [learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)

- **Microsoft.Extensions.AI.Abstractions** — Core exchange types: `IChatClient`, `IEmbeddingGenerator<TInput,TEmbedding>`. Any .NET LLM client can implement these interfaces.
- **Microsoft.Extensions.AI.OpenAI** — Implementation for OpenAI-compatible endpoints ("Implementation of generative AI abstractions for OpenAI-compatible endpoints" per the package description)
- **Microsoft.Extensions.AI.AzureAIInference** — *(preview-only, never went stable; wraps Azure.AI.Inference — the Models/Inference API, not Azure OpenAI)*
- **OllamaSharp** — Ollama implementation of `IChatClient` (successor to the deprecated, never-stable Microsoft.Extensions.AI.Ollama; the official MS Learn quickstart for local models uses `new OllamaApiClient(...)` from OllamaSharp)
- **Microsoft.Extensions.AI.Evaluation** — Core abstractions for evaluation; the LLM-judged evaluators (Relevance, Truth, Completeness, …) live in the sibling **Microsoft.Extensions.AI.Evaluation.Quality** package

**This is the unification layer.** Build against `IChatClient` and swap providers at will.
(The cited Learn page documents the abstractions; the package roster above is verified against
nuget.org — see the citation table at the bottom.)

### Microsoft Agent Framework (GA April 3, 2026)

**Source:** [learn.microsoft.com/en-us/agent-framework/overview](https://learn.microsoft.com/en-us/agent-framework/overview/) · [1.0 announcement, 2026-04-03](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/)

- GA April 3, 2026 (per the official 1.0 announcement) as the successor to Semantic Kernel and
  AutoGen — Microsoft's own wording: "The Agent Framework is the direct successor, created by the
  same teams." Semantic Kernel is **not** absorbed: SK v1.x remains supported for the foreseeable
  future (critical bugs, security), with new investment going to Agent Framework.
- `Microsoft.Agents.AI` — Core agent abstractions (stable)
- `Microsoft.Agents.AI.OpenAI` — OpenAI-backed agent (stable)
- `Microsoft.Agents.AI.Foundry` — Foundry Agents support (stable line lags the core: 1.5.0 vs 1.19.0)
- Supports: tool calling, multi-agent orchestration, RAG, CodeAct

```
dotnet add package Microsoft.Agents.AI
dotnet add package Microsoft.Agents.AI.OpenAI
dotnet add package Microsoft.Agents.AI.Foundry
```

(Stable since the April 2026 GA; `--prerelease` is only needed for pre-GA snapshots of this note.)

### Semantic Kernel (predecessor of Agent Framework, still supported)

- Was the main .NET LLM orchestration framework for years
- Agent Framework is its direct successor; SK v1.x continues to ship and be supported
- Microsoft recommends Agent Framework for new projects

### OpenAI Official SDK for .NET

- `Azure.AI.OpenAI` — Azure OpenAI's official extension package for OpenAI's .NET library
- `OpenAI` — the official .NET library for the OpenAI service API
- Neither implements `IChatClient` itself; **Microsoft.Extensions.AI.OpenAI wraps both** (Azure.AI.OpenAI extends the OpenAI library), so one wrapper covers both.

### Community / Third-Party

| Package | Notes |
|---------|-------|
| **LlmTornado** | 30+ LLM connectors, agent framework, MCP, A2A integration *(per vendor README — not independently verified)* |
| **LM-Kit.NET** | Enterprise AI toolkit, tool calling, RAG *(per vendor description)* |
| **BotSharp** (project; packages ship as `BotSharp.*`, e.g. BotSharp.Core) | Multi-agent framework, NLP focus |
| **LLamaSharp** | Local GGUF model inference via llama.cpp |

### For ai-sheepdog

The recommended stack:
1. **Microsoft.Extensions.AI.Abstractions** — `IChatClient` as the provider-agnostic seam
2. **Microsoft.Extensions.AI.OpenAI** — default provider (works with any OpenAI-compatible endpoint)
3. **OllamaSharp** — local-model provider (implements `IChatClient` directly)
4. **Spectre.Console.Cli** — CLI framework (already in the skeleton)
5. Avoid Agent Framework initially — ai-sheepdog should be the *harness*, not consume one

---

## 3. Decision: What ai-sheepdog should build

Per the LangChain anatomy, ai-sheepdog needs:

| Priority | Component | Status |
|----------|-----------|--------|
| P0 | Agent loop (prompt → model → tool call → execute → repeat) | **TODO** |
| P0 | Tool registry (register + execute functions) | **TODO** |
| P0 | IChatClient integration (Microsoft.Extensions.AI) | **TODO** |
| P1 | Context management (windowing, file offload) | Future |
| P1 | Persistent state / memory | Future |
| P2 | Hooks / middleware | Future |
| P2 | Subagent spawning | Future |
| P2 | Progressive skill loading | Future |

## 4. Not evaluated

Recorded so the recommendation above is read at its true strength. None of the following was
measured; the recommendation rests on architecture fit, not on evaluation:

- **Agent Framework vs own harness** — no trade-off analysis (time-to-first-loop, maintenance
  burden, escape hatches when the framework's abstractions fight back). The "avoid AF" call is a
  design argument (the product *is* a harness), not a measured one.
- **Performance / cost** — no latency, throughput, or token-cost comparisons between providers,
  and none between raw HTTP clients and `IChatClient` middleware overhead.
- **IChatClient surface risk** — no spike confirming the interface covers tool calling,
  streaming, and multi-turn state for the target providers; the fallback (wrapping a provider
  SDK directly) is untested.
- **Community packages** — LlmTornado/LM-Kit feature lists are vendor claims, never exercised.
- **Embeddings** — `IEmbeddingGenerator` was noted, not evaluated; the P0 loop needs only chat.

## Citation table

| Claim | Source | Checked |
|-------|--------|---------|
| LangChain post exists; "Agent = Model + Harness" verbatim; March 2026 | blog.langchain.com/the-anatomy-of-an-agent-harness (HTTP 200, fetched 2026-08-29) | 2026-08-29 |
| Harness components = prose bullets, not a table | same fetch | 2026-08-29 |
| "Ralph Loop" is the exit-interception pattern, distinct from self-verification | same fetch | 2026-08-29 |
| `IChatClient`/`IEmbeddingGenerator` as core exchange types (verbatim) | learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai | 2026-08-29 |
| M.E.AI.OpenAI = "OpenAI-compatible endpoints" | nuget.org registration API (10.9.0 stable) | 2026-08-29 |
| AzureAIInference preview-only, wraps Azure.AI.Inference | nuget.org flat-container (latest 10.0.0-preview) | 2026-08-29 |
| M.E.AI.Ollama deprecated → OllamaSharp; OllamaSharp implements IChatClient | nuget.org registration (deprecation.alternatePackage) + learn quickstart chat-local-model (`dotnet add package OllamaSharp`, `new OllamaApiClient(...)`) | 2026-08-29 |
| Evaluation abstractions vs .Quality evaluators | nuget.org descriptions (10.9.0) | 2026-08-29 |
| AF GA April 3, 2026 | devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0 (datePublished 2026-04-03) | 2026-08-29 |
| AF = "direct successor" of SK + AutoGen; SK v1.x supported | learn AF overview (verbatim) + devblogs SK/AF FAQ (verbatim) | 2026-08-29 |
| Microsoft.Agents.AI / .OpenAI / .Foundry stable | nuget.org flat-container (1.19.0 / 1.19.0 / 1.5.0) | 2026-08-29 |
| OpenAI packages do not implement IChatClient; M.E.AI.OpenAI wraps both | nuget.org descriptions (OpenAI 2.13.0, Azure.AI.OpenAI 2.1.0) | 2026-08-29 |
| BotSharp ships as BotSharp.* packages | nuget.org (BotSharp.Core 5.2.0) | 2026-08-29 |
| Spectre.Console.Cli 0.55.0 in the skeleton | src/AiSheepdog/AiSheepdog.csproj + Directory.Packages.props | 2026-08-29 |
