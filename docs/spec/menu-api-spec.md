# Spec — CustomSettings 设置菜单 API（注册设置项 + 自定义子页）

> 状态：**Spec（已确认决策）**，尚未写代码。
> 来源：本文件由 `docs/menu-api-spec.md` 的实现细则按 to-spec 模板修缮而成；详细的原版代码事实（反编译源码行号、场景结构、`MenuRegister` 先例源码）保留在仓库内实现细则文档中，spec 正文只承载决策。

## Problem Statement

《渎神》原版设置菜单（`OptionsWidget`，含 GAME / VIDEO / AUDIO / ACCESSIBILITY 等子菜单）的选项是场景预置对象 + 写死枚举，其他 mod 无法往里面加设置项，也无法创建自己的设置页面。mod 作者想要让玩家在游戏设置里配置 mod 选项，只能自建 UI 或侵入式修改原版菜单代码，代价高且易碎。

`Blasphemous.CustomSettings` 想成为这个能力的提供方：让其他 mod 通过一行注册调用，把设置项挂进原版设置菜单，或创建完全自定义的设置子页——不改动原版程序集、不改动原版枚举 switch。

## Solution

CustomSettings 提供两个服务注册扩展方法（ModdingAPI 3.0.1 服务模式，先例为 `Framework.Menus` 的 `MenuRegister`）：

- `provider.RegisterCustomSettingsOption(option, target)` — 在指定挂载点注入一个设置项（Toggle / Arrow / Text），UI 通过复制原版现成选项对象生成（内置默认模板，调用方可覆盖）。
- `provider.RegisterCustomSettingsTab(tab, parent)` — 注册一个自定义设置子页（新建空页 + 新选项，或克隆原版子菜单页删光选项后加新选项），返回子页句柄。

宿主范围限定**主菜单 / 暂停菜单的设置页内（作为子页）**；入口由调用方自定（Options 根菜单 / `ExtrasMenuWidget` 根菜单 / 任意子菜单 / mod 子页内），子页支持嵌套。取值不自动持久化：`T? GetValue<T>()` + `onChange` / `onClose` 事件回传当前值。

## User Stories

1. 作为一个 mod 开发者，我想调用 `RegisterCustomSettingsOption` 把 1 个 Toggle 开关挂进原版 GAME 子菜单，以便玩家能在游戏设置里开关我 mod 的功能。
2. 作为一个 mod 开发者，我想调用 `RegisterCustomSettingsOption` 把 1 个 Arrow 多选项挂进原版 GAME 子菜单，以便玩家能从我的选项列表里选一项。
3. 作为一个 mod 开发者，我想调用 `RegisterCustomSettingsOption` 把 1 个 Text 数值选项挂进原版 GAME 子菜单，以便玩家能输入/调整一个整数数值。
4. 作为一个 mod 开发者，我想调用 `RegisterCustomSettingsOption` 把 1 个 Text 执行型选项挂进原版 GAME 子菜单，以便玩家点击它触发一个动作（如打开子页、打开链接）。
5. 作为一个 mod 开发者，我想把设置项挂进原版 VIDEO / AUDIO / ACCESSIBILITY 子菜单，以便选项按游戏原版分类归位。
6. 作为一个 mod 开发者，我想把设置项挂进 `ExtrasMenuWidget`（主菜单"额外内容"）的根菜单，以便不依赖设置页也能暴露我的选项。
7. 作为一个 mod 开发者，我想把设置项挂到任意 Transform（例如我自己的子页内部），以便在 mod 自己的菜单里也能配置。
8. 作为一个 mod 开发者，我想注册一个自定义设置子页（`RegisterCustomSettingsTab`），以便创建我自己的"Mod 设置"页面，不混入原版选项。
9. 作为一个 mod 开发者，我想让我的子页从原版 GAME 菜单的一个入口选项打开（点击后切到我的子页），以便玩家在熟悉的设置流程里到达我的页面。
10. 作为一个 mod 开发者，我想在我的子页里再挂一个子页入口（嵌套），以便组织多级设置结构。
11. 作为一个 mod 开发者，我想在子页关闭时收到 `onClose` 回调拿到最终值，以便在玩家离开我的设置页时统一保存/应用。
12. 作为一个 mod 开发者，我想在玩家改动选项值时立即收到 `onChange` 回调，以便做即时预览或联动。
13. 作为一个 mod 开发者，我想随时用 `T? GetValue<T>()` 读取选项当前值（Toggle→bool? / Arrow→int? 索引 / Text→int?），以便同步其他逻辑。
14. 作为一个 mod 开发者，我不想自己搭 UI 结构（字体、高亮框、间距），以便保持和原版设置选项一致的观感——由 API 复制原版选项对象。
15. 作为一个 mod 开发者，我想传入自己的模板 Transform 覆盖默认模板，以便选项外观完全由我控制。
16. 作为一个 mod 开发者，我想 API 在主菜单和游戏内暂停菜单都自动挂载我的选项，以便两种场景下玩家都能配置。
17. 作为一个 mod 开发者，我想场景切换后选项自动重建，以便不必关心 UI 生命周期。
18. 作为一个 mod 开发者，我想注册调用在 `BlasMod.OnRegisterServices(ModServiceProvider provider)` 里完成（与 Framework.Menus 的 `RegisterNewGameMenu` 同风格），以便我按 ModdingAPI 惯例使用。
19. 作为一个 mod 开发者，我想 API 不修改原版 `OptionsWidget` / `ExtrasMenuWidget` 代码、不动原版枚举与 switch，以便升级游戏版本时我的 mod 不碎。
20. 作为一个玩家，我想在设置菜单里看到 mod 提供的开关/选择项并正常操作，以便用设置项控制 mod。
21. 作为一个玩家，我想从设置菜单进入 mod 的自定义设置页并返回，以便完成配置后回到原设置流程。
22. 作为一个 mod 开发者，我想知道注册失败/定位失败的原因（日志），以便排查问题。
23. 作为一个 mod 开发者，我想让多个 mod 同时注册互不干扰，以便生态共存。
24. 作为一个 mod 开发者，我想在不支持的目标（如未知子菜单名）上注册时得到明确失败而不是静默，以便尽早发现错误。

