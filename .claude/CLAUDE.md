# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project Overview

This is a Unity 6000.0.46f1 Mixed Reality project for fire simulation using Meta XR MR Utility Kit. The project targets Meta Quest devices with OpenXR support and utilizes Universal Render Pipeline (URP).

# Guidelines

This document defines the project's rules, objectives, and progress management methods. Please proceed with the project according to the following content.

## Top-Level Rules

- To maximize efficiency, **if you need to execute multiple independent processes, invoke those tools concurrently, not sequentially**.
- **You must think exclusively in English**. However, you are required to **respond in Japanese**.
- To understand how to use a library, **always use the Contex7 MCP** to retrieve the latest information.
- For temporary notes for design, create a markdown in `.tmp` and save it.
- **After using Write or Edit tools, ALWAYS verify the actual file contents using the Read tool**, regardless of what the system-reminder says. The system-reminder may incorrectly show "(no content)" even when the file has been successfully written.
- Please respond critically and without pandering to my opinions, but please don't be forceful in your criticism.

## Unity Project Structure

### Key Directories
- `Assets/Script/` - Main C# scripts for fire simulation and MR interactions
- `Assets/Scenes/` - Unity scenes: `SampleScene.unity`, `firescene.unity`, `FireDistinguish.unity`
- `Packages/` - Unity packages including Meta XR MR Utility Kit (`com.meta.xr.mrutilitykit`)
- `.tmp/` - Temporary design documentation

### Core Scripts Architecture
- **FlameController.cs** - Manages flame placement using controller raycasting. Handles OVRInput for left controller trigger, spawns flame on floor, and delays smoke spawn at ceiling height
- **FlameAndSmokeManager.cs** - Integrates with MRUK (Mixed Reality Utility Kit) to detect floor/ceiling anchors. Provides ceiling Y-coordinate for smoke positioning via `HasCeilingAnchor()` method. Listens to `MRUK.Instance.SceneLoadedEvent`
- **Particle Controllers** - Various scripts for flame growth, smoke expansion, audio, and damage simulation

### Unity-Specific Commands

**Opening the Project:**
- Open Unity Hub, add project, and open with Unity 6000.0.46f1

**Building for Meta Quest:**
- File > Build Settings > Android platform
- Player Settings: Configure XR Plugin Management for OpenXR
- Build and deploy to Meta Quest device

**Testing in Unity Editor:**
- Play mode requires XR simulator or connected Meta Quest device
- Scripts depend on MRUK anchors (floor/ceiling detection)

## Programming Rules

### C# Conventions
- Use Unity's serialization with `[SerializeField]` for inspector-exposed fields
- Leverage Unity's coroutines for delayed operations (e.g., `SpawnSmokeAfterDelay`)
- Check for null references before `Instantiate`/`Destroy` operations
- Use Unity's layer mask system for raycasting (e.g., `floorLayer`)

### MR Utility Kit Integration
- Always wait for `MRUK.Instance.SceneLoadedEvent` before accessing anchors
- Use bitflag checks for anchor labels: `(anchor.Label & MRUKAnchor.SceneLabels.FLOOR) != 0`
- Implement fallback behaviors when anchors are not detected

### General Rules
- Avoid hard-coding values unless absolutely necessary
- Do not use `any` or `unknown` types in TypeScript
- You must not use a TypeScript `class` unless it is absolutely necessary (e.g., extending the `Error` class for custom error handling that requires `instanceof` checks)

## Development Style - Specification-Driven Development

### Overview

When receiving development tasks, please follow the 5-stage workflow below. This ensures requirement clarification, structured design, comprehensive testing, and efficient implementation.

### 5-Stage Workflow

#### Stage 1: Requirements

- Analyze user requests and convert them into clear functional requirements
- Document requirements in `.tmp/requirements.md`
- Use `/requirements` command for detailed template

#### Stage 2: Design

- Create technical design based on requirements
- Document design in `.tmp/design.md`
- Use `/design` command for detailed template

#### Stage 3: Test Design

- Create comprehensive test specification based on design
- Document test cases in `.tmp/test_design.md`
- Use `/test-design` command for detailed template

#### Stage 4: Task List

- Break down design and test cases into implementable units
- Document in `.tmp/tasks.md`
- Use `/tasks` command for detailed template
- Manage major tasks with TodoWrite tool

#### Stage 5: Implementation

- Implement according to task list
- For each task:
  - Update task to in_progress using TodoWrite
  - Execute implementation and testing
  - Run lint and typecheck
  - Update task to completed using TodoWrite

### Workflow Commands

- `/spec` - Start the complete specification-driven development workflow
- `/requirements` - Execute Stage 1: Requirements only
- `/design` - Execute Stage 2: Design only (requires requirements)
- `/test-design` - Execute Stage 3: Test design only (requires design)
- `/tasks` - Execute Stage 4: Task breakdown only (requires design and test design)

### Important Notes

- Each stage depends on the deliverables of the previous stage
- Please obtain user confirmation before proceeding to the next stage
- Always use this workflow for complex tasks or new feature development
- Simple fixes or clear bug fixes can be implemented directly

# important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.
