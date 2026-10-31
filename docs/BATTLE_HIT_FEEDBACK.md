# Battle Hit Feedback / 战斗受击反馈

## English

### Scope

The current battle Player and Enemy prefabs now recoil briefly when they take
actual HP damage, then return to their original visual position. This is cosmetic
recoil, not gameplay knockback. White flashes, material changes, screen shake,
attack effects and sound are intentionally not included.

### Implementation

- `BaseController.TakeDamage()` publishes `OnDamageTaken(actualDamage)` once when
  HP actually decreases. It uses the applied amount, including a lethal hit capped
  by remaining HP. Zero damage, healing and MP restoration do not publish it.
- `BattleHitFeedback` listens to that event on its own controller and uses the
  project's existing DOTween library to punch the visual child's local position.
- The Player and Enemy prefabs have a collider-free `VisualRoot`. Their original
  MeshFilter and MeshRenderer moved to this child; mesh and material references
  remain unchanged. Colliders, controller scripts, floating-text anchors and
  camera anchors stay on the logical hierarchy outside the animated child.
- Only battle prefabs opt into the effect. Field player/enemy prefabs and
  `BattleManager` are unchanged.

### Configuration and Lifetime

`Controller` references the local character controller; `Visual Root` references
its display-only child. Binding to the actor root, an unrelated transform, or a
child containing a collider is rejected.

Default duration is 0.25 seconds, vibrato 4, elasticity 0.6. `Local Punch` is a
local-space displacement: Player uses Z -0.16 and Enemy uses Z +0.16 for the
current battle layout. It is not calculated from an attacker's position.

The effect records the child's rest position when enabled. Repeated hits kill
only its own previous tween, restore rest, then start again, preventing drift.
Disabling or destroying the component restores rest and releases its animation;
the damage subscription is removed on disable. The tween is linked to the visual
object's lifetime and uses scaled time. It adds no action lock or extra battle wait.

All existing damage paths using `TakeDamage` share the feedback, including Burn
ticks and individual targets of an AOE. For a future model, keep the stable
logical root and put only the model beneath `VisualRoot`; keep colliders and
camera/floating-text anchors outside that child.

### Verification

45 Unity Play Mode assertions passed on 2026-10-30, covering both prefab imports,
visual movement, unchanged logical/collider/anchor positions, exact restoration,
unchanged materials, repeated hits, disable/re-enable, custom rest positions,
event unsubscription, Burn, charged multi-target AOE, destruction and death.
Temporary checks and captures live in ignored `Library/HitFeedbackChecks`.
The existing 57-assertion boss rotation regression also passed again after the
prefab changes, retaining phase-one selection, Burn timing, 80/120 AOE damage and
the four-action second-phase cycle.
No real save or scene was modified. This is not a standalone-build acceptance test.

## 中文

### 范围

当前战斗用的 Player、Enemy Prefab 在实际损失 HP 时，会短暂后晃再回到原来的显示位置。
这是视觉反馈，不是实际击退。本轮明确不加入闪白、材质变色、屏幕震动、攻击特效或音效。

### 实现

- `BaseController.TakeDamage()` 仅在 HP 实际减少时，发送一次
  `OnDamageTaken(actualDamage)`。数值是实际扣血量，致死时不会超过原剩余 HP；
  0 伤害、治疗和 MP 恢复不发送受击通知。
- `BattleHitFeedback` 监听自身角色的该事件，使用项目已有的 DOTween 对显示子节点的
  本地坐标播放短暂往返晃动。
- Player、Enemy Prefab 新增不含碰撞体的 `VisualRoot`。原 MeshFilter、MeshRenderer
  移到该子节点，保留原网格和材质引用。碰撞体、角色控制脚本、飘字与摄像机锚点，
  仍留在动画子节点之外的逻辑层级。
- 只有战斗 Prefab 接入效果，没有修改探索用的玩家/敌人 Prefab，也没有修改 `BattleManager`。

### 配置与生命周期

`Controller` 引用当前角色 Controller；`Visual Root` 引用纯显示子节点。
若绑定角色根节点、无关物体或包含碰撞体的子节点，会拒绝播放并提示配置问题。

默认持续 0.25 秒，vibrato 为 4，elasticity 为 0.6。
`Local Punch` 是本地坐标位移：按当前战斗站位，玩家为 Z -0.16，敌人为 Z +0.16；
它不是根据攻击者位置计算的真实击退方向。

组件启用时记录显示节点的原位置。连续受击只结束自身上一次动画，先归位再重播，
避免累积偏移。组件关闭或销毁时归位并清理动画，关闭时解除伤害事件订阅。
动画绑定显示物体的生命周期，遵循游戏时间缩放，不增加行动锁或额外战斗等待。

所有沿用 `TakeDamage` 的伤害都会触发，包括 Burn 扣血和群攻中的每个受击目标。
以后替换模型时，可保留稳定的逻辑根节点，只将模型放在 `VisualRoot` 下；
碰撞体、摄像机与飘字锚点不要放进这个动画子节点。

### 验证

2026-10-30 的 45 个 Unity Play Mode 断言通过，覆盖双方 Prefab 导入、显示节点移动、
逻辑根节点/碰撞体/锚点不动、精确归位、材质不变、连续受击、关闭/重启、自定义原位置、
事件解除、Burn、蓄力多人群攻、组件销毁和阵亡清理。
临时检查和截图保存在忽略的 `Library/HitFeedbackChecks`，未修改真实存档或场景。
Prefab 修改后另行重跑的 57 项 Boss 轮转回归也通过，保留一阶段选择、Burn 时机、
普通/蓄力群攻的 80/120 伤害和二阶段四步循环。
尚未进行独立构建验收。
