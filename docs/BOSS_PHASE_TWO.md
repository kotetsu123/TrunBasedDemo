# Boss Phase Two / Boss 二阶段

## English

`boss_001` now has a fixed second-phase pattern. On its own action, at or below
50% HP, it enters phase two once. The phase remains active even if it heals.
Crossing the threshold does not grant an extra action.

| Boss action | Effect |
| --- | --- |
| Phase one | Existing weighted AI, including its configured AOE |
| First phase-two action | Display `Phase 2`, then charge without damage or MP cost |
| Next boss action | Release the configured AOE against all living opponents |
| Following actions | Repeat charge, AOE, charge, AOE |

The charge ends the boss's action normally. The timeline resumes, so players can
act when their action values reach zero. It does not guarantee a full-party turn;
the number of intervening actions depends on speed and current action values.
An amber `Charging: MagmaBurst` label stays above the boss HP bar and beside its
timeline portrait until release. The timeline warning remains visible in close-up shots.

### Dangerous Skill Warning

`SkillData.isDangerous` marks a skill for a preparation warning. It defaults to
false; the existing `MagmaBurst` asset has it enabled. The flag is visual metadata,
not a damage bonus or a new skill type.

When the boss charges a marked skill, its runtime `Character.intent` becomes
`ActionIntent.SkillDanerous` (the existing enum spelling is preserved). The
timeline icon uses a persistent orange glow, including while waiting, next, and
active. Normal next-actor colors and active highlighting cannot overwrite it.
Changes also apply immediately when the timeline state or order has not changed.

The warning remains through the release impact and clears when the release
sequence finishes. Cancellation, insufficient MP, missing targets, death,
initialization, and battle results clear it too. An unmarked skill may still
charge and display the existing text without receiving a danger glow. Phase-one
instant casts keep their original flow; marking a skill does not add a charge turn.

The warning color and opacity are configurable on `TimelineIconView` as
`Dangerous Glow Color` and `Dangerous Alpha`. No scene wiring changes are needed.

### Configuration

In `Enemy Character Data Base`, on `boss_001`:

- `Enemy Ai Type`: `Boss`.
- `Boss Phase Two Hp Ratio`: `0.5`.
- `Boss Phase Two Aoe Skill`: `MagmaBurst` (`Damage`, `EnemyAll`).
- On the `MagmaBurst` skill asset, `Is Dangerous`: enabled.

The existing `MagmaBurst` numbers remain unchanged: MP cost 0, power 10. Damage still
uses `Attack + skill.power`; with the current boss attack 70, that is 80 per
living target. These numbers are a starting configuration, not balanced content.

Phase one still uses this skill's random-AI weight and cooldown. In phase two,
the fixed pattern determines its cadence; its random-AI weight and
`enemyCooldownTurns` do not apply to scripted releases. Other enemies retain
normal weighted selection and cooldowns. Bosses without a valid phase-two skill
also retain the previous AI.

MP is charged once on release, not once per target. If the boss cannot afford
the AOE, it clears a pending charge and falls back to an affordable normal skill.
Death cancels the charge. Initializing a new battle clears both runtime flags.
No runtime charge state is stored in shared ScriptableObjects or field saves.

### Implementation

- `Character`: phase threshold and AOE reference; `Copy()` preserves configuration.
- `EnemyController`: per-instance `IsBossPhaseTwo` and `IsBossCharging` flags.
- `BattleManager.WaitForEnemyAction()`: fixed charge/release flow before normal AI.
- `HpBar.SetIntent()`: persistent, non-interactive charge label.
- `TimeLineIcon.SetIntent()`: matching screen-space warning beside the boss portrait.
- `TimelineIconView.SetDangerous()`: danger glow layered over normal timeline states.

### Verification

The original implementation passed 63 Unity Play Mode assertions, covering the half-HP boundary, phase-one
selection, separate charge/release actions, timeline unlock, living multi-target
damage, one MP payment, repeated cycles, MP failure fallback, independent enemy
instances, healing, death cancellation, initialization reset, and unchanged source
assets. The 1920 x 1080 capture confirms the timeline warning survives a boss
close-up. Temporary checks and capture are in ignored `Library/BossPhase`, not
runtime assets. Full difficulty balancing and a standalone build remain pending.

On 2026-10-27, 44 additional Unity Play Mode assertions passed for the dangerous
intent warning: asset configuration, unchanged damage/cost, immediate color
refresh, all three timeline states, cache rebuilds, actor switches, release,
ordinary-skill fallback, missing targets, death, initialization, and all battle
results. The 1920 x 1080 captures show the orange glow during charging and its
removal after release. Checks and captures are archived in ignored
`Library/DangerIntentChecks`; no scene or real save data was modified.

## 中文

`boss_001` 在自己的行动开始时检查血量：HP 不高于 50% 时进入二阶段，
每场战斗只触发一次。之后回血也不会退回一阶段，转阶段不会赠送额外行动。

