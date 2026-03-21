# Agent Design Chat — Token Reduction, Memory, Procedural Memory & Vision Models

---

## Q1: Reducing Token Usage for Browser Agent Web Content

**Options to reduce token usage for agent interactions with web content:**

### Deterministic / Rule-Based Approaches

- **HTML stripping/cleaning** — Strip all tags, scripts, styles, and boilerplate before passing web content to the agent. Libraries: `HtmlAgilityPack` (C#) or `BeautifulSoup`/`trafilatura` (Python). `trafilatura` is excellent at extracting article/main body text with minimal noise.
- **CSS selectors / XPath targeting** — Have the agent emit a selector (e.g. `#main-content p`) and resolve it deterministically, only sending matched nodes upstream. Browser MCP tools usually support this.
- **Readability algorithms** — Mozilla's Readability (or Python port `readabilipy`) deterministically extracts clean article content. Works well for editorial/blog content.
- **DOM snapshots vs full HTML** — Use the accessibility tree or simplified DOM snapshot instead of raw HTML. Browser MCP often exposes `get_accessibility_tree` — dramatically smaller than raw HTML and already structured.
- **Chunking with retrieval** — Chunk page content, embed locally (e.g. `sentence-transformers`), only pass relevant chunks to the main model. Fully deterministic retrieval step.

### Lightweight "Stupid" Model Approach

Pattern:
1. Main model decides it needs info from a URL
2. Emits structured question: `{ "url": "...", "question": "What is the return policy?" }`
3. Small local model (e.g. `phi-3-mini`, `qwen2.5-1.5b`, `smollm`) receives stripped HTML + question and returns just the answer
4. Answer goes back to main model

Good local model options: **Phi-3 Mini**, **Qwen2.5-1.5B/3B**, **Gemma 2 2B** — served via `ollama` or `llama.cpp`. C# via `OllamaSharp`, Python via `ollama` SDK.

### Hybrid Architecture (Recommended)

```
Browser MCP → Raw HTML
    → trafilatura/Readability strip (deterministic)
    → Accessibility tree or chunked text
    → If targeted extraction: CSS selector (deterministic)
    → If open-ended Q&A over content: small local model
    → Answer injected into main model context
```

### Other Token-Reduction Tricks

- **Cache page content** — Cache extracted/summarized content per URL per session
- **Summarize once, reference repeatedly** — Small model produces a one-time summary stored in agent memory
- **Structured extraction prompts** — Use schemas like `{ extract: ["price", "availability", "return_policy"] }` for reliable, shorter outputs
- **Browser MCP `snapshot` vs `screenshot`** — Prefer text-based snapshots to avoid vision model token costs

> Deterministic preprocessing alone (trafilatura + accessibility tree) will likely cut token usage by 70-90%.

---

## Q2: Agent Memory Design

### The Four Memory Types

**Working Memory (In-Context)**
What's in the current context window. Ephemeral, expensive, limited. Goal of everything else is to minimize what lives here.

**Episodic Memory (What happened)**
Log of past interactions, actions taken, and outcomes. Stored externally (vector DB, SQLite) and fetched semantically or by recency.

**Semantic Memory (What it knows)**
Facts, summaries, and learned knowledge — e.g. "user prefers C#", "this site's checkout button is always in `#cart-confirm`". Distilled conclusions rather than raw events.

**Procedural Memory (How to do things)**
Reusable plans, tool usage patterns, prompt templates the agent has learned work well for certain task types.

### Practical Architecture

```
┌─────────────────────────────────────┐
│           Active Context            │  ← Working memory (small, curated)
│  System prompt + current task +     │
│  retrieved memories + recent turns  │
└─────────────────────────────────────┘
           ↑ retrieve          ↓ write
┌─────────────────────────────────────┐
│          Memory Store               │
│                                     │
│  Episodic:  vector DB (chroma,      │
│             pgvector, qdrant)       │
│                                     │
│  Semantic:  key-value or JSON       │
│             (Redis, SQLite)         │
│                                     │
│  Procedural: prompt template store  │
└─────────────────────────────────────┘
```

### Memory Lifecycle

**Writing** — After each agent turn or task completion, extract structured facts or a short summary (using a cheap model). This is the most underdesigned part in most implementations.

**Reading/Retrieval** — Before each turn, query the memory store. Use semantic search (embedding similarity) + structured lookup (exact key match). Often want both.

**Forgetting/Eviction** — Options: time-decay scoring, recency cutoffs, importance scoring at write time (model rates 1-10), or max-N with LRU eviction.

**Consolidation** — Periodically compress episodic memories into semantic ones. "Agent tried X three times and failed" becomes "X doesn't work for this site."

### Write-time Design

- **Explicit extraction prompt** — After task completion, run cheap prompt: "Given this conversation, extract: (1) user preferences, (2) facts learned, (3) what worked/didn't." Output structured JSON.
- **Agent self-tagging** — Agent emits `<remember>` blocks mid-conversation that get intercepted and stored.
- **Event-driven writes** — Write on specific triggers: task success, task failure, tool error, user correction.

### Retrieval Strategy

1. Semantic search over episodic store — top-k relevant past episodes
2. Structured lookup of known entity facts
3. Recency filter — always include last N interactions
4. Merge and summarize — compress if retrieved context is too large

### Implementation Libraries

- **Python**: `mem0` (open-source, handles extraction/embedding/retrieval), `chromadb` + `sqlite`
- **C#**: `Microsoft.SemanticKernel` — `ISemanticTextMemory`, `VolatileMemoryStore`, `QdrantMemoryStore`

---

## Q3: Procedural Memory Implementation

### What Procedural Memory Looks Like

A "procedure" = a reusable, parameterized workflow. Could be:
- A sequence of browser MCP tool calls (`navigate → wait → click → extract`)
- A Python/C# script handling a specific site interaction
- A hybrid: script with an LLM step for ambiguous parts

Key: **store procedures with their trigger conditions** — not just what to do, but when to recognize this situation calls for this procedure.

### Storage Schema

```json
{
  "id": "checkout-amazon",
  "name": "Amazon Checkout Flow",
  "trigger_description": "User wants to purchase an item on Amazon and is on a product page",
  "trigger_embedding": [...],
  "parameters": ["product_url", "quantity"],
  "script": "...",
  "script_type": "python",
  "success_count": 14,
  "failure_count": 2,
  "last_updated": "2026-02-10",
  "notes": "Fails if 2FA is triggered, escalate to main model"
}
```

### Script Representation Options

**Option 1: Raw executable script (deterministic)**
Python/C# script using browser MCP or Playwright directly. Fastest, zero token cost at runtime. Rigid — breaks if site changes.

```python
async def run(params, browser):
    await browser.navigate(params["url"])
    await browser.click("#add-to-cart")
    await browser.wait_for_selector("#cart-count")
    return await browser.get_text("#cart-count")
```

**Option 2: Prompt template (model-guided)**
Structured prompt injected into main model's context, constraining it to a known-good approach.

```
To extract prices from this site:
1. Look for element matching .price-box or [data-price]
2. If not found, check the OG meta tags
3. Parse with regex: \$[\d,]+\.?\d*
```

**Option 3: Plan skeleton (hybrid — recommended)**
High-level step sequence where some steps are deterministic and others are LLM-resolved.

```json
{
  "steps": [
    { "type": "script", "action": "navigate", "url": "{product_url}" },
    { "type": "script", "action": "click", "selector": "#add-to-cart" },
    { "type": "llm", "task": "Handle any popup or modal that appeared" },
    { "type": "script", "action": "extract", "selector": "#confirmation-number" }
  ]
}
```

### Procedure Lifecycle

```
First time (no procedure exists):
  Main model solves it from scratch (expensive)
      ↓
  On success: extract the workflow, store as new procedure
      ↓
  Optionally: cheap model "compiles" it into a script

Subsequent times:
  Retrieve matching procedure
      ↓
  Execute script/skeleton directly
      ↓
  On failure: fall back to main model, then update procedure

Periodic improvement:
  If success_count > N, "compile" prompt-based procedures into pure scripts
```

### Retrieval at Runtime

1. Embed the task description
2. Semantic search over `trigger_description` embeddings
3. If similarity > threshold (e.g. 0.85), retrieve procedure
4. Main model gets compressed context: task + procedure summary
5. Main model decides: run as-is / run with modifications / ignore and solve fresh

### Failure Handling and Versioning

- **Failure threshold**: if `failure_count / total > 0.3`, flag for review or auto-deprecate
- **Versioning**: keep last N versions, allow rollback
- **Partial failure recovery**: log which step and why — makes retraining faster
- **Canary execution**: after site changes, have main model observe and validate

### Python Implementation Sketch

```python
class ProceduralMemory:
    def retrieve(self, task: str) -> Procedure | None:
        embedding = embed(task)
        results = self.vector_db.query(embedding, top_k=3)
        best = results[0] if results[0].score > 0.85 else None
        return self.db.get_procedure(best.id) if best else None

    def store(self, procedure: Procedure):
        embedding = embed(procedure.trigger_description)
        self.vector_db.upsert(procedure.id, embedding)
        self.db.save(procedure)

    def record_outcome(self, procedure_id: str, success: bool, failed_step: int = None):
        self.db.update_stats(procedure_id, success, failed_step)
        if self.db.get_failure_rate(procedure_id) > 0.3:
            self.db.flag_for_review(procedure_id)

async def handle_task(task: str):
    procedure = memory.retrieve(task)
    
    if procedure:
        try:
            result = await execute_procedure(procedure, task)
            memory.record_outcome(procedure.id, success=True)
            return result
        except ProcedureFailure as e:
            memory.record_outcome(procedure.id, success=False, failed_step=e.step)
            # fall through to main model
    
    result, workflow = await main_model_solve(task)
    new_procedure = extract_procedure(task, workflow)
    memory.store(new_procedure)
    return result
```

> Goal: agent starts expensive and flexible, becomes cheap and deterministic over time as procedures accumulate and get compiled into scripts.

---

## Q4: GLM-OCR and Vision Models on Ollama for Web Content

### GLM-OCR Assessment

**GLM-OCR is specialized for document OCR, not general web content understanding.**

- Excels at extracting structured content from images — tables, formulas, text in scanned documents, PDFs
- Achieves 1.86 pages/second throughput, ranks #1 on OmniDocBench V1.5 (score 94.62)
- Supports structured JSON output extraction
- **Limitation**: image-in, text-out only — cannot process raw HTML. Not a reasoning model — it transcribes, doesn't answer questions intelligently.

**Verdict**: Useful as a screenshot parser (browser screenshot → GLM-OCR extracts text → main model reasons), but not a general web comprehension model.

### Ollama Vision Models for Web Content

| Model | Size | Why Relevant |
|---|---|---|
| **qwen2.5vl** / **qwen3-vl** | 3B–7B | Best general vision-language reasoning, understands screenshots contextually |
| **granite3.2-vision** | 2B | Designed for document/visual content extraction, compact, supports tool use |
| **moondream** | 1.8B | Very lightweight edge model, good for simple screenshot Q&A |
| **llava-phi3** | 3.8B | Small, fast, reasonable visual reasoning |
| **deepseek-ocr** | 3B | Token-efficient OCR, similar niche to GLM-OCR |
| **mistral-small3.1/3.2** | 24B | Heavier but strong vision + tool use — closer to main model territory |

**Best for "stupid local model" role**: `qwen2.5vl:7b` or `granite3.2-vision:2b` — can answer questions about screenshots, not just transcribe.

### Recommended Pipeline

```
Browser MCP
    ├── get_accessibility_tree() → raw text path
    │       → trafilatura strip (deterministic)
    │       → small text model (qwen2.5:3b, phi3-mini)
    │
    └── screenshot() → image path  
            → GLM-OCR or deepseek-ocr (if pure extraction needed)
            → qwen2.5vl:7b (if visual Q&A / layout understanding needed)
```

Use accessibility tree + text model path first (cheapest). Fall back to screenshot + vision model only when the page is heavily visual (SPAs, canvas-rendered content, image-heavy layouts).
