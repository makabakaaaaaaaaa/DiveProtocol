# DiveProtocol Agent Guidelines

本文件约束之后在本仓库中执行任务的 Codex 或其他自动化代理。

## 工程约束

- 项目名称：DiveProtocol
- 根命名空间：`DiveProtocol`
- 使用 `ProjectSettings/ProjectVersion.txt` 记录的项目实际 Unity 版本（当前为 `6000.0.69f1`）。
- 使用 Universal Render Pipeline（URP）。
- Runtime 代码放在 `Assets/_DiveProtocol/Code/Runtime`。
- Editor 代码放在 `Assets/_DiveProtocol/Code/Editor`。
- 测试放在 `Assets/_DiveProtocol/Code/Tests`。
- 自有资源放在 `Assets/_DiveProtocol`。
- 第三方资源不得放入自有代码目录。

## 代码规则

- 类、方法和公开属性使用 PascalCase。
- 私有字段使用 `_camelCase`。
- 一个主要公开类对应一个 `.cs` 文件。
- 避免全局静态可变状态。
- 避免巨型 GameManager，通过职责明确的小系统协作。
- 对外暴露只读属性，状态修改通过方法完成。
- 为关键公共 API 添加简短 XML 注释。
- 不引入未经批准的第三方依赖。
- 不自动修改 `Packages/manifest.json`，除非任务明确要求。
- 不自动提交 Git Commit。

## Unity 资源规则

- 不直接手写或批量修改 `.unity` 和 `.prefab` 的 YAML 内容。
- 需要生成场景、Prefab 或批量资源时，优先编写 Unity Editor 工具，由用户在编辑器中执行。
- 不删除 `.meta` 文件。
- 不修改 `Assets/ThirdParty` 和 `Packages` 目录中的第三方源码。
- 所有场景名使用 `SCN_` 前缀。
- Prefab 使用 `PF_` 前缀。
- ScriptableObject 资产使用 `SO_` 前缀。

## 交付规则

- 修改前先检查现有实现。
- 保留已有有效内容。
- 每次任务完成后列出：新增文件、修改文件、未完成事项、已运行的检查或测试。
- 若无法运行 Unity 编译或测试，要明确说明，不能声称已经通过。

## Unity manual-setup rule

Unless the user explicitly requests an Editor automation tool:

- Do not create Unity Editor one-click generators.
- Do not create Create or Repair menus.
- Do not create migration, reset, setup, validation-window, or auto-layout tools.
- Do not automatically generate or modify Scenes, Prefabs, or ScriptableObject assets.
- Implement only the requested runtime C# scripts.
- After implementing a runtime script, provide clear manual Unity Inspector and Hierarchy setup instructions.
- Do not add MenuItem attributes unless the user explicitly asks for an Editor menu.
- Do not add new code under an Editor folder unless explicitly requested.

## Unity interaction manual-setup rule

Unless the user explicitly requests Unity Editor automation:

- Do not create interaction Editor generators.
- Do not create Create or Repair interaction menus.
- Do not automatically add interaction components to Scenes or Prefabs.
- Do not automatically create doors, pickups, terminals, keys, prompts, or test objects.
- Do not automatically modify Scene or Prefab layouts.
- Implement only the requested Runtime C# scripts.
- After writing a Runtime interaction script, provide manual setup instructions:
  - which GameObject receives the component;
  - which Collider or Trigger is required;
  - which Inspector references must be assigned;
  - which tags or layers are required;
  - recommended initial values;
  - how to test the interaction manually.
- Do not add MenuItem attributes unless explicitly requested.
- Do not add new Editor-folder code unless explicitly requested.