## Implementation Decisions

- **API 暴露形态（仅服务方式）**：CustomSettings 定义 `SettingsMenuRegister` 静态类，提供 `public static void RegisterCustomSettingsOption(this ModServiceProvider provider, SettingsOption option, SettingsMenuTarget target)` 与 `public static SettingsTab RegisterCustomSettingsTab(this ModServiceProvider provider, SettingsTab tab, SettingsMenuTarget parent = null)` 两个扩展方法。调用方在自己的 `BlasMod.OnRegisterServices(ModServiceProvider)` 里调用。数据入 static 注册表，注册项归属写 `provider.RegisteringMod`（对齐 `MenuRegister` 先例，先例源码见实现细则文档）。
- **数据模型（决策编码的契约形状，来自实现细则）**：
  ```csharp
  public sealed class SettingsOption
  {
      public string Title;                    // 选项标题文本
      public OptionType Type;                 // Toggle | Arrow | Text
      public IReadOnlyList<string> Choices;   // Arrow 的选项列表（索引即值）
      public object DefaultValue;             // Toggle: bool / Arrow: int / Text 数值: int
      public Action<T?> OnChange;             // 值变化回调
      public Action OnClose;                  // 菜单/子页关闭回调
  }
  public sealed class SettingsTab
  {
      public string Title;
      public IReadOnlyList<SettingsOption> Options;
  }
  ```
- **挂载点模型（`SettingsMenuTarget`）**：两种目标——(1) 原版子菜单枚举（`OptionsWidget.MENU.GAME/VIDEO/AUDIO/ACCESSIBILITY`、`ExtrasMenuWidget.MENU.EXTRAS` 等）；(2) 任意 `Transform`（支持子页内嵌套）。定位用双通道：运行时 `GameObject.Find` 按场景结构路径 + 反射读 `OptionsWidget.optionsRoot` / `ExtrasMenuWidget.extrasRoot` 字典；`OptionsWidget`/`ExtrasMenuWidget` 实例用 `Object.FindObjectOfType<...>()` 获取（仅对应场景存在）。主菜单与暂停菜单是两个实例，分别定位。
- **选项 UI 生成（复制原版模板）**：默认模板运行时从 GAME 子菜单定位——toggle ← `ENABLEHOWTOPLAY` 选项对象、arrow ← `AUDIOLANGUAGE` 选项对象、text ← `CONTROLSREMAP` 选项对象（三者的控件类型已核实）；调用方传入自定义模板 `Transform` 时优先。克隆后清掉原版行为残留（`Selection` 高亮子物体随模板自带），标题/值/事件由 API 接线，插入目标 root 末尾由 `VerticalLayoutGroup` 排布。
- **注入不触碰原版枚举与 switch**：原版 `GAME_OPTIONS` 等枚举与 `ShowMenu`/`UpdateInputGameOptions` 的 switch 是死的，无法注入新枚举值；新选项**不进 `gameElements` 等字典**，显隐/选中/输入交互由 API 自管，避免被原版 `Update()` 干扰。
- **子页与嵌套导航（栈式）**：子页 = API 新建的 root `Transform`（带 `CanvasGroup`、`VerticalLayoutGroup`、`Selection` 高亮节点）。打开子页 = 隐藏当前挂载点 root（记录原 CanvasGroup 状态）→ 显示子页 root；返回 = 恢复挂载点 → 触发子页 `onClose`。嵌套用栈管理（push/pop），支持"mod 子页内再打开另一个 mod 子页"。**不修改 `optionsRoot` 字典、不 patch `ShowMenu`**。入口由调用方自定（任意选项点击事件里调 `SettingsTab.Open()/Close()`）。
- **取值语义**：`Toggle → bool?`、`Arrow → int?`（`Choices` 下标）、`Text 数值 → int?`、`Text 执行 → 无值（仅事件）`。`T? GetValue<T>()` 强类型读取 + `event Action<T?> OnChange` + `event Action OnClose`。设置菜单是异步 UI，`OpenMenu` 不阻塞返回，回传靠事件。
- **导航接线**：优先用 UGUI 标准 `Selectable.navigation`（手动设 up/down 指向相邻选项），不依赖原版 private `LinkButtonsVertical`；若实测原版 `EventsButton` 选中链路必须走自身逻辑，再考虑反射/Harmony。
- **生命周期**：注册发生在 `OnRegisterServices`（启动早期，早于场景加载）→ 数据入 static 表；CustomSettings 在 `OnLevelLoaded` 进入 MainMenu 或游戏场景（暂停菜单可用）时消费注册表执行注入/建子页；UI 属场景对象，场景切换销毁、下次进入重建，无需 `DontDestroyOnLoad`。
- **模块划分（本 mod 内）**：挂载点定位器、模板定位器、数据模型与注册扩展（`SettingsMenuRegister`）、选项注入器（克隆+接线）、子页管理器（栈式导航）。

