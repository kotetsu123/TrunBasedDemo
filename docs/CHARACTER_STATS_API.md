# Character Stats API / 角色属性接口

## 范围

本文先说明数据与选人接口，再说明下方 Status 页面的接入。属性接口本身不处理 UI 布局、快捷键、Canvas、暂停或装备。

- `FieldCharacterStatsController` 从 `PartyRuntimeState` 读取当前队伍，不从数据库模板读取属性。
- `CharacterStatsSnapshot` 是选中角色某一时刻的只读属性快照，不会反向修改角色数据。
- 原始队伍成员仍然是运行时引用；UI 只读取它们，不应通过它们写入属性。

## 数据

| 属性 | 含义 |
| --- | --- |
| `Members` | 当前运行时队伍，顺序与队伍一致，包含阵亡队员 |
| `SelectedIndex` | 选中队员在当前队伍中的索引；没有队员时为 `-1` |
| `SelectedMember` | 选中队员的运行时对象；没有队员时为 `null` |
| `CurrentStats` | 最近一次刷新产生的只读快照；空队伍时为 `null` |

快照字段：`CharacterId`、`Name`、`Portrait`、`Level`、`Exp`、`ExpToNextLevel`、`Hp`、`MaxHp`、`Mp`、`MaxMp`、`Attack`、`Speed`、`IsDead`。

`Exp` 是当前等级内的经验；`ExpToNextLevel` 是升到下一级所需经验。`IsDead` 同时考虑阵亡标记和 HP 归零，其余数值原样读取，显示格式由 UI 决定。

## 调用

| 方法 | 行为 |
| --- | --- |
| `Refresh()` | 重新读取队伍和角色属性，优先按 `characterId` 保留当前选择 |
| `SelectMember(int index)` | 按索引选择队员，成功返回 `true` |
| `SelectMember(string characterId)` | 按稳定 ID 选择队员，成功返回 `true` |
| `SelectNextMember()` | 循环选择下一名队员 |
| `SelectPreviousMember()` | 循环选择上一名队员 |

非法索引、空 ID 或不存在的 ID 返回 `false`，不会改变当前选择。前后切换会跳过空成员，但不会跳过阵亡成员。空队伍的前后切换返回 `false`，并清空当前快照。

`OnStatsChanged(CharacterStatsSnapshot stats)` 在刷新或切换时发出。`stats == null` 表示没有可显示的角色，UI 应清空旧数据或显示空状态。

通过 Inspector 绑定 `Button.onClick` 时使用返回 `void` 的入口：`OnClickMember(int index)`、`OnClickNextMember()` 和 `OnClickPreviousMember()`。上表返回 `bool` 的选人方法供代码调用，用于检查选择是否成功。

## 接入约定

1. 把 `FieldCharacterStatsController` 挂到之后的属性面板或 UI 管理对象上。
2. UI 订阅 `OnStatsChanged` 后调用一次 `Refresh()`，再把快照值填进自己的控件。
3. 每次显示面板，以及同场景读档成功后，调用 `Refresh()`。读档会替换运行时角色对象，因此不能长期缓存旧的 `SelectedMember`。
4. 关闭或销毁 UI 时解除自己的事件订阅。控制器禁用或销毁时也会解除角色 HP/MP 事件订阅，重新启用时会刷新。

控制器启用期间，选中队员的 HP/MP 事件会触发刷新。正常升级会发布这些事件，所以也能刷新升级后的其他属性。单独直接修改 Attack、Speed 或替换队伍不会自动通知这个接口，调用方需要显式调用 `Refresh()`。

这里只提供数据事件，不负责面板是否可见；CanvasGroup 隐藏也不会禁用组件，因此每次打开时仍需刷新。读档后显式刷新会按 ID 重新绑定新角色对象，并解除旧对象的订阅。

```csharp
// Subscribe before the first refresh; unsubscribe when the view no longer listens.
statsController.OnStatsChanged += RenderStats;
statsController.Refresh();

// UI callbacks can select by slot, stable ID, or navigation direction.
statsController.SelectMember(0);
statsController.SelectMember("your_character_id");
statsController.SelectNextMember();
statsController.SelectPreviousMember();
```

以上接口不限定面板布局或入口。当前接入的 Status 页面初版如下。

## Status 页面初版（2026-10-18）

`FildScene` 和 `Boss_FieldScene` 已接入 `Assets/Prefabs/UI/FieldUI/StatusPanel.prefab`，从 ESC 菜单的 Status 按钮打开。此页使用现有头像和运行时属性，支持鼠标点选队友、左右按钮以及 A/D 或左右方向键切换。阵亡角色仍可查看，空队伍显示空状态。

`FieldStatusPanelController` 订阅属性接口并显示数据，`FieldStatusMemberView` 负责队员条目。每次打开以及菜单内同场景读档成功后刷新；读档重建角色时按 ID 重新绑定。HP/MP 事件和正常升级会同步刷新显示。装备和技能页仍留待后续整合；Status / Item 底部导航见下节。

