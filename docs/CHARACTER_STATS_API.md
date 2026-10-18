# Character Stats API / 角色属性接口

## 范围

这次只提供数据与选人接口，不创建 UI、不修改场景，不处理快捷键、Canvas、暂停或装备。

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

面板布局、按钮入口和语言样式暂不预设，之后按你的 UI 方案连接。
