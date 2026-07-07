# DiveProtocol

DiveProtocol 是一个面向 Windows PC、使用 Universal Render Pipeline（URP）的 Unity 项目。

## 项目信息

- Unity 版本：6000.0.69f1
- 渲染管线：Universal Render Pipeline（URP）
- 当前阶段：Demo 工程搭建
- 目标平台：Windows PC

## 打开方式

使用 Unity `6000.0.69f1` 打开本项目根目录。

预期启动场景目前暂未创建；后续将从 `SCN_Bootstrap` 启动。

## 目录说明

- `Assets/_DiveProtocol`：项目自有游戏资源
- `Assets/ThirdParty`：第三方资源
- `Packages`：Unity Package Manager 配置与依赖
- `ProjectSettings`：Unity 项目设置

## Git 说明

- 必须提交资源对应的 `.meta` 文件。
- 不提交 Unity 生成的 `Library` 目录。
- 大型二进制资源建议使用 Git LFS。

## 基础开发原则

- 不直接修改第三方插件。
- 不把正式代码放入 `Assets` 根目录。
- 不使用 `GameObject.Find` 作为核心架构。
- 不把当前一局的运行数据直接保存在 ScriptableObject 资产中。

