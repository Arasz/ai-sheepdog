# Project-scope code review — ai-sheepdog (initial review campaign)

> Campaign opened 2026-08-29 by a complete-project-scope-code-review run.
> Base commit under review: `37146e2be24d2ce414febbf6e0840e3742f87380` (main == origin/main, clean tree).
> Status: Phase 0 complete; lanes dispatched. This file is appended to as phases land.

## Phase 0 — Verified ground truth (measured 2026-08-29 at the base commit)

Gates, run from the repo root:

- `dotnet build` → Build succeeded, **0 warnings / 0 errors**, ~1.5s
- `dotnet test` → **1 total, 1 passed, 0 failed, 0 skipped**, 918ms
- `bun run typecheck` (scripts/) → exit 0
- `actionlint` on all three workflows (build.yml, publish.yml, release.yml) → clean
- `dotnet pack src/AiSheepdog/AiSheepdog.csproj -c Release` → `ai-sheepdog.0.1.0.nupkg` created
- GitHub CI: build workflow **success on main** (run 33249457512, 33s). release.yml and publish.yml have **never run**.
- No test carries a `Category` trait; CI runs unfiltered `dotnet test --no-build` — the partition question is moot at this size but is noted as a watch item for when the suite grows.

Size (git ls-files):

- Production C#: **20 lines** — src/AiSheepdog/DefaultCommand.cs (15), src/AiSheepdog/Program.cs (5)
- Test C#: **21 lines** — tests/AiSheepdog.Tests/DefaultCommandTests.cs (one test)
- scripts/build-package.ts 57 lines; docs/research.md 114 lines; README.md 3 lines
- Everything else is framework/config: .ai-badger/** (~550 tracked paths), .claude/**, .github/** mirrors, 3 workflows, build props

Package reality: `dotnet list package` → src references **Spectre.Console.Cli 0.55.0 only**. Microsoft.Extensions.AI is **not referenced anywhere**, despite .ai-badger/config.json:6 and the README claiming "provider-agnostic LLM access via Microsoft.Extensions.AI IChatClient".

Repo state facts: public repo Arasz/ai-sheepdog; 42 committed symlinks (.claude/skills/* and .github/skills/* → ../../.ai-badger/skills/<name>); committed .claude/settings.json.bak-20260829-130537; PackAsTool metadata at src/AiSheepdog/AiSheepdog.csproj:6-10,31 (PackageId ai-sheepdog, ToolCommandName sheepdog, InternalsVisibleTo AiSheepdog.Tests).

Lane roster (derived from the repo's stacks — dotnet, github, ts/node — and its own surfaces):

1. Architecture & skeleton fitness
2. C#/.NET code quality + packaging
3. Test-suite QA
4. Consumer surface & product design (one lane over a 15-line CLI, with the two lenses kept as separate mandated sections)
5. Operations / CI / security
6. Research-record accuracy (docs/research.md claims vs the live world)

Note on isolation: all six lanes are read-only; the project's .ai-badger/delegation.md:29-34 explicitly exempts read-only lanes from worktree isolation. Tree cleanliness is verified after lanes return.
Note on model mix: this harness's delegate_task pins lanes to the session model; per-lane model overrides (opus/sonnet) are not available here — recorded as a campaign limitation, mitigated by keeping lane briefs goal-shaped per the delegation map's reasoning-model guidance.

## Severity calibration inputs (Phase 4, measured before findings arrived)

Production reality of this project — read-only probes, 2026-08-29 ~13:37 UTC+2:

- **nuget.org: 0 hits for packageid:ai-sheepdog** (flat-container BlobNotFound; search totalHits 0) → the tool has no users and cannot break anyone. Loaded, not fired.
- **GitHub: 0 releases, 0 tags.** The release pipeline's expected first effect (tag v0.1.0 from VERSION=0.1.0) never happened.
- **No `production` environment exists in the repo** (`gh api .../environments` → empty). publish.yml:37's `environment: production` gate references nothing; its approval/protection value is currently zero.
- **The repo's only workflow run is a manual `workflow_dispatch` of build** (run 33249457512). The initial push — the one that introduced build.yml, release.yml and VERSION — triggered no workflow (GitHub does not run workflows added in the push that adds them). Both release.yml and publish.yml have never executed.
- **Symlink materialization on a fresh clone (macOS, core.symlinks default): works** — `/tmp/sheepdog-clone/.claude/skills/task` resolves to a directory. Windows-without-symlinks remains a hypothesis for the ops lane.

Calibration posture: there is no live system and no user population. Severity is therefore calibrated against (a) risk to the first real release, and (b) cost of the defect being baked into the harness before any harness code exists. "Loaded, not fired" applies to the entire release/publish path.

## Findings

Integrated from 5 lanes (A=architecture, S=surface+design, Q=test QA, O=ops/CI, R=research accuracy)
plus 3 orchestrator packaging probes (OP). Convergent findings consolidated; provenance in brackets.
Base commit `37146e2`. Grade mix: **28 findings — 24 MEASURED, 4 READ, 0 INFERRED. Severity: 5 HIGH,
9 MEDIUM, 11 LOW, 3 NIT.**

### CLI contract & output

### G1 — The tool's help names the wrong command: `USAGE: AiSheepdog.dll [OPTIONS]` [MEASURED]
**Severity:** HIGH **Provenance:** S3
**Evidence:** installed-shim `sheepdog --help` hex-checked by lane; reproduced via repo build by orchestrator: identical output.
Every help reader is told to type `AiSheepdog.dll`, which exists on no PATH. Fix: `CommandAppSettings.ApplicationName = "sheepdog"` (one line).

### G2 — Unknown flags silently succeed while stray positionals hard-fail, and there is no real `--version` [MEASURED]
**Severity:** HIGH **Provenance:** S1+S2, Q3 (convergent)
**Evidence:** `sheepdog --bogus` → exit 0 + banner (reproduced by orchestrator); `--version` identical bytes to `--bogus`; `--bogus extraarg` → exit 0 both tokens swallowed; `extraarg` → exit 255 "Unknown command". No test pins any of it; no settings type rejects typos.
Flag garbage is indistinguishable from success; `--version` works only by accident of the banner and breaks at the first real subcommand. Needs a ruling before harness code bakes the contract in.

### G3 — Everything, including error output, goes to stdout; stderr is always empty [MEASURED]
**Severity:** MEDIUM **Provenance:** S4
**Evidence:** `sheepdog frobnicate` → exit 255, full error block on stdout, stderr 0 bytes (reproduced by orchestrator).
Scripted consumers cannot separate diagnostics from output. Zero cost now; painful after consumers exist.

### G4 — Usage errors exit 255 with no pointer to `--help` [MEASURED]
**Severity:** LOW **Provenance:** S5
**Evidence:** `sheepdog frobnicate` → 255; caret points at the bad word, no "run --help" hint. 255 vs conventional 1/2 is a shell-handling trap.

### G5 — Help is 4 lines with no description [MEASURED]
**Severity:** LOW **Provenance:** S6
**Evidence:** full help output is exactly 4 lines (USAGE/OPTIONS/-h); no `DESCRIPTION:` despite the csproj carrying one. A `[Description]` attribute is free.

### G6 — Output routes through static `AnsiConsole`, so the suite cannot assert output, and it stays green if printing dies [MEASURED]
**Severity:** HIGH **Provenance:** A6 + Q1/Q2 (convergent; Q proved by mutation)
**Evidence:** Q1: deleting both `MarkupLine` calls keeps the shipped test green (mutation run: 1 failed probe, shipped test passed). Q2: `result.Output` is empty as wired (`ShouldBe("SENTINEL-DUMP")` → `""`); after constructor-injecting `IAnsiConsole`, 2/2 green with output captured. A6 measured the same capture gap independently.
The only shipped test proves the process exits 0 — nothing else. Fix: inject `IAnsiConsole` + assert output; measured to unblock with zero test-side wiring.

### G7 — Banner tagline deviates from the plan's ruled wording (voice ruling needed) [MEASURED]
**Severity:** NIT **Provenance:** S9 — plan line 36 ruled "work in progress"; shipped line adds the sheep joke; passes the repo's own voice rules. Taste ruling, not a fix.

### G8 — The entry command commits to Spectre's sync `Command` base while the domain's first operation is async LLM I/O [MEASURED]
**Severity:** MEDIUM **Provenance:** A1
**Evidence:** `DefaultCommand.cs:6-8`; reflection probe: `AsyncCommand.ExecuteAsync(CommandContext, CancellationToken) -> Task<int>` exists in pinned Spectre.Console.Cli 0.55.0.
First real command must either sync-over-async or migrate the pattern. 2-line change today.

### Release & publish pipeline

### G9 — Publish ships a nupkg with no tag/release linkage check [MEASURED]
**Severity:** MEDIUM **Provenance:** O1
**Evidence:** publish.yml:57-60 pushes every nupkg with `--skip-duplicate`; nothing verifies VERSION at dispatch HEAD has a tag or release (0 tags, 0 releases measured).
A dispatch can publish a version whose release page is empty. Fix: gate on `git ls-remote` tag v$VERSION (or `gh release view`).

### G10 — The `environment: production` gate is illusory: GitHub auto-creates it empty [MEASURED]
**Severity:** MEDIUM **Provenance:** O2
**Evidence:** `gh api .../environments` → `[]` (orchestrator-confirmed in Phase 0); GitHub docs: a workflow referencing a missing environment creates it with no protection. First publish dispatch auto-creates an unprotected gate. Fix: pre-create `production` with a required reviewer.

### G11 — release.yml's tag-exists skip also suppresses release creation [READ]
**Severity:** MEDIUM **Provenance:** O3
**Evidence:** release.yml:48,55 gate both steps on `exists != 'true'`; tag-without-release (deleted release, re-pushed VERSION) yields no release record. Fix: check release existence separately.

### G12 — No branch protection on main [MEASURED]
**Severity:** LOW **Provenance:** O10 — `branches/main/protection` → 404. Mitigations measured: default token read-only, secret scanning + push protection on. Solo-repo tradeoff; a compromised write = tag+release+publish in one path.

### G13 — All workflows float on `ubuntu-latest` vs the repo's own reproducibility rule [READ]
**Severity:** LOW **Provenance:** O4 — strongest case is publish (shipped artifact); `global.json` bounds the SDK. Ruling needed, not effort.

### G14 — NuGet API key passed as a command-line argument [READ]
**Severity:** NIT **Provenance:** O11 — matches NuGet/login's docs, GitHub masks it, but `env:` indirection is the repo's own stated preference. Also `xargs` without `-r` runs once on empty input (fails safe).

### G15 — Dependabot disabled and unconfigured [MEASURED]
**Severity:** LOW **Provenance:** O12 — `dependabot_security_updates: disabled`, no dependabot.yml. Small today (2 runtime deps), free to enable.

### Build, CPM & packaging

### G16 — Two-tier CPM hides the root version table from tests, and the root declares a Spectre.Console pin nothing consumes [MEASURED]
**Severity:** LOW **Provenance:** A2 + OP (convergent)
**Evidence:** A2 probe: nested `tests/Directory.Packages.props` stops the props walk-up — root-defined package in a test project fails `NU1010`. OP probe: `dotnet list package --include-transitive` on src → transitive Spectre.Console resolves **0.55.0**, not the declared 0.57.2 (no top-level reference; transitive pinning disabled) — the pin is dead and the shipped bundle carries 0.55.0 while the root file advertises 0.57.2.
Fix: one root file, two labeled ItemGroups; delete or transitive-pin the Spectre.Console entry.

### G17 — The public package has no icon and no deterministic-build/SourceLink metadata [MEASURED]
**Severity:** LOW **Provenance:** OP
**Evidence:** nuspec inspected: license/readme/repository(+#commit 37146e2) correct; no `<icon>`, no SourceLink props. Structural package layout itself verified correct (`tools/net10.0/any/`, deps.json, DotnetToolSettings.xml). Cosmetic-now, worth one line before first publish.

### Test suite

### G18 — The suite proves only "exits 0": one mutation-tested assertion gap (see G6) [MEASURED]
**Severity:** HIGH **Provenance:** Q1 — retained as the suite-side half of G6; the fix is the same change.

### G19 — The version-projection logic has no test [MEASURED]
**Severity:** LOW **Provenance:** Q5 — `DefaultCommand.cs:10-11` projects assembly version into `Major.Minor.Build`; only the VERSION file regex is gated (ValidateVersionFile), not the rendered banner. One exact-output assertion pins it.

### G20 — InternalsVisibleTo grants access to zero internal members [MEASURED]
**Severity:** NIT **Provenance:** Q4 — grep: no `internal` in src. Dead directive; delete until first internal appears.

### G21 — The first five tests this codebase needs are named and none exist [MEASURED]
**Severity:** MEDIUM **Provenance:** Q6 — banner contains `ai-sheepdog 0.1.0`; exact progress line; `--help` exit 0 with usage; unknown-flag path as ruled; stray-positional path as ruled. Plan input, not a defect.

### Research record (docs/research.md — steers the pending architecture decision)

### G22 — The doc recommends Microsoft.Extensions.AI.Ollama as the current Ollama implementation; it is deprecated, never stable [MEASURED]
**Severity:** HIGH **Provenance:** R7
**Evidence:** NuGet registration: latest 9.7.0-preview with `deprecation.alternatePackage = OllamaSharp`; MS quickstart uses OllamaSharp's `OllamaApiClient` as IChatClient. (Lane's live fetch this session; orchestrator's re-fetch was blocked by an approval gate — riding on the lane's MEASURED grade.)
Replace line 48 with OllamaSharp. Package choice changes if local models are ever added; the IChatClient seam is unchanged.

### G23 — The install block tells readers to `--prerelease` packages that have been stable since April 2026 [MEASURED]
**Severity:** MEDIUM **Provenance:** R12 — `--prerelease` on Foundry resolves to a preview, not the 1.5.0 stable. Stale pre-GA snapshot; correct or date-stamp it.

### G24 — "Semantic Kernel now merged into Agent Framework" is an oversimplification [MEASURED]
**Severity:** MEDIUM **Provenance:** R13 — MS's own FAQ: "direct successor… same teams", SK v1.x continues to be supported. "Successor" is the accurate word; the doc's recommendation (avoid AF for sheepdog) is unaffected.

### G25 — The record has no "not evaluated" section before it steers the architecture decision [READ]
**Severity:** MEDIUM **Provenance:** R18 — the AF-vs-own-harness trade-off, perf/cost, and IChatClient-surface-risk columns are empty; vendor feature lists transcribed without "per vendor" tags. Weakest load-bearing claims named by the lane: G22, G23, G24, and the "both implement IChatClient" wording (below).

### G26 — Prose corrections bundle: five small inaccuracies in docs/research.md [MEASURED]
**Severity:** LOW **Provenance:** R2+R3+R6+R14+R15 (+R17 citation-support note)
(a) components are "distilled from the post", not a verbatim table (post enumerates in prose); (b) the Ralph Loop row conflates it with the separate self-verification component; (c) AzureAIInference is preview-only and wraps Azure.AI.Inference; (d) neither OpenAI package *implements* IChatClient — Microsoft.Extensions.AI.OpenAI wraps both (strengthens the seam claim); (e) BotSharp is a project name, not a package ID; plus: the cited Learn page does not enumerate the five-package roster (verified against nuget.org instead).

### Repo hygiene

### G27 — A tracked `.claude/settings.json.bak-*` has diverged from the live file [MEASURED]
**Severity:** LOW **Provenance:** A5+O9 (convergent) — committed in bootstrap, contents benign (hooks wiring, no secrets), already stale, nothing regenerates it. Delete + ignore pattern.

### G28 — 42 committed skill symlinks break on Windows checkouts [MEASURED]
**Severity:** LOW **Provenance:** O8 — mode 120000; on `core.symlinks=false` they materialize as plain-text files and skill discovery silently breaks; the scaffold's own docs state the tradeoff. Ruling needed: accept or copy-with-header.

## Verified healthy / disproven leads

Recorded so a later simplification pass does not sweep them up, and because disproving a lead is a result:

- `--help` works out of the box on the unconfigured `CommandApp<DefaultCommand>` (S lead a — disproven as a defect).
- Spectre markup degrades correctly: piped output is plain text, zero ANSI bytes, hex-checked (S lead c).
- All five pinned action SHAs match their commented version tags (O lead a — disproven; supply-chain check clean).
- publish.yml's sparse-checkout incantation is runtime-correct for checkout v7 (O lead h — disproven).
- `dotnet test --no-build` works under MTP on a fresh runner — the only CI run exercised exactly that path (O lead i — disproven).
- **ValidateVersionFile gate watched RED**: scratch copy with `VERSION=not-a-version` → build fails with the exact gate error (OP). A gate that has been seen failing can be trusted.
- nupkg structure correct for a RID-agnostic managed tool; repository commit embedded for traceability (OP).
- No over-engineering found: single-project shape is right at 20 lines; no structure carried ahead of need (A leads a/c — disproven; the one wrong-for-domain shape is G8).
- `.slnx` has no CI implication on SDK 10.0.400; bare dotnet build/test discover it (A lead d).
- research.md's load-bearing claims hold: LangChain post real with thesis verbatim (R1); IChatClient seam confirmed verbatim at learn.microsoft.com (R4); AF GA 2026-04-03 confirmed at the official announcement (R10); all three Microsoft.Agents.AI.* package IDs exist at stable versions (R11); M.E.AI.OpenAI "works with any OpenAI-compatible endpoint" confirmed by the package's own description (R5).

## Still open

- Whether nuget.org trusted publishing (OIDC) is registered for Arasz/ai-sheepdog — not verifiable repo-side; first publish dispatch is the test.
- Whether an ai-badger re-scaffold would resurrect the two-tier CPM shape (manifest.json does not list the props files — probably not; untested because the probe writes to the repo).
- IDE-side `.slnx` behavior (Rider/VS) — only the dotnet CLI surface was verified.
- Mechanism of `--bogus extraarg` → exit 0 (behavior measured; parser mechanism not decompiled).
- Windows/older-terminal rendering of TTY escape sequences — untested outside macOS.
- Foundry's stable line (1.5.0) lagging core (1.19.0) — unexplained, likely irrelevant to sheepdog.
- Provenance note: G22/G23's verification rides on the research lane's live fetches; the orchestrator's independent re-fetch was blocked by the approval gate and not retried.

## Owner questions

1. Flag contract (G2): strict unknown-option rejection + explicit `--version`, or pin the swallow-everything contract with tests?
2. Output seam (G6): switch DefaultCommand to injected `IAnsiConsole` now to unblock output tests?
3. Help naming (G1): rule `ApplicationName = "sheepdog"`?
4. Streams/exit codes (G3/G4): errors to stderr; keep Spectre's 255 or normalize to 1/2?
5. Sync vs async (G8): switch to `AsyncCommand` now (2 lines) or migrate per-command later?
6. CPM (G16): merge into one root Directory.Packages.props?
7. Publish gate (G9/G10): tag-linkage check in publish.yml + pre-create the `production` environment with a required reviewer before first publish?
8. Release skip logic (G11): separate release-existence check?
9. README (S7): add the plan-ruled build badge now, NuGet badge at first publish?
10. Package face (S8/G17): is the 3-line README acceptable on nuget.org at first publish; add PackageIcon?
11. Voice (G7): keep the sheep tagline as brand voice, or revert to the plan's plain wording?
12. Research doc (G22/G23/G24/G25): correct the roster + install block, add a "not evaluated" section?
13. Hygiene (G27/G28): delete the .bak; symlinks — accept the Windows limitation or copy-with-header?
14. Reproducibility (G13) and Dependabot (G15): pin publish's runner image; enable Dependabot?

## Severity calibration (final)

No live system, no users, nothing published (Phase 4 inputs above: nuget 0 hits, 0 tags/releases, no
environment, release/publish never executed). Everything is **loaded, not fired**. Consequences:

- No finding is a hotfix. The campaign ranks work by *cost of baking the defect in before the first
  real release and the first harness code*, not by blast radius today.
- HIGH = a contract or record defect that gets more expensive the moment real code lands on top of it
  (G1 help naming, G2 flag contract, G6/G18 untestable output seam, G22 research doc steering an
  architecture decision toward a deprecated package).
- MEDIUM = pre-release hygiene whose cost jumps at a specific trigger: first subcommand (G8), first
  publish dispatch (G9/G10), first release recovery (G11), first consumer script (G3).
- LOW/NIT = cleanup with no trigger; batch them.
- Sequencing constraint surfaced by calibration: fixing G6 (output seam) before ruling G2 (flag
  contract) means writing output assertions against a contract that might change — the rulings
  (owner questions 1-5) are cheap and precede the code.

## Phase 3 — Adversarial verification (run inline; delegation disabled by owner f:)

Procedure: every load-bearing MEASURED claim re-run or re-derived by the orchestrator; arithmetic
recomputed; grade honesty checked. Delegated passes for this phase failed on API timeouts, so the
pass ran in-session against the same sources.

| Claim | Verdict | Evidence for the verdict |
|---|---|---|
| G1 help names `AiSheepdog.dll` | REPRODUCED | orchestrator re-run, identical output; fix API `CommandAppSettings.ApplicationName` + `CommandAppSettings` confirmed present in pinned Spectre.Console.Cli 0.55.0 (DLL byte probe) |
| G2 flag matrix (`--bogus`/`--version`/`--bogus extraarg`/`frobnicate`) | REPRODUCED | orchestrator re-run: exit 0/0/0/255, stderr 0 bytes |
| G3 errors on stdout only | REPRODUCED | same re-run |
| G4 exit 255 no help hint | REPRODUCED | same re-run |
| G5 help is 4 lines | REPRODUCED | same re-run |
| G6/G18 suite blind to output (mutation) | REPRODUCED | fresh `git archive 37146e2` copy, both MarkupLine calls deleted → `dotnet test`: total 1, succeeded 1, failed 0 — shipped test stays green |
| G7 tagline deviates from plan line 36 | REPRODUCED | plan text vs DefaultCommand.cs:12 |
| G8 sync `Command` base; `AsyncCommand` exists | REPRODUCED | source read + `AsyncCommand`/`ExecuteAsync` present in DLL bytes |
| G9 publish has no tag linkage check | REPRODUCED | publish.yml:57-60 text; 0 tags/0 releases (gh api) |
| G10 `production` environment auto-created empty | REPRODUCED | `gh api .../environments` → `[]` |
| G11 release skip suppresses release creation | REPRODUCED | release.yml:48,55 text |
| G12 no branch protection on main | REPRODUCED | `gh api .../branches/main/protection` → 404 "Branch not protected" |
| G13 floating `ubuntu-latest` | REPRODUCED | yml text, all three workflows |
| G14 API key as command-line argument | REPRODUCED | publish.yml:60 text |
| G15 Dependabot disabled | REPRODUCED | `gh api repos/...` → `dependabot_security_updates: disabled` |
| G16 two-tier CPM + dead Spectre.Console 0.57.2 pin | REPRODUCED | `dotnet list package --include-transitive` → transitive Spectre.Console 0.55.0; NU1010 probe (A2) witnessed failing |
| G17 no icon / no SourceLink | REPRODUCED | nuspec inspection |
| G19 version projection untested | REPRODUCED | the one test asserts exit code only |
| G20 InternalsVisibleTo dead | REPRODUCED | grep: no `internal` in src |
| G21 five named tests missing | REPRODUCED | behaviors measured this campaign |
| G22 Ollama package deprecated → OllamaSharp | STANDS on lane's live fetch | NuGet registration deprecation metadata quoted by lane; orchestrator re-fetch blocked by approval gate — provenance note retained |
| G23 stale `--prerelease` install block | STANDS on lane's live fetch | same basis |
| G24 "merged into" → "successor" | STANDS on lane's live fetch | MS FAQ quotes verbatim in lane report |
| G25 no "not evaluated" section | REPRODUCED | docs/research.md structure read in full |
| G26 prose bundle (a)-(e) | STANDS on lane's live fetches | verbatim source quotes in lane report |
| G27 .bak diverged from live settings | REPRODUCED | diff: live has 17 lines the .bak lacks |
| G28 42 committed symlinks | REPRODUCED | `git ls-files -s \| grep -c 120000` → 42 |
| Header arithmetic | CORRECTED during assembly | first written 22/5/1 grades + 10 LOW/4 NIT; recomputed → 24 MEASURED / 4 READ / 0 INFERRED, 11 LOW / 3 NIT; header fixed before publication; recounted post-fix: matches |
| Provenance honesty note (G22/G23) | ACCURATE | the block is real; finding strengths stated correctly |
| Healthy/disproven section | SPOT RE-DERIVED | ValidateVersionFile RED witnessed; nupkg internals inspected; `ApplicationName` fix feasibility confirmed; remaining rows ride on lanes' MEASURED probes with commands quoted |

**Result: 0 claims refuted, 0 softened; 23 reproduced by the orchestrator, 5 standing on the
research lane's cited live fetches; 1 self-caught arithmetic correction (already applied). The
record's core conclusions survive adversarial re-derivation.**

## Plan revision v2 — what the two plan reviews changed

Both reviews ran inline (same f: constraint). Architect pass verdict: REVISE — edits below applied.
Gate-audit verdict: GATE-HONEST-AFTER-EDITS — edits below applied.

1. **MUST — coverage hole closed:** G17 (package icon / SourceLink) was in no work package; added
   to WP3 (build/packaging surface).
2. **MUST — WP1 gate made executable:** "tests written FIRST watched red" was only true for tests
   pinning defect behavior. Reworded to the mutation-witnessed pattern: (a) witnessed RED already
   exists (mutation: prints deleted → shipped test green — reproduced twice this campaign);
   (b) land the IAnsiConsole seam; (c) add output tests (green); (d) re-run the mutation → must go
   RED. Strict-contract tests (unknown flag, if ruled strict) are watched red pre-fix as before.
3. **SHOULD — WP2 gate de-theatred:** scratch-workflow dry-run replaced by testing the linkage-check
   logic as a local script against `gh api`: RED against a tagless VERSION, GREEN once v0.1.0 is
   tagged. The workflow wrapper stays actionlint-gated; first real dispatch remains declared
   residual risk (UNVERIFIED until then).
4. **SHOULD — WP4 gate concretized:** "re-verified at edit time" now requires a citation table
   appended to docs/research.md (claim → source → checked date) as the checkable artifact.
5. **SHOULD — WP5 given minimal gates:** absence grep for the .bak path; badge URL returns 200;
   dependabot config validated after push.
6. **SHOULD — WP1 unblocked:** seam work (G6, G1, G8, G20) is ruling-independent and starts
   immediately; only the flag-contract and stderr tests wait on rulings 1/4. The calibration's
   sequencing constraint is honored by test placement, not by blocking the whole package.
7. **SHOULD — execution shape fixed:** all five WPs run sequentially in one session on the campaign
   branch, one commit per WP — builds take 1.5s, worktree isolation buys nothing at this size, and
   delegation is disabled. No shared files between WPs (verified by walking each WP's file set);
   the only serialization point is the branch itself.
8. Deferral accepted by both reviews: module before/after diagrams stay deferred (20 lines, no
   structural change proposed); voice (G7) is a ruling, its 1-line edit (if reverted) belongs to WP1.

## Plan (v2 — reviewed)

Work packages grouped by surface; everything touching one file lands in one package. Each names its
gate, all of which must be watched red before trusted. Rulings marked ⏸ are owner questions the
implementation may not start ahead of; defaults below are the recorded recommendation and are
reversible.

**WP1 — CLI contract & output seam** (DefaultCommand.cs, Program.cs, DefaultCommandTests.cs)
- Ruling-independent seam work starts immediately: IAnsiConsole constructor injection (G6),
  ApplicationName="ai-sheepdog" (G1, D3 — owner note: app name is ai-sheepdog; the installed
  command stays `sheepdog`), switch to `AsyncCommand` (G8), delete InternalsVisibleTo (G20).
- Waits on rulings: none remaining — D1/D4 closed 14/14 APPROVE (strict flag contract; errors to
  stderr **and** through a logger, D4 note).
- Then: flag contract per D1 (G2), stderr routing + logging seam per D4 (G3; [LoggerMessage]
  pattern when the logging package lands), `[Description]` (G5), banner format (G19), the five
  named tests (G21, Q6).
- Gate (mutation-witnessed pattern): witnessed RED already exists — the mutation (both MarkupLine
  calls deleted, shipped test stays green) was reproduced twice this campaign. Sequence: land the
  seam → add the output tests (green) → re-run the mutation → the new suite must go RED.
  Strict-contract tests (unknown flag, if ruled strict) are written against current behavior first
  and watched red pre-fix. Run the CLI; hex-verify stderr now carries errors.

**WP2 — Release/publish hardening** (.github/workflows/publish.yml, release.yml)
- Rulings: ⏸ none — these are the plan's own recommendations, recorded and reversible.
- tag-linkage check before nuget push (G9), separate release-existence check in release.yml (G11),
  api-key via env: indirection + `xargs -r` (G14), ⏸ pin publish runner to ubuntu-24.04 (G13, rec:
  yes), ⏸ pre-create `production` environment with required reviewer (G10 — repo-settings action,
  not a file change).
- Gate: the linkage-check logic is tested as a local script against `gh api` — watched RED against a
  tagless VERSION, GREEN once v0.1.0 is tagged. Workflow wrapper stays actionlint-gated; the first
  real dispatch is declared residual risk and stays UNVERIFIED until it runs.

**WP3 — Build/CPM simplification** (Directory.Packages.props, tests/Directory.Packages.props → merged; AiSheepdog.csproj)
- One root props file, two labeled ItemGroups (G16) — the test group carries only packages used by
  tests (D7 note); delete the dead Spectre.Console 0.57.2 pin or transitive-pin deliberately (G16,
  second half); add PackageIcon + deterministic-build/SourceLink metadata (G17 — folded in by plan
  review 1).
- Gate: the NU1010 probe in reverse — after the merge, a scratch test referencing a root-defined
  package resolves (watched: it fails NU1010 against the current two-tier shape); the existing suite
  stays green; `dotnet list package --include-transitive` shows the intended Spectre.Console
  resolution; the packed nuspec carries the icon reference.

**WP4 — Research record correction** (docs/research.md)
- OllamaSharp replacement (G22), install block date-stamp/correction (G23), "successor" wording
  (G24), prose bundle G26 (a)-(e) + roster citation, new "Not evaluated" section (G25).
- Gate: a citation table appended to docs/research.md (claim → source URL → checked date) is the
  checkable artifact; every changed claim re-verified against its source at edit time; the "Not
  evaluated" section names AF-vs-own-harness trade-offs explicitly and marks vendor claims "per
  vendor".

**WP5 — Hygiene batch** (.claude/settings.json.bak → delete; README badge per ⏸ S7 ruling rec: build
badge now, NuGet badge at first publish; ⏸ G28 symlink shape rec: accept, revisit if Windows users
appear; G15 Dependabot rec: enable; G12 branch protection rec: leave, solo repo, revisit at first
external contributor)
- Gate (minimal, per gate audit): absence grep for the .bak path; badge URL returns 200; dependabot
  config validated after push.

**Sequencing (v2).** All five WPs run sequentially in one session on the campaign branch, one commit
per WP — builds take 1.5s, worktree isolation buys nothing at this size, and delegation is disabled
(owner f:). No two packages share a file (verified by walking each WP's file set); the only
serialization point is the branch itself. WP4 is independent and can run first if the architecture
decision is imminent. Version marker: no bump (nothing released; VERSION stays 0.1.0 until the first
real release).

**Module before/after.** At 20 production lines the module shape does not change: single project,
one command. The only structural movement is inside DefaultCommand (sync→async base, static→injected
console) and the props merge. Drawings deferred to implementation; no architectural restructure is
proposed — G6's injection is the seam that will let the harness's future output be tested at all.

## Owner gate — CLOSED 2026-08-29, 14/14 APPROVE

Form: docs/work/2026-08-29-project-scope-review.html · feedback: docs/work/2026-08-29-project-scope-review-feedback.md
(end marker verified, nothing unanswered). Rulings and what each changes:

| ID | Verdict | Note | Consequence |
|---|---|---|---|
| D1 | APPROVE | — | strict unknown-option rejection + explicit `--version` into WP1 |
| D2 | APPROVE | — | IAnsiConsole constructor injection into WP1 |
| D3 | APPROVE | "App name is ai-sheepdog" | **amended**: ApplicationName = "ai-sheepdog"; installed command stays `sheepdog` (plan-ruled ToolCommandName) — help shows the package name |
| D4 | APPROVE | "also we will use the logger for errors" | **extended**: errors → stderr **and** through a logging seam in WP1 ([LoggerMessage] pattern when the logging package lands) |
| D5 | APPROVE | — | AsyncCommand base into WP1 |
| D6 | APPROVE | — | sheep tagline stays |
| D7 | APPROVE | "in tests — only packages used by tests" | **constraint**: merged CPM's test group carries only test-used packages |
| D8 | APPROVE | — | build badge now, NuGet badge at first publish |
| D9 | APPROVE | — | 3-line README acceptable at first publish; PackageIcon/SourceLink with WP3 |
| D10 | APPROVE | — | delete .bak; accept symlinks |
| D11 | APPROVE | — | publish runner pinned ubuntu-24.04; Dependabot on; branch protection stays off |
| D12 | APPROVE | — | publish tag-linkage check; pre-create `production` environment with required reviewer |
| D13 | APPROVE | — | release-existence check separate from tag check |
| D14 | APPROVE | — | research.md corrections + Not-evaluated section before P0 harness work — **corrections verified against current Microsoft Learn docs 2026-08-29**: IChatClient seam verbatim (learn.microsoft.com/dotnet/ai/microsoft-extensions-ai); Ollama quickstart instructs `dotnet add package OllamaSharp` with `new OllamaApiClient(...)` as IChatClient and never mentions the deprecated package; AF overview verbatim: "The Agent Framework is the direct successor, created by the same teams". SK-v1.x-support wording stands on the lane's devblogs FAQ fetch (SK Learn page renders no strippable body text). |

## Follow-up — workflow trigger gap (RESOLVED 2026-08-29 ~14:35 UTC+2)

**Finding:** build.yml never fired from a `push` or `pull_request` event on this repo — 0 such runs
in history including the bootstrap push; only `workflow_dispatch` worked. Merges #1/#2/#3 landed
without a build run; the red-main incident was caught by manual dispatch, not CI.

**Fix:** an explicit API rewrite of the repo's Actions permissions
(`PUT /actions/permissions {"enabled":true,"allowed_actions":"all"}`) — after which the very next
push fired build (and every push since). Eliminated along the way (each verified live): workflow
state/registration, trigger parsing, auth path (SSH vs HTTPS), deploy keys, GitHub status, account
scope (ai-badger fired fine throughout). Evidence shape during the outage: PushEvent arrived and
other apps' check suites (vercel, claude) were created — the Actions app's never was. Stale PR
events never retro-fired; only events after the rewrite trigger.

**Verified post-fix:** push → 2/2 runs green (incl. the merge of #7); fresh PR #8 → its own
pull_request run green (1 check on the rollup). PR #8 is left open as the standing evidence.
Interim dispatch-per-branch discipline is retired.
