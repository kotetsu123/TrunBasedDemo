# Basic Attack Feedback / 普通攻击反馈

## English

### Scope

The player's Attack command and the enemy's existing Attack skill now use a short
target-directed lunge, resolve the original damage once at full extension, then
return the display model to rest. This is a visual gesture, not movement to the
target or a physics collision. Fire, Burn, MagmaBurst, healing, items, damage
formulas, skill costs, and the boss rotation are unchanged. No audio, flash, or
additional attack particles are included.

### Configuration and Hierarchy

Both battle prefabs have `BattleAttackFeedback` and this display hierarchy:

```text
Actor root: controller, colliders, camera/floating-text anchors
  VisualRoot: existing hit recoil and enemy death fade
    AttackRoot: basic-attack translation and the original mesh
```

The original mesh/renderer file IDs, materials, logical root pose, colliders, and
anchors are preserved. Separate attack and hit transforms allow recoil to overlap
the attack without competing to write the same local position. Put replacement
models beneath `VisualRoot/AttackRoot`, leaving gameplay colliders and anchors
outside the animated display hierarchy.

Defaults are a 0.45-world-unit lunge over 0.14 seconds, a 0.06-second impact hold,
and a 0.2-second recovery. Movement follows the target on the horizontal plane,
is capped at one quarter of the actor-target distance, and is converted into the
motion parent's local space. Rotation and scale do not animate. Scaled game time
pauses the gesture and its impact together.

The enemy prefab references its actual Attack `SkillData` in `Attack Skill`.
Matching uses asset identity, not the skill's displayed name. Only that skill
gets the lunge; other enemy skills keep their existing presentation and rules.

### Timing and Cleanup

`BattleManager` supplies the impact callback. Player Attack still uses
`target.TakeDamage(actor.data.Attack)`; enemy Attack still uses
`actor.UseSkill(skill, target)`. Existing HP events, floating damage, recoil, and
enemy death feedback therefore remain the hit's downstream behavior.

The player attack reuses the existing player-side interaction camera, allowing
both combatants to be seen. It waits for that camera before starting the gesture
and releases its camera lock in `finally`. The existing action coroutine owns the
whole gesture, so the turn cannot finish before recovery.

Invalid/dead actors or targets cannot apply delayed damage, and the selected
target reference is retained rather than redirected to a replacement enemy.
Attacker death or feedback disable cancels pending impact. Target destruction
does not prevent the attacker from recovering. Missing/disabled feedback uses
the original damage fallback. Duplicate playback is rejected, and disable,
destruction, tween cancellation, and normal completion restore the captured
rest position and clean up only this component's tween.

### Verification

42 Unity Play Mode assertions passed on 2026-11-01 using the real prefabs and
attack sequences, including player/enemy impact timing, unchanged damage/cost,
camera framing and unlock, stable logical/collider/anchor positions, repeated
attacks, pause, overlapping recoil, missing/disabled feedback, invalidated targets,
attacker death, reentrancy, custom offsets, transformed display parents, and
lethal-hit/final-victory integration. Real 1920x1080 game captures were inspected.
The existing 48 death-feedback assertions also passed again after updating only
the test renderer lookup for the nested mesh. No scene or real save was changed.
The 57-assertion boss rotation regression also passed again, retaining the Fire,
Burn, charge, enhanced AOE cycle and its original skill costs and damage.
Checks and screenshots are archived under ignored `Library/AttackFeedbackChecks`;
this is not standalone-build acceptance or final approval of the gesture's feel.

### Timeline Lifecycle Follow-up

The user subsequently reported a destroyed `TimelineIconView` being accessed
during next-actor refresh. The exact stack was reproduced with real
`BattleManager.LateUpdate`: the enemy model was still fading while its timeline
view had already been destroyed. Earlier action checks paused automatic battle
updates and did not exercise this ordering.

`TimeLineUI` now synchronizes its cache with live authoritative icons, removes
dead/destroyed/replaced entries, checks old-next references, clears empty orders,
and unsubscribes both ordering and action events. View entry points also guard
Unity's destroyed-object null state. Position tweens are owned by each
`TimeLineIcon` and killed on disable/destroy rather than retained inside a shared
sequence after an icon disappears.

20 focused Play Mode lifecycle assertions passed on 2026-11-01, including the
original LateUpdate reproduction, icon recreation, player revival, dangerous
glow, empty ordering, enable/disable subscriptions, simultaneous deaths, and
position-tween cleanup. That run produced neither the reported exception nor
destroyed-target DOTween startup warnings. Reproduction and checks are archived
under ignored `Library/TimelineLifecycleChecks`. The earlier 147 checks were not
all rerun for this UI-only follow-up; they do not substitute for this new case.

## 中文

### 范围