## Testing Decisions

- **好测试的标准**：只测外部行为——"注册后，在对应场景的设置菜单里能看到并能操作该选项/子页，取值与事件正确"，不测实现细节（不测反射路径、不测内部字典结构）。
- **自动化测试现实**：本仓库当前无任何测试项目，Unity 游戏 UI 无法单元测试；ModdingAPI 生态惯例为实机验证 + `ModLog` 日志。因此**测试缝 = 实机验证清单 + 纯逻辑单元测试（可选）**。
- **可单元测试的纯逻辑**（若后续加测试项目）：注册表写入/归属（`OwnerMod`）、`SettingsMenuTarget` 解析失败路径、取值类型映射（Toggle/Arrow/Text → bool?/int?）。若加，用 NUnit/xUnit 独立于 Unity 的纯 C# 项目，引用真实 ModdingAPI dll。
- **实机验证清单（主要测试手段）**：
  1. 主菜单：向 GAME 子菜单各注入 1 个 Toggle/Arrow/Text，确认显示、可选中、可改值、事件触发、返回正常。
  2. 游戏内暂停菜单：同样注入，确认暂停菜单实例下生效。
  3. 子页：GAME 菜单入口选项 → 打开 mod 子页 → 配置 → 返回；嵌套两级子页。
  4. 冲突检查：新选项存在时原版选项的上下导航、Accept/Apply 按钮、横向改值均不受影响。
  5. 回归：加载多个注册 mod 互不干扰；场景切换往返后选项正确重建。
- **验证方式**：BepInEx 控制台日志（注册成功/失败、定位失败原因）+ 游戏内截图（按 Vision 技能流程截图核对布局）。

## Out of Scope

- **游戏内随时弹出自定义菜单**（战斗中/地图中）：宿主限定主菜单/暂停菜单设置页内，不做自建常驻 Canvas、不做游戏暂停/输入拦截。
- **值自动持久化**（BepInEx ConfigEntry / 游戏存档）：API 只回传当前值，存储由调用方决定。
- **标题本地化**（原版 `ScriptLocalization` key 方案）：调用方传最终文本，不引入本地化系统。
- **修改/复用原版 `gameElements` 字典或 `optionsRoot` 字典**、patch `ShowMenu`：显式不做。
- **与 `Blasphemous.Framework.Menus` 集成**（复用其 Creator/ModMenu 做 UI 构建）：本方案不引用该框架（其 Canvas 生命周期与流程层绑死主菜单，见 handoff 文档）。
- **指定选项插入位置**（非末尾）：默认追加末尾，位置参数后续版本再加。
- **Unity 场景资源工程**（改原版场景/prefab 文件）：全部运行时生成。

## Further Notes

- 详细的原版代码事实（`OptionsWidget` 结构、`SelectableOption`、`SetOptionGameSelected` 高亮逻辑、GAME 菜单三类型映射、`ExtrasMenuWidget` 同型结构、`ModServiceProvider` 空壳、`MenuRegister.cs` 先例全文、GenericElements 场景结构）与风险/待实测点表见仓库内 `docs/spec/menu-api-spec.md` 的原始实现细则部分（本次修缮保留于文档历史 / 实现顺序小节）。
- 实现顺序建议：挂载点定位器 → 模板定位器 → 数据模型与注册扩展 → 选项注入（API-A）→ 子页与嵌套导航（API-B）→ 实机验证。
- 本仓库 issue tracker = GitHub，`gh` CLI；GitHub issue tracker 文档见 `docs/agents/issue-tracker.md`。
- 游戏环境：渎神 1（Unity 2017.4.40f1 / net35 / BepInEx Mono），ModdingAPI 3.0.1；internal 反射无需 SkipVisibilityChecks。
