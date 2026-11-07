# Burn Feedback / 灼烧反馈

Updated / 更新：2026-11-06

## English

### Behavior and Scope

Burn now has two battle-only visuals: a short orange/yellow spark burst when
the affected unit takes its own action-end DOT damage, and a flame with a small
remaining-tick counter below that unit's HUD. Applying Burn does not emit sparks.
An ordinary attack against a burning unit does not emit them either.

The initial timeline debuff text has been removed. Boss charging intent text
and dangerous-skill coloring remain. No sound, dynamic lights, model tint/scale,
new turn delay, or BattleManager presentation branch was added.

The original Burn asset remains unchanged: 0 MP, 10 fixed damage at the end of
each of two affected-unit actions, reapplication refreshes rather than stacks.
Its GUID remains `0eb4ed56dccf4b3ba39db54d1605da10`. Boss rotation is unchanged.

### Events and Resource Identity

- `DebuffState.SourceSkill` retains the applied skill reference alongside the
  existing effect, name, power, and duration snapshots. `Clear()` releases it.
- `BaseController.TickDebuffAtTurnEnd()` captures that reference before advancing
  the state, then emits `OnDebuffTicked(sourceSkill, actualDamage)` after positive
  damage. Actual damage is the HP difference, including a lethal tick capped by
  remaining HP. Final ticks retain their source even if expiration/death clears it.
- `BattleBurnFeedback` listens to that event. `BattleDebuffHud` listens to
  `OnDebuffChanged`. Both compare the configured original Burn asset by identity,
  not by display name or by treating every DOT as fire. Another DOT still deals
  normal damage but does not automatically receive this presentation.
- Neither listener controls damage, status duration, action timing, or costs.
  Disabling feedback cannot disable the underlying DOT.

### Prefab Wiring and Lifetime

`Player.prefab` and `Enemy.prefab` each contain `BattleBurnFeedback`, configured
with their controller, the original Burn skill, and the existing neutral
`BattleFireParticles` material. Defaults are 0.45 seconds and 18 sparks, with a
hard maximum of 48 particles. Stretched particles rise and fade in front of the
logical collider bounds; they have no gameplay collider, shadows, or material clones.

The bounded effect is restarted rather than accumulated. Scaled time pauses its
simulation and cleanup. A lethal tick may display during the existing death hold;
normal completion, disabling, and destruction clean up the owned particle root.

`Playerhud.prefab` and `EnemyHP.prefab` contain `BattleDebuffHud`, referenced by
`PlayerHudItem` and `HpBar` respectively. Their normal bind/unbind paths pass the
current owner. The view lazily creates a non-raycast `BurnStatus` image and `Turns`
label; it does not block model/HUD target selection. The player icon is 36 canvas
units, anchored below the HP/MP rows; the enemy icon is 28 units below its HP bar.
No canvas scaling or scene UI hierarchy was changed.

The counter shows remaining DOT ticks, not global battle rounds. Rebinding first
unsubscribes the old owner. Disabling hides the marker and unsubscribes; enabling
refreshes current state. Expiration, death, revival, initialization, and battle-end
cleanup hide the flame through the existing status-clearing flow.

### Flame Asset

The built-in image generation tool, not the fallback CLI, produced the transparent
bitmap copied into `Assets/Art/UI/BurnIcon.png`. Its `.meta` imports it as a single
sprite, maximum size 64, no mipmaps, alpha preserved, with GUID
`8ac78e05021a454da76a2ebcecf28e64`. The original generated alpha is unchanged.

Final generation prompt:

```text
Create one production game HUD status icon: a small stylized burning flame, centered on a genuinely transparent background. Flat, crisp, bold silhouette readable when scaled down to 24 x 24 pixels. Orange-red outer flame, warm golden inner flame, tiny pale-yellow hot center, subtle dark charcoal outline for contrast on light and dark game scenes. Slightly asymmetric rising flame tongues. No text, no numbers, no badge, no frame, no circle, no UI mockup, no background glow, no ground, no extra objects. Square canvas, icon fills about 82 percent of canvas height with clean transparent padding. This will be imported into a Unity turn-based RPG as the Burn debuff HUD sprite.
```

### Verification

Unity Play Mode passed 43 Burn presentation assertions, 57 rerun boss rotation
assertions, and 20 rerun timeline lifecycle assertions. The boss checks now expect
the HUD flame instead of the removed timeline label. Coverage includes the real
prefabs/skills, actual completed turns, independent owners, final/lethal ticks,
pause, reapplication, UI rebind/disable, identity matching, death hold, escape,
victory, dangerous warnings, and destroyed-icon/tween lifecycle cleanup.

Actual battle/HUD renders at 1280x720 and 800x600 were inspected. Checks verify
transparent sprite padding, visible flame pixels, in-canvas status bounds, and
live collider-free sparks. The final presentation and timeline checks reported no
Unity exceptions or destroyed-target tween warnings. Source scenes, Burn data,
and real saves were not modified by tests.

Temporary helpers, reports, and screenshots are archived under ignored
`Library/BurnPresentationChecks`, `Library/BossRotationChecks`, and
`Library/TimelineLifecycleChecks`, not shipped as game Assets. Script compilation
was checked separately. A standalone player build and user visual acceptance are
still pending; these checks are not a complete demo playthrough.

