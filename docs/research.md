# Research: Dotnet LLM Interaction Packages & Agent Harness Anatomy

> Sources gathered 2026-08-29. Treat version numbers as current-at-time-of-writing.

## 1. The Anatomy of an Agent Harness (LangChain, March 2026)

**Source:** [blog.langchain.com/the-anatomy-of-an-agent-harness](https://blog.langchain.com/the-anatomy-of-an-agent-harness/)

**Core thesis:** `Agent = Model + Harness`. The model provides reasoning; the harness provides
everything the model needs to *act* on that reasoning — orchestration, tools, context, memory,
guardrails.

### Minimum harness components

| Layer | What it does |
|-------|-------------|
| **Agent loop** | Iterates: send prompt → model responds (text or tool call) → if tool call, execute and feed result back → repeat until done |
| **Tool registry & execution** | Register callable tools (functions, APIs, shell); parse tool-call syntax from model output; execute and return results |
| **Hooks / middleware** | Intercept model calls and tool calls to modify behavior (guardrails, logging, retries, redaction) |
| **Context management** | Windowing, summarization, offloading to files to stay within token limits |
| **Persistent state / memory** | Filesystem or DB that survives across turns — the agent's "working memory" |
| **Subagent spawning** | Spin up isolated child agents for independent subtasks, each with own context |
| **Verification / eval loops** | Run tests or self-evaluate after tool use; loop back on failure ("Ralph Loop") |
| **Progressive skill loading** | Load capabilities only when relevant (not all at once) — reduces prompt bloat |

### The "Ralph Loop" pattern

Intercepts the model's exit attempt via a hook and reinjects the original prompt in a clean
context window, forcing the agent to continue work against a completion goal. Prevents premature
termination.

### Key insight for ai-sheepdog

ai-sheepdog's harness needs *at minimum*: an agent loop, a tool registry, context management,
and persistent state. Hooks, subagents, and progressive skills are the next layers up.

---

## 2. .NET LLM Interaction Packages (August 2026)

### Microsoft.Extensions.AI (the foundation)

**Source:** [learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)

- **Microsoft.Extensions.AI.Abstractions** — Core exchange types: `IChatClient`, `IEmbeddingGenerator<TInput,TEmbedding>`. Any .NET LLM client can implement these interfaces.
- **Microsoft.Extensions.AI.OpenAI** — OpenAI implementation of `IChatClient`
- **Microsoft.Extensions.AI.AzureAIInference** — Azure AI implementation
- **Microsoft.Extensions.AI.Ollama** — Ollama implementation
- **Microsoft.Extensions.AI.Evaluation** — LLM-as-judge evaluation framework

**This is the unification layer.** Build against `IChatClient` and swap providers at will.

### Microsoft Agent Framework (April 2026 GA)

**Source:** [learn.microsoft.com/en-us/agent-framework/overview](https://learn.microsoft.com/en-us/agent-framework/overview/)

- Shipped April 3, 2026 as the production-ready unification of Semantic Kernel + AutoGen
- `Microsoft.Agents.AI` — Core agent abstractions
- `Microsoft.Agents.AI.OpenAI` — OpenAI-backed agent
- `Microsoft.Agents.AI.Foundry` — Azure AI Foundry integration
- Supports: tool calling, multi-agent orchestration, RAG, CodeAct

**Packages (preview as of Jan 2026, GA April 2026):**
```
dotnet add package Microsoft.Agents.AI --prerelease
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
dotnet add package Microsoft.Agents.AI.Foundry --prerelease
```

### Semantic Kernel (legacy, now merged into Agent Framework)

- Was the main .NET LLM orchestration framework for years
- Now folded into Microsoft Agent Framework
- Still usable standalone but Microsoft recommends Agent Framework for new projects

### OpenAI Official SDK for .NET

- `Azure.AI.OpenAI` — Azure OpenAI client
- `OpenAI` — direct OpenAI API client (community/official)
- Both implement or can be wrapped with `IChatClient`

### Community / Third-Party

| Package | Notes |
|---------|-------|
| **LlmTornado** | 30+ LLM connectors, agent framework, MCP, A2A integration |
| **LM-Kit.NET** | Enterprise AI toolkit, tool calling, RAG |
| **BotSharp** | Multi-agent framework, NLP focus |
| **LLamaSharp** | Local GGUF model inference via llama.cpp |

### For ai-sheepdog

The recommended stack:
1. **Microsoft.Extensions.AI.Abstractions** — `IChatClient` as the provider-agnostic seam
2. **Microsoft.Extensions.AI.OpenAI** — default provider (works with any OpenAI-compatible endpoint)
3. **Spectre.Console.Cli** — CLI framework (already in the skeleton)
4. Avoid Agent Framework initially — ai-sheepdog should be the *harness*, not consume one

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
