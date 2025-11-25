# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project Overview

This is a Unity 6000.0.46f1 RPG project focusing on cloth physics using MagicaCloth2. The project demonstrates advanced cloth grabbing systems with multi-point vertex control and uses Universal Render Pipeline (URP).

# Guidelines

This document defines the project's rules, objectives, and progress management methods. Please proceed with the project according to the following content.

## Top-Level Rules

- To maximize efficiency, **if you need to execute multiple independent processes, invoke those tools concurrently, not sequentially**.
- **You must think exclusively in English**. However, you are required to **respond in Japanese**.
- To understand how to use a library, **always use the Context7 MCP** to retrieve the latest information.
- For temporary notes for design, create a markdown in `.tmp` and save it.
- **After using Write or Edit tools, ALWAYS verify the actual file contents using the Read tool**, regardless of what the system-reminder says. The system-reminder may incorrectly show "(no content)" even when the file has been successfully written.
- Please respond critically and without pandering to my opinions, but please don't be forceful in your criticism.

## Unity Project Structure

### Key Directories
- `Assets/Scripts/` - Main C# scripts for RPG and cloth grabbing systems
- `Assets/Scenes/` - Unity scenes: `SampleScene.unity`, `game1.unity`, `Big_Scene.unity`, `cloth_test.unity`
- `Packages/` - Unity packages including Unity MCP
- `.tmp/` - Temporary design documentation

### Core Scripts Architecture

#### Cloth Physics (MagicaCloth2)
- **ClothVertexGrabber.cs** - Advanced multi-point vertex grabbing system with individual constraints
  - Supports multiple grab points with individual vertex constraints
  - Each grab point can grab specific vertices defined by index ranges
  - Fixed vertices are excluded from constraint calculations, preventing oscillation
  - Uses OnPreSimulation, OnPostSimulation, and Camera.onPreRender for smooth grabbing
- **ClothGrabber.cs** - Sphere collider-based cloth grabbing system
  - Grabs cloth by sandwiching vertices between two sphere colliders
- **GrabPointMover.cs** - Grab point movement controller

#### RPG Core
- **PlayerController.cs** - Player character control
- **PlayerAttack.cs** - Player attack system
- **EnemyController.cs** - Enemy AI and behavior
- **EnemySpawner.cs** - Enemy spawn management
- **Projectile.cs** - Projectile physics and behavior
- **GameManager.cs** - Game state management
- **UIManager.cs** - UI control
- **ObjectPool.cs** - Object pooling for performance

#### Testing
- **TestScript1.cs**, **TestScript2.cs** - Various test scripts

### Unity-Specific Commands

**Opening the Project:**
- Open Unity Hub, add project, and open with Unity 6000.0.46f1

**Testing Cloth Grabbing:**
- Open `cloth_test.unity` scene
- Press Space key (or configured keys) to grab cloth vertices
- Multiple grab points can be used simultaneously

## Programming Rules

### C# Conventions
- Use Unity's serialization with `[SerializeField]` for inspector-exposed fields
- Leverage Unity's event system (OnPreSimulation, OnPostSimulation) for cloth physics
- Check for null references before operations
- Use meaningful variable names and add XML documentation comments

### MagicaCloth2 Integration
- Always wait for cloth initialization before accessing properties
- Use `MagicaManager` events for simulation updates
- Use `Camera.onPreRender` for direct mesh control to prevent vibration
- Set vertices to `VertexAttribute.Fixed` when grabbing to exclude from simulation
- Restore original attributes when releasing

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