| Boss 行动 | 效果 |
| --- | --- |
| 一阶段 | 沿用原技能权重选择，也可以使用已配置的 AOE |
| 二阶段第一次行动 | 显示 `Phase 2`，然后蓄力；不伤害、不扣 MP |
| 下次轮到 Boss | 对全部存活对手释放指定 AOE |
| 后续行动 | 固定重复“蓄力、AOE、蓄力、AOE” |

蓄力后正常结束本次行动，时间轴恢复推进。玩家是否能行动、能行动几人，
取决于速度和当时的行动值，不是强制全队各行动一次。
血条上方和行动顺序头像旁的琥珀色 `Charging: MagmaBurst` 会保留到释放前。
行动顺序旁的提示不受特写镜头裁切影响。

### 危险技能预警

`SkillData.isDangerous` 用于标记需要准备预警的技能，默认关闭；现有的
`MagmaBurst` 资源已开启。它只是表现标记，不会提高伤害，也不是新的技能类型。

Boss 蓄力准备已标记的技能时，运行时 `Character.intent` 变为
`ActionIntent.SkillDanerous`，沿用已有枚举的拼写。时间轴头像持续显示橙色警告光，
覆盖等待、下一个行动者和当前行动者三种状态，普通的阵营颜色和行动高亮不会将它
覆盖。即使顺序和状态没有变化，颜色也会立即更新。

预警保留到大招命中并结束本次释放演出；取消蓄力、MP 不足、无有效目标、阵亡、
重新初始化或战斗结算时都会清除。没有危险标记的技能仍可蓄力并显示原文字提示，
但不会出现危险色。一阶段直接施放技能的流程不变，勾选标记不会额外增加蓄力回合。

可以在 `TimelineIconView` 中调整 `Dangerous Glow Color` 和 `Dangerous Alpha`，
分别控制预警颜色与不透明度，不需要重新绑定场景。

### 配置

在敌人数据库的 `boss_001` 中：

- `Enemy Ai Type` 为 `Boss`。
- `Boss Phase Two Hp Ratio` 为 `0.5`。
- `Boss Phase Two Aoe Skill` 引用 `MagmaBurst`，类型必须是 `Damage / EnemyAll`。
- `MagmaBurst` 技能资源的 `Is Dangerous` 已开启。

没有更改原技能数值：`MagmaBurst` 消耗 0 MP，`power` 为 10。
伤害仍是 `Attack + skill.power`；目前 Boss 攻击力 70，所以每个存活目标受伤 80。
本次先接机制，不代表这些数值已经完成平衡。

一阶段仍采用该技能的随机权重和冷却；二阶段指定 AOE 的释放节奏由固定循环控制，
不再采用它的随机技能权重和普通 AI 冷却。
普通敌人、没有合法二阶段配置的 Boss 仍使用原有 AI。

释放时只扣一次 MP，不按目标数量重复扣费。MP 不足时取消待释放状态，
尝试使用可以支付的普通技能；阵亡时取消蓄力。重新初始化战斗时清空两个状态。
阶段和蓄力是每只敌人的运行时状态，不写入共享 SO 或探索存档。

### 代码分工

- `Character` 保存阶段阈值、技能引用，`Copy()` 复制这些配置。
- `EnemyController` 保存每只 Boss 独立的 `IsBossPhaseTwo`、`IsBossCharging`。
- `BattleManager.WaitForEnemyAction()` 在普通 AI 前处理固定蓄力/释放流程。
- `HpBar.SetIntent()` 显示不拦截鼠标的持续蓄力提示。
- `TimeLineIcon.SetIntent()` 在屏幕上的行动顺序头像旁同步显示提示。
- `TimelineIconView.SetDangerous()` 在普通时间轴状态之上叠加危险警告色。

### 验证

原版实现通过了 Unity Play Mode 的 63 个断言，覆盖半血边界、一阶段技能选择、蓄力与释放
分属两次行动、恢复行动锁、多人群攻与单次扣 MP、重复循环、MP 不足回退、
不同敌人状态隔离、回血、死亡取消、初始化重置和不修改源资源。
1920 x 1080 截图确认 Boss 特写时行动顺序旁的提示仍可见。
临时检查源码和截图保存在忽略的 `Library/BossPhase`，不进入运行时资源。
完整难度平衡和独立构建尚未验收。

2026-10-27 另有 44 个 Unity Play Mode 断言通过，覆盖危险标记配置、伤害与费用不变、
立即变色、三种时间轴状态、缓存重建、切换行动者、正常释放、普通技能回退、
无有效目标、阵亡、初始化及三种战斗结算。1920 x 1080 截图确认蓄力时出现橙色警告，
释放后警告消失。测试源码和截图归档在忽略的 `Library/DangerIntentChecks`，
未修改场景或真实存档。
