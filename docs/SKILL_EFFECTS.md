# DOT and Party Healing / 持续伤害与群体治疗

## English

### Configuration

Skill effects and target scope are separate. Group healing is `Heal` plus
`AllyAll`, not another `SkillType`. Existing enum values remain unchanged:
`Damage = 0`, `Heal = 1`, `Revive = 2`; the new `Dot` value is 3.

| Template | Skill Type | Target Type | Power | MP Cost | DOT Duration |
| --- | --- | --- | --- | --- | --- |
| Burn | Dot | EnemySingle | 10 per tick | 5 | 2 target actions |
| PartyHeal | Heal | AllyAll | 40 per living ally | 25 total | Not used |

The templates are in `Assets/Scripts/Data/SkillScriptableObject`. They are not
automatically assigned to Paul, other party members, or the boss. Their values
are examples, not final balance. A DOT can also target `EnemyAll`.

### DOT Rules

- Applying a DOT does not deal immediate damage. `power` is fixed damage per
  tick, without adding the caster's Attack.
- The affected unit takes one tick at the end of its own completed action,
  including an enemy charging action. Other units' actions do not advance it.
- One DOT is supported per unit. Reapplication replaces the DOT name/damage and
  refreshes its duration; effects do not stack or share mutable state.
- A timeline label shows the effect name and remaining ticks, separately from
  the boss charge warning.
- Healing restores HP without removing the DOT. Death, revival, initialization,
  victory, defeat, and escape clear the effect. DOT state is battle-only and is
  not written to shared skill assets, party data, or field saves.
- Lethal ticks use normal death and battle-result handling. DOTs do not tick
  after the battle has ended.

### Party Healing and Group Skills

Selecting `Heal / AllyAll` previews the allied group. Confirm with the existing
Space input; no single target needs to be selected. Right-click/Escape still
returns to the skill list. The skill heals every living, on-field ally,
including the caster, up to each unit's MaxHp. It never revives fallen allies.

MP is paid once for the cast, not once per target. Empty target groups do not
consume MP. Full-health allies do not make a normal healing skill unavailable.
Player group damage/DOT and enemy group skills share the same target-resolution
and presentation path. Normal enemy AI can select DOT skills using its existing
weights/cooldowns; its existing heal trigger and boss-healing policy remain.

### Scope

This change supplies skill behavior only. Boss phase-two selection still uses
the previously implemented fixed charge/AOE cycle. A rotation mixing DOT,
single-target attacks, and occasional charged AOE is a separate next step.
It does not add defense, equipment, cleanse, DOT stacking, or a general buff
framework.

### Verification

45 Unity Play Mode assertions passed, covering group-skill confirmation, living
targets, caster inclusion, HP caps, one MP payment, empty groups, DOT timing,
refresh/expiry, state isolation, existing charged AOE, enemy DOT selection and
cooldowns, escape, revival, and lethal DOT victory/defeat. A 1920 x 1080 capture
checks that charge and DOT labels occupy separate rows. Test code/results are
kept in ignored `Library/SkillEffects`, not shipped under `Assets`.

The rapid scene-reload test produced DOTween destroyed-target warnings for
RectTransform/CanvasGroup tweens. Their cleanup remains a separate follow-up;
the assertions passed, but this does not constitute a clean full-game playthrough
or standalone-build verification.

## 中文

### 技能配置

效果和目标范围分开配置。群体治疗使用 `Heal + AllyAll`，不新增重复的
`GroupHeal` 类型。原有枚举编号不变：`Damage = 0`、`Heal = 1`、`Revive = 2`，
新增 `Dot = 3`。

| 模板 | Skill Type | Target Type | Power | MP Cost | DOT 持续 |
| --- | --- | --- | --- | --- | --- |
| Burn | Dot | EnemySingle | 每次扣血 10 | 5 | 目标自己的 2 次行动 |
| PartyHeal | Heal | AllyAll | 每位存活队友恢复 40 | 总计 25 | 不使用 |

模板位于 `Assets/Scripts/Data/SkillScriptableObject`，不会自动分配给 Paul、
其他队友或 Boss。数值仅用于试验，尚未平衡。DOT 也可以配置为 `EnemyAll`。

### DOT 规则

- 施加时不立即伤害。`power` 是每次固定扣血值，不叠加施法者攻击力。
- 在受影响者自己的行动结束时扣一次血，包括敌方的蓄力行动。
  其他角色行动不会消耗它的持续次数。
- 每个单位暂时只保留一个 DOT。再次施加时覆盖名称和伤害，并刷新持续次数，
  不叠层，各角色状态独立。
- 行动顺序头像旁显示效果名和剩余次数，与 Boss 的蓄力提示分开。
- 治疗只恢复 HP，不解除 DOT。死亡、复活、初始化、胜利、失败和逃跑时清除。
  DOT 只存在于本场战斗，不写入共享 SO、队伍数据或探索存档。
- DOT 致死沿用正常死亡及战斗结算。战斗结束后不再追加 DOT 扣血。

### 群体治疗与群体技能

选择 `Heal / AllyAll` 后预览己方全队，沿用空格确认，不需要选定某个队友。
右键或 Escape 仍返回技能列表。治疗包括施法者在内的全部存活、在场队友，
每人的 HP 最多恢复到 MaxHp，不会治疗或复活阵亡角色。

每次施法只扣一次 MP，不按目标人数重复扣费；没有合法目标时不扣费。
满血队友不会导致普通治疗技能被禁用。玩家的群伤/群体 DOT 和敌方群体技能
复用目标收集及表现流程。普通敌人的 DOT 参与原有权重和冷却；
原有敌方治疗触发条件、Boss 不使用通用治疗的策略保持不变。

### 本次范围

本次只补技能能力。Boss 二阶段目前仍是上一版固定蓄力/群攻，之后再单独调整为
单体、DOT 和偶尔蓄力群攻的轮转。没有新增防御、装备、净化、DOT 叠层或完整 Buff 框架。

### 验证

Unity Play Mode 的 45 个断言通过，覆盖群体技能确认、存活目标、包含施法者、HP 上限、
只扣一次 MP、空目标、DOT 时机/刷新/到期/状态隔离、原有蓄力群攻、敌方 DOT 选择及冷却、
逃跑、复活和 DOT 致死后的胜负结算。1920 x 1080 截图检查了蓄力和 DOT 提示分行显示。
测试源码和结果保存在忽略的 `Library/SkillEffects`，不留在 `Assets` 中。

快速重载场景的测试出现了 DOTween 对已销毁 RectTransform/CanvasGroup 的警告，
相关动画清理另行处理。断言通过不等于已完成无警告的完整通关或独立构建验收。
