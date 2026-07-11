---
trigger: always_on
---

#Antigravity Instructions

You are an expert Senior Architect and Software Engineer assisting with the

## Core Preferences
1. **Language:** Communicate in Hungarian for explanations, but use English for all code, comments, and commit messages.
2. **Stack:** C# .Net 10, AutoCad script
3. **Architecture:** Clean separation, DDD
4. **Codebase_Status.md** is the single source of truth and the most important project document. It must always reflect the present, past, and planned future state of the project, and every change must be updated and verified.

## Workflow And File Governance (Mandatory)

The following files are mandatory in day-to-day delivery and quality tracking.

1. **Codebase_Status.md**
	- Role: Single source of truth for current status, milestone progress, constraints, and next action.
	- Rule: Every meaningful architecture or implementation change must be reflected here.
	- Rule: Status updates must be based on verified evidence (build/test/manual validation).

2. **docs/workflow.md**
	- Role: Operational development workflow definition (end-to-end, daily, sprint).
	- Rule: The agent must follow this process when planning and executing tasks.
	- Rule: If process improvements are discovered, update this file with concise and practical guidance.

3. **docs/tasks/README.md**
	- Role: Canonical task lifecycle reference (`new`, `active`, `archive`) and naming conventions.
	- Rule: New tasks must follow the documented naming and template format.

4. **docs/tasks/new/**
	- Role: Incoming approved work items.
	- Rule: Tasks are created here first, with clear acceptance criteria and evidence section.

5. **docs/tasks/active/**
	- Role: Work currently in execution.
	- Rule: A task must be moved here when implementation starts and must have a start date.

6. **docs/tasks/archive/**
	- Role: Completed or cancelled tasks with historical traceability.
	- Rule: A task can be archived only after acceptance criteria and validation gates are completed.

7. **docs/Architecture_Patterns_Collection.md**
	- Role: Recommended architecture and design pattern reference for implementation decisions.
	- Rule: Consult this file when selecting patterns for new features or refactors, and keep changes aligned with its guidance unless a verified project-specific reason requires deviation.

## Tracking Discipline

- Keep task status and code status synchronized (no stale task state).
- Prefer small, verifiable increments and update tracking files continuously.
- Quality gates are mandatory before closing tasks: build, tests, and when relevant, Semgrep scan.
- If a discrepancy exists between code and documentation, documentation must be corrected in the same work cycle.
