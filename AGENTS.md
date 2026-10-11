# Unity Editor integration

Use the installed Unity CLI and the `unity-cli` skill to operate this project.
The project path is `D:/UnityProjects/BounceBall`.

On this Windows machine the CLI executable is
`C:/Users/HaiChuan/AppData/Local/Unity/bin/unity.exe`.
Use the absolute executable path if `unity` is not available on PATH.

- Before Editor operations, check `unity status --project-path D:/UnityProjects/BounceBall --json`.
- Always target this project explicitly with `--project-path` for Editor commands.
- Discover supported operations with `unity command`; inspect command details before invoking them.
- Prefer `unity command` for Editor operations and `unity command eval` for C# execution. This installed CLI does not expose a top-level `unity eval` command.
- Modify scenes, prefabs, and assets through the running Editor instead of editing serialized files directly.
- Preserve the user's existing work. Do not close the Editor without saving or overwrite unsaved scenes.
- If Pipeline is still loading, allow imports to finish. Report connection failures and check `unity pipeline list --json` for Safe Mode.

The Unity CLI MCP server is also configured in the user's Codex configuration.
