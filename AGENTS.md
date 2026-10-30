# AGENTS.md

## Agent Instructions

Persistent instructions for the opencode agent working on this project.

### 1. Approval-First Rules

#### 1.1 Inspect First

Before changing anything, inspect the relevant code, dependencies, conventions, and call sites, then report findings. Reading and searching the codebase are always allowed — never ask permission to read files.

#### 1.2 Ask Before You Start

Never assume approval. Before implementing, state:
- The files you will create or modify, and why.
- The intended behavior and approach.
- Risks or compatibility implications.
- Any new dependencies you believe are necessary.

If you are unsure how to do something, or need a decision on architecture, API contracts, security, payment behavior, checkout UX, data models, error handling, or business rules — stop and ask me before starting. Explain the options and their trade-offs. Do not silently pick an assumption.

Do not ask about trivial details already settled by the project conventions or determinable by inspecting the code.

Approval for one change never authorizes unrelated changes, new dependencies, or future batches.

#### 1.3 Feature Scoping

Stay inside the feature you are working on. For example, payment work means `payment.ts`, `payment.html`, `payment.scss`, and `checkout-service.ts` — plus, only when genuinely required, a related model/DTO in its own file, or a router/config change. Do not expand into unrelated areas of the app.

Prefer finishing a feature in one batch, then stop and report:
- The exact files created/changed and what changed.
- The checks performed and their results.
- Anything still unresolved.

Wait for my review before continuing.

Never claim a test, build, or verification passed unless you actually ran it and observed the result.

#### 1.4 Files: Create Freely, Never Delete Silently

You may create new files as needed without asking first.

Never delete, overwrite, or discard an existing file or my uncommitted work until you tell me which file and why, and I approve.

#### 1.5 Dependencies and Backend

Do not install, add, remove, or upgrade any npm/NuGet package, library, dependency, or tool without my explicit approval. Before proposing one, explain why it is needed, what it will do, and whether the task can be done with the existing dependencies.

Do not change backend behavior, payment contracts, or database models without explicit approval.

#### 1.6 Angular Rules — Mandatory

- Never use `*ngIf` — use Angular's built-in `@if`.
- Never use `*ngFor` — use Angular's built-in `@for`.
- Follow the Angular version already installed in this project.
- Do not introduce APIs, syntax, or patterns that are deprecated in the installed version when a supported alternative exists.
- Follow existing project conventions unless they conflict with these rules or introduce a concrete problem.
- Use the HTML character reference `&#x20;` when an explicit HTML space character is needed; do not replace every ordinary space with it unnecessarily.
- Do not add Angular modules, packages, or dependencies when the existing standalone setup and installed packages are sufficient.

#### 1.7 Code Quality and Existing Work

- Preserve existing user changes and uncommitted work.
- Do not run destructive commands.
- Do not make unrelated refactors, formatting-only changes, or cleanup.
- Follow the existing architecture, coding style, and naming conventions.
- Reuse existing services, endpoints, components, and dependencies where appropriate.
- Do not duplicate existing functionality without a clear reason.

#### 1.8 Payment and Stripe Safety

- Inspect the existing backend payment flow and contracts before proposing implementation.
- Never handle or store raw card details in the application or backend; Stripe collects card data directly.
- Reuse the existing Stripe integration and installed packages where possible.
- Handle loading states, failed requests, missing client secrets, and repeated entry into the payment step explicitly.
- Never assume payment success based solely on frontend state.
- Ask before making any material decision about payment-intent creation, updates, confirmation, or order completion.

#### 1.9 Required Response Format

Before implementation, report:
1. Current implementation and findings.
2. Existing endpoint contracts.
3. Existing packages and integration.
4. Exact proposed file list and the reason for each file.
5. Proposed implementation plan.
6. Any proposed new files or dependencies.
7. Questions requiring my design decision.

Then stop and wait for my explicit approval.

#### 1.10 Maintaining This File

When I explicitly ask you to save, remember, or add an instruction to `AGENTS.md`:
1. Read the current `AGENTS.md` before changing it.
2. Preserve all existing instructions; add the new one in the appropriate section or a new clearly named section.
3. Avoid duplicate or contradictory rules; if a conflict exists, explain it and ask me how to resolve it.
4. Show me a concise summary of the proposed addition before editing.
5. Wait for my explicit approval before modifying the file.
6. After approval, update only the necessary part and report exactly what changed.

Do not interpret every message as an instruction to save. Do not modify `AGENTS.md` while working on unrelated tasks. Do not remove or weaken existing safety, approval, dependency, or verification rules without my explicit approval.

#### Session Startup

At the beginning of each new task: read `AGENTS.md`, apply it alongside the current conversation's instructions, and never assume it overrides a more recent explicit instruction from me.
