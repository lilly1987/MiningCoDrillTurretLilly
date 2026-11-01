# GitHub Copilot Instructions for MiningCo. DrillTurret (Continued)
## Mod Overview and Purpose
MiningCo. DrillTurret is a mod for the game RimWorld that adds a new turret called the drill turret. The drill turret can automatically drill nearby ores and natural rocks. The mod includes several features and systems that allow players to customize the behavior of the drill turret and improve its efficiency.
## Key Features and Systems

  * Automatic drilling: The drill turret can automatically drill nearby ores and natural rocks.
  * Autonomous operation: Unmanned drilling can be enabled while retaining the existing operator speed bonus.
  * Configurable drilling: Global settings control range, wall obstruction, damage, drilling interval, and target search interval.
  * Designation priority: Marked mining targets can be prioritized during automatic target selection.
  * Designated-only mode: The mining mode cycle can be set to mine only targets marked for mining.
  * Mining mode: The mining mode can be switched between only ores, only rocks, and both.
  * Manning: Manning the turret will improve its drilling speed, with level 20 miners using the drill turret mining 2x faster.
  * Drill efficiency and yield: Drill efficiency and yield can be improved via one additional research.

## Coding Patterns and Conventions
The mod follows standard coding patterns and conventions used in RimWorld modding. The code is organized into several classes, each responsible for a specific aspect of the drill turret's behavior. The code is written in C# and includes XML files that define the mod's assets and settings.
## XML Integration
The mod includes several XML files that define the mod's assets and settings. These files are located in the "Defs" folder and include definitions for effects, jobs, research projects, thing defs, and work givers. The XML files are integrated into the mod using the RimWorld Defs system, which allows them to be loaded and used at runtime.
## Harmony Patching
The mod uses Harmony patching to modify the behavior of existing RimWorld classes and methods. Harmony patching is a powerful tool that allows modders to add custom code to existing game code without having to modify the game's source code. The mod includes several Harmony patches that modify the behavior of the drill turret and other RimWorld classes.
## Suggestions for Copilot
GitHub Copilot can be a valuable tool for RimWorld modders. Here are some suggestions for using Copilot with this mod:

  * Use Copilot to generate code for new features and systems.
  * Use Copilot to generate XML files for new assets and settings.
  * Use Copilot to generate Harmony patches for modifying existing game code.
  * Use Copilot to generate documentation for the mod, including code comments and README files.
  * Use Copilot to generate unit tests for the mod's code.

By following these guidelines, you can use GitHub Copilot to help you develop and maintain the MiningCo. DrillTurret mod.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.