Status 打开时保持探索暂停。从 ESC 菜单打开时，关闭按钮、右键或 ESC 返回原 ESC 菜单，继续暂停；再关闭 ESC 菜单才恢复探索。从头像 HUD 打开时，关闭 Status 直接恢复探索。背包快捷键在 Status 打开及刚关闭的同一帧不生效。

页面与队员条目都是可编辑的 uGUI 预制体，后续布局直接在 Unity 中编辑 `StatusPanel.prefab` 和 `StatusMember.prefab`。两个 Field 场景已保存预制体实例及菜单、输入引用，运行只依赖这些预制体和运行时脚本。

首次接入时已检查队友切换、属性事件、升级、存档数据重建后的绑定、空队伍、阵亡与返回暂停状态，并查看了 1280×720 和 800×600 两种比例的预览。存档数据重建在内存中进行，不代替手动点击 Load 的完整回归。初始布局生成器和临时 Play Mode 检查工具已移除。

## 探索头像 HUD（2026-10-19）

两个 Field 场景使用 `Assets/Prefabs/UI/FieldUI/PortraitHud.prefab`，位于右上角。上半弧红色显示 HP，下半弧蓝色显示 MP；点击头像打开当前领队的 Status，姓名和详细属性在 Status 中查看，HUD 不显示姓名或悬停提示。教程、对话或其他暂停菜单打开时，头像入口不可用。

头像使用圆形遮罩，外层为 `Assets/Art/UI/PortraitHudFrame.png` 的蓝银色云纹装饰框。装饰框不接收射线，位于血蓝弧内缘上方；弧形颜色降低饱和度，保留独立的暗色空槽。框图由内置图像生成工具制作，提示要求为居中透明圆孔、细蓝银色边框、右下云纹、无头像与文字；HP/MP 仍由 uGUI 动态填充，不是框图的一部分。

`FieldPortraitHudController` 显示运行时队伍中的第一位非空成员，不跟随 Status 页的浏览选择切换领队。HP/MP 事件和数值变化会刷新弧形；队伍对象被读档或战斗返回替换时自动重新绑定。空队伍隐藏 HUD，阵亡时显示 KO，零最大值显示空弧形。

原左侧队伍 HUD 在两个场景内保留但关闭。道具的队友目标选择面板仍独立使用。Boss 场景的测试入队按钮移至左上角，为头像留出位置。头像 HUD、图片和运行时脚本可直接编辑，临时制作辅助脚本已在完成验证后移除。

## Status / Item 菜单（2026-10-20）

两个 Field 场景共用 `Assets/Prefabs/UI/FieldUI/SystemMenu.prefab` 的底部 Status、Item 页签和关闭入口。`FieldSystemMenuController` 只协调现有属性页与背包页，不新增装备、技能、存档页面，也不改成跨场景常驻 UI。场景实例分别绑定自己的 `statusPanel` 和 `inventoryPanel`；控制器初始化时绑定属性页与原背包输入组件。

`OpenStatus()`、`OpenItems()` 切换到对应页面，`ToggleItems()` 供原 B 键入口使用：探索中打开 Item，Status 中切到 Item，Item 中关闭整个菜单。页签切换先立即隐藏另一页与道具选人弹窗，再显示并刷新新页面，全程保持探索暂停。背包的 `IsOpen` 由当前页面推导，不再另行记录一个可能与页面不一致的开关。

从头像或 B 键进入时，关闭后恢复探索；从 ESC 菜单的 Status 入口进入时，无论最终停在哪一页，关闭都返回 ESC 菜单并保持暂停。顶部关闭按钮直接关闭整个菜单；ESC 或右键在道具选人期间只取消选人，再次按下才关闭菜单。同一帧的取消、关闭由控制器消费，避免其他输入组件再次打开 ESC 菜单。教程或其他暂停界面打开时，B 键不能叠加打开系统菜单。

Item 沿用现有运行时背包、槽位顺序、拖放与道具效果。格子、说明和使用按钮布局为底部导航留出空间，长列表仍可滚动；选人弹窗和反馈提示使用独立 Canvas 排序，保持在页面内容上方。切回 Status 会重新读取角色属性，因此使用道具后的 HP/MP 可直接查看。Save/Load 仍在原 ESC 菜单中。

本次已在两个场景检查头像入口、页签点击、B 键入口、ESC 返回、道具选人取消、切页清理、实际治疗与扣除一份道具、治疗后属性刷新，以及空队伍。1280×720 和 800×600 的截图及边界检查确认底部页签、默认 20 个格子和数量文字不互相遮挡；实际键盘输入验证了 B 打开 Item、ESC 关闭后恢复探索。检查使用运行时数据副本并在结束时恢复，没有写入存档文件。临时制作和 Play Mode 检查脚本已移除。
