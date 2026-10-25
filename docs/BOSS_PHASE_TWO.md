# Boss Phase Two / Boss 二阶段

## English

`boss_001` now has a fixed second-phase pattern. On its own action, at or below
50% HP, it enters phase two once. The phase remains active even if it heals.
Crossing the threshold does not grant an extra action.

| Boss action | Effect |
| --- | --- |
| Phase one | Existing weighted AI, excluding the configured phase-two AOE |
| First phase-two action | Display `Phase 2`, then charge without damage or MP cost |
| Next boss action | Release the configured AOE against all living opponents |
| Following actions | Repeat charge, AOE, charge, AOE |

The charge ends the boss's action normally. The timeline resumes, so players can
act when their action values reach zero. It does not guarantee a full-party turn;
the number of intervening actions depends on speed and current action values.
An amber `Charging: MagmaBurst` label stays above the boss HP bar and beside its
timeline portrait until release. The timeline warning remains visible in close-up shots.

### Configuration

In `Enemy Character Data Base`, on `boss_001`:

- `Enemy Ai Type`: `Boss`.
- `Boss Phase Two Hp Ratio`: `0.5`.
- `Boss Phase Two Aoe Skill`: `MagmaBurst` (`Damage`, `EnemyAll`).

The existing `MagmaBurst` remains unchanged: MP cost 0, power 10. Damage still
uses `Attack + skill.power`; with the current boss attack 70, that is 80 per
living target. These numbers are a starting configuration, not balanced content.

The fixed pattern determines this skill's cadence; its random-AI weight and
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

### Verification

63 Unity Play Mode assertions passed, covering the half-HP boundary, phase-one
selection, separate charge/release actions, timeline unlock, living multi-target
damage, one MP payment, repeated cycles, MP failure fallback, independent enemy
instances, healing, death cancellation, initialization reset, and unchanged source
assets. The 1920 x 1080 capture confirms the timeline warning survives a boss
close-up. Temporary checks and capture are in ignored `Library/BossPhase`, not
runtime assets. Full difficulty balancing and a standalone build remain pending.

## 中文

`boss_001` 在自己的行动开始时检查血量：HP 不高于 50% 时进入二阶段，
每场战斗只触发一次。之后回血也不会退回一阶段，转阶段不会赠送额外行动。

| Boss 行动 | 效果 |
| --- | --- |
| 一阶段 | 沿用原技能权重选择，但不随机使用指定的二阶段 AOE |
| 二阶段第一次行动 | 显示 `Phase 2`，然后蓄力；不伤害、不扣 MP |
| 下次轮到 Boss | 对全部存活对手释放指定 AOE |
| 后续行动 | 固定重复“蓄力、AOE、蓄力、AOE” |

蓄力后正常结束本次行动，时间轴恢复推进。玩家是否能行动、能行动几人，
取决于速度和当时的行动值，不是强制全队各行动一次。
血条上方和行动顺序头像旁的琥珀色 `Charging: MagmaBurst` 会保留到释放前。
行动顺序旁的提示不受特写镜头裁切影响。

### 配置

在敌人数据库的 `boss_001` 中：

- `Enemy Ai Type` 为 `Boss`。
- `Boss Phase Two Hp Ratio` 为 `0.5`。
- `Boss Phase Two Aoe Skill` 引用 `MagmaBurst`，类型必须是 `Damage / EnemyAll`。

没有更改原技能数值：`MagmaBurst` 消耗 0 MP，`power` 为 10。
伤害仍是 `Attack + skill.power`；目前 Boss 攻击力 70，所以每个存活目标受伤 80。
本次先接机制，不代表这些数值已经完成平衡。

指定 AOE 的释放节奏由固定循环控制，不再采用它的随机技能权重和普通 AI 冷却。
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

### 验证

Unity Play Mode 的 63 个断言通过，覆盖半血边界、一阶段技能选择、蓄力与释放
分属两次行动、恢复行动锁、多人群攻与单次扣 MP、重复循环、MP 不足回退、
不同敌人状态隔离、回血、死亡取消、初始化重置和不修改源资源。
1920 x 1080 截图确认 Boss 特写时行动顺序旁的提示仍可见。
临时检查源码和截图保存在忽略的 `Library/BossPhase`，不进入运行时资源。
完整难度平衡和独立构建尚未验收。