## 中文翻译

### 行为与范围

Burn 新增两种仅用于战斗的表现：受影响单位在自身行动结束受到 DOT 伤害时，
身上短暂闪过橙黄色火星；该单位的 HUD 下方显示火焰和小号剩余次数。
施加 Burn 时不播放火星，普通攻击命中已经灼烧的单位时也不播放。

初版时间轴 Debuff 文字已移除。Boss 蓄力意图文字和危险技能变色保留。
本次没有增加音效、动态灯光、模型变色/缩放、新行动延迟，也没有向 BattleManager 增加表现分支。

原 Burn 资源未变：0 MP，在受影响者自己的两次行动结束时各造成固定 10 伤害，
重复施加刷新而不叠层。GUID 仍为 `0eb4ed56dccf4b3ba39db54d1605da10`，Boss 轮转不变。

### 事件与资源身份

- `DebuffState.SourceSkill` 在原有效果、名称、强度和持续次数快照之外保存来源技能引用；
  `Clear()` 清除该引用。
- `BaseController.TickDebuffAtTurnEnd()` 在推进状态之前取出引用，发生正伤害后发出
  `OnDebuffTicked(sourceSkill, actualDamage)`。实际伤害采用前后 HP 差值，致死时不会超过剩余 HP。
  即使到期或死亡清除状态，最后一次扣血事件仍保留正确来源。
- `BattleBurnFeedback` 监听扣血事件，`BattleDebuffHud` 监听 `OnDebuffChanged`。
  两者按配置的原 Burn 资源身份匹配，不按显示名匹配，也不将所有 DOT 当成火焰。
  其他 DOT 仍正常扣血，但不会自动套用这套表现。
- 两个监听组件都不控制伤害、持续次数、行动时机或消耗；禁用反馈不会禁用 DOT。

### Prefab 接线与生命周期

`Player.prefab` 和 `Enemy.prefab` 均添加 `BattleBurnFeedback`，引用各自 Controller、
原 Burn 技能和已有中性 `BattleFireParticles` 材质。默认持续 0.45 秒、18 颗火星，
粒子上限为 48。拉伸粒子在逻辑碰撞体前方上升淡出，不添加玩法碰撞体、阴影或材质实例。

特效重复触发时重新播放，不累积根节点。使用缩放时间，暂停时粒子模拟和清理一起暂停。
致死扣血仍可在原死亡停留期间显示；正常结束、禁用和销毁都会清理自身粒子根节点。

`Playerhud.prefab` 和 `EnemyHP.prefab` 添加 `BattleDebuffHud`，分别由 `PlayerHudItem`
和 `HpBar` 引用，沿用原绑定/解绑流程传入当前角色。组件按需生成不拦截射线的 `BurnStatus`
图片和 `Turns` 数字，不妨碍点击模型或 HUD 选人。玩家图标为 36 个 Canvas 单位，
锚定在 HP/MP 行下方；敌方图标为 28 单位，位于血条下方。未修改 Canvas 缩放或场景 UI 层级。

数字表示剩余 DOT 扣血次数，不是全场战斗轮数。重新绑定会先解除旧角色订阅；
禁用时隐藏并解绑，启用时读取当前状态。到期、死亡、复活、初始化和战斗结束沿用原状态清理流程隐藏火焰。

### 火焰资源

透明位图通过内置图像生成工具生成，没有使用备用 CLI，最终复制到
`Assets/Art/UI/BurnIcon.png`。其 `.meta` 设置为单张 Sprite、最大尺寸 64、关闭 Mipmap、
保留 Alpha，GUID 为 `8ac78e05021a454da76a2ebcecf28e64`。生成图的原始 Alpha 未改动。

上方 `Final generation prompt` 代码块完整保留实际发送的英文提示词：要求一个真正透明背景、
缩小后仍清晰的火焰状态图标，橙红外焰、金黄内焰、浅黄中心和深灰轮廓，无文字、边框或背景。

### 验证

Unity Play Mode 通过 43 项灼烧表现断言、重跑的 57 项 Boss 轮转断言和 20 项时间轴生命周期断言。
Boss 检查已将旧时间轴文字预期改为 HUD 火焰。覆盖真实 Prefab/技能、实际完成行动、
不同角色独立状态、最后一次/致死扣血、暂停、刷新、UI 重绑/禁用、资源身份匹配、
死亡停留、逃跑、胜利、危险预警及已销毁图标/动画的生命周期清理。

已检查 1280x720 和 800x600 的实际战斗/HUD 渲染；检查透明边缘、火焰像素、
状态位于 Canvas 内，以及存活且无碰撞体的火星粒子。最终表现检查和时间轴检查没有报告
Unity 异常或已销毁目标的动画警告。测试没有修改源场景、Burn 数据或真实存档。

临时脚本、报告和截图归档到被忽略的 `Library/BurnPresentationChecks`、
`Library/BossRotationChecks`、`Library/TimelineLifecycleChecks`，不作为游戏 Assets 提交。
另行检查脚本编译。独立 Player 构建和用户视觉确认仍待验收，本轮检查不等于完整 Demo 通关验收。