玩家的 Attack 指令和敌人现有的 Attack 技能，现在会先朝目标短促前探，在伸到最前方时
结算一次原有伤害，再让显示模型归位。这是视觉动作，不是实际移动到目标身边或通过碰撞
判定命中。Fire、Burn、MagmaBurst、治疗、道具、伤害公式、技能费用和 Boss 轮转不变。
本次不加入音效、闪白或额外攻击粒子。

### 配置与层级

双方战斗 Prefab 均配置 `BattleAttackFeedback`，显示层级如下：

```text
角色根节点：控制器、碰撞体、镜头及飘字锚点
  VisualRoot：原有受击晃动及敌人死亡淡出
    AttackRoot：普通攻击位移及原网格
```

保留原网格/Renderer 的 file ID、材质、逻辑根节点位置姿态、碰撞体及锚点。
出手与受击使用不同 Transform，允许同时播放，避免互相争写本地坐标。
以后替换模型时放在 `VisualRoot/AttackRoot` 下，玩法碰撞体与锚点留在动画显示层级之外。

默认用 0.14 秒前探 0.45 个世界单位，命中停留 0.06 秒，再用 0.2 秒归位。
位移沿朝向目标的水平方向，上限为双方距离的四分之一，并转换为显示节点父物体的本地
坐标；不播放旋转或缩放。动作和命中时机均遵循游戏时间，暂停时一起停止。

敌方 Prefab 的 `Attack Skill` 引用实际 Attack `SkillData`，按资源身份匹配，不判断
技能显示名称。只有这项技能前探，其他敌方技能保留原表现和规则。

### 时机与清理

`BattleManager` 提供命中回调。玩家普攻仍使用 `target.TakeDamage(actor.data.Attack)`，
敌人普攻仍使用 `actor.UseSkill(skill, target)`；命中之后继续沿用现有 HP 事件、伤害飘字、
受击晃动和敌人死亡反馈。

玩家普攻复用现有偏玩家侧的交互镜头，让双方同时入镜，等待镜头就位后开始出手，
并在 `finally` 中解除镜头锁定。整个动作由原行动协程等待，因此归位前不会结束该行动。

角色或目标已经无效/死亡时，不会补算延迟伤害；保留确认的目标引用，不会转打补位敌人。
攻击者死亡或反馈组件关闭时取消尚未发生的命中；目标被销毁也不妨碍攻击者归位。
缺少/禁用反馈组件时仍回退原伤害逻辑。拒绝重复播放；组件关闭、销毁、动画取消和正常
播完时，恢复记录的原位置，并且只清理自身动画。

### 验证

2026-11-01 使用真实 Prefab 与攻击流程的 42 项 Unity Play Mode 断言通过，覆盖双方
命中时机、伤害/费用不变、镜头构图及解锁、逻辑位置/碰撞体/锚点不动、连续攻击、暂停、
受击叠加、缺少/禁用反馈、目标失效、攻击者死亡、重入、自定义位置、显示父节点旋转缩放，
以及致命一击和最终胜利衔接。已检查真实 1920x1080 游戏截图。
原 48 项死亡反馈回归也通过，测试仅调整嵌套网格的 Renderer 查询方式；未修改场景或
真实存档。另行重跑的 57 项 Boss 轮转回归通过，保留 Fire、Burn、蓄力、强化群攻的顺序
及原技能费用和伤害。临时检查和截图归档忽略的 `Library/AttackFeedbackChecks`，不作为游戏资源
提交；这不等于独立构建验收或用户对动作手感的最终确认。

### 时间轴生命周期后续修复

用户随后报告刷新下一位时访问已销毁的 `TimelineIconView`。已经用真实
`BattleManager.LateUpdate` 复现相同堆栈：敌人模型仍在淡出，但时间轴头像已经销毁。
此前的动作检查暂停了自动战斗更新，没有覆盖这一刷新顺序。

`TimeLineUI` 现在根据当前有效图标同步缓存，移除死亡、销毁或被替换的记录，检查旧
下一位引用、清理空排序，并正确解除排序和行动两类事件订阅。View 入口也处理 Unity
对象已销毁的判空。位置动画改由各 `TimeLineIcon` 自己持有，关闭或销毁时结束，
不再让共享 Sequence 保留已经消失的图标。

2026-11-01 的 20 项专项 Play Mode 生命周期断言通过，覆盖原 LateUpdate 复现路径、
图标重建、玩家复活、危险高亮、空排序、启用/关闭订阅、多人同时死亡及位移动画清理。
该次运行没有原异常，也没有已销毁目标的 DOTween 启动警告。复现与检查归档在忽略的
`Library/TimelineLifecycleChecks`。此前 147 项检查没有在这次纯 UI 修复后全部重跑，
也不能代替本次补充的场景。
