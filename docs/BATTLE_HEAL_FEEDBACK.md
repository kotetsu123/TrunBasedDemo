# Healing Feedback / 治疗反馈

## English

### Scope and Trigger

Successful HP healing now produces a soft green aura and small four-point green
stars that appear in succession, rise, and fade. This is separate particle
rendering, not model flashing, recoloring, movement, or scaling. No sound or
dynamic light is added.

`BaseController.Heal` publishes `OnHealed(actualHeal)` only when HP actually
increases. The two added lines are the event declaration and its invocation;
healing amounts, HP notifications, floating numbers, skill costs, and item
consumption retain their original paths. `BattleHealFeedback` listens on the
battle Player and Enemy prefabs. Other BaseController instances do not gain
particles unless the component is attached and configured.

Single-target Heal, SelfHeal, PartyHeal, enemy healing, and battle HP potions
therefore share one effect. Group healing plays independently on each recovered
character, with the original single skill cost. Full HP, zero/negative healing,
dead targets, MP restoration, and revival do not trigger these stars. Exploration
HUD healing is not changed.

### Assets and Configuration

Both prefabs reference the same `BattleHealStar.mat` and its `Battle/Heal Star`
shader. The shader draws a camera-facing four-point sparkle from particle UVs
and vertex color, with transparent additive blending; no bitmap or runtime
texture generation is needed. The aura reuses the existing neutral
`BattleFireParticles.mat` and soft texture, colored green by particle data. That
shared material and the Fire/FireBall component are not modified.

Defaults are a 0.95-second tail, eight stars staggered across at most 0.25 seconds,
and 0.24-world-unit star size. Positions are derived from the character's logical
collider bounds and the current camera so stars spawn in front of the model.
Stars rise at 0.35 to 0.7 world units per second, growing in and fading out. Both
particle systems have emission disabled except for explicit emits, do not loop,
and have a 24-particle limit each. They have no gameplay colliders or shadows.

### Lifetime and Integration

The component owns its coroutine and runtime particle root. Healing is applied
at its original moment; battle actions and cameras do not wait for the cosmetic
tail. No BattleManager branch or skill enum is added.

Repeated healing restarts one effect instead of accumulating roots, without
combining or suppressing the underlying healing events. Particles and lifetime
use scaled time and pause together. Completion, death, component disable,
destruction, and scene unload clean up owned particles. Disable also unsubscribes
the event, and reenable binds exactly once. Missing materials cannot block HP
recovery. Source model materials, transforms, colliders, scenes, and real saves
are not edited by this effect.

### Verification (2026-11-05)

31 focused Unity Play Mode assertions passed with the real Player/Enemy prefabs,
Heal, SelfHeal, PartyHeal, LesserHeal, and battle potion paths. They covered
actual/clamped event values, unchanged healing/cost/consumption, simultaneous
group targets, duplicate/dead/full targets, excluded MP/revival/damage events,
repeat cleanup, pause/resume, subscriptions, disabled/missing visuals, actor
destruction/death, and final cleanup with native timeline LateUpdate enabled.

The star shader was rendered in isolation and checked for lit horizontal/vertical
arms and dark diagonal corners. The actual battle camera showed 1,578 visible
green effect pixels against an otherwise identical render with the effect
hidden. Both 1280x720 battle and star-shape captures were inspected. That run had
no Unity errors, shader failures, or destroyed-target tween warnings. Temporary
checks and images are archived in ignored `Library/HealFeedbackChecks`, not
shipped in Assets. Standalone-build acceptance and user approval of the effect's
appearance remain outstanding.

The existing 28 player FireBall and 32 enemy Fire assertions were also rerun and
passed, totaling 91 checks for this change. Those suites retained the original
damage, MP, hit/death cleanup, and result-menu behavior. The earlier standalone
boss-rotation and timeline-lifecycle suites were not rerun for this effect.

## 中文

### 范围与触发

成功回复 HP 时，现在会出现柔和绿光，以及依次闪现、向上飘散并淡出的小型绿色十字星。
这是独立的粒子渲染，不是模型闪白、变色、位移或缩放。本次不加入音效或动态灯光。

`BaseController.Heal` 只在 HP 实际增加时发布 `OnHealed(actualHeal)`。基类仅新增事件
声明和触发两行，治疗量、HP 通知、飘字、技能费用和道具消耗继续走原有路径。
`BattleHealFeedback` 在战斗 Player 和 Enemy Prefab 上监听该事件；其他 BaseController
实例只有挂上并配置该组件后，才会出现粒子。

因此单体 Heal、SelfHeal、PartyHeal、敌方治疗和战斗回血药共用一套效果。
群体治疗会在每个实际回血的角色身上分别播放，技能仍只消耗原本的一次费用。
满血、零或负数治疗、死亡目标、回复 MP 和复活都不触发这一版十字星；探索 HUD 的
治疗表现不变。

### 资源与配置

双方 Prefab 引用同一个 `BattleHealStar.mat` 和 `Battle/Heal Star` 着色器。
着色器根据粒子 UV 和顶点颜色绘制面向镜头的四尖十字星，使用透明叠加混合，不需要
位图或运行时生成贴图。绿光复用现有中性的 `BattleFireParticles.mat` 和柔边贴图，
由粒子数据染成绿色，不修改该共享材质或 Fire/FireBall 组件。

默认收尾 0.95 秒，8 颗星在最多 0.25 秒内分批出现，星星大小为 0.24 个世界单位。
位置根据角色逻辑碰撞体包围盒和当前镜头计算，让星星生成在模型前方。
星星以每秒 0.35 到 0.7 个世界单位向上移动，逐渐放大显现并淡出。
两个粒子系统除显式发射外均关闭自动发射，不循环，每个系统最多 24 个粒子；
不带玩法碰撞体，也不投射阴影。

### 生命周期与接入

组件自行管理协程和运行时粒子根节点。治疗仍在原来的时机生效，战斗行动和镜头不等待
这段纯表现收尾，没有向 BattleManager 添加分支或新增技能枚举。

连续治疗会重新播放同一个效果，不积累根节点，也不合并或吞掉底层治疗事件。
粒子和生命周期使用游戏时间，暂停时一起停止。正常结束、死亡、组件关闭、销毁和
场景卸载都会清理所属粒子。关闭时同时取消事件订阅，重新启用时只绑定一次。
材质缺失不会阻止 HP 回复；不会修改源模型材质、Transform、碰撞体、场景或真实存档。

### 验证（2026-11-05）

使用真实 Player/Enemy Prefab、Heal、SelfHeal、PartyHeal、LesserHeal 和战斗药水路径的
31 项专项 Unity Play Mode 断言通过。覆盖实际及封顶后的事件数值、治疗量/费用/消耗
不变、多个群疗目标、重复/死亡/满血目标、不触发效果的回蓝/复活/伤害事件、连续播放
清理、暂停/恢复、事件订阅、禁用或缺少显示资源、角色销毁/死亡和最终清理。
原生时间轴 LateUpdate 保持启用。

单独渲染了十字星着色器，并检查横竖四个尖角发亮、对角区域保持黑色。
实际战斗镜头与同帧隐藏特效的画面对比，检测到 1,578 个可见绿色效果像素，已检查
1280x720 战斗截图和十字星形状截图。该次没有 Unity 错误、着色器失败或已销毁目标的
动画警告。临时检查与图片归档忽略的 `Library/HealFeedbackChecks`，不留在 Assets
随游戏提交；独立构建和用户对外观效果的确认仍待验收。

另外重跑的 28 项玩家 FireBall 和 32 项敌方 Fire 检查也通过，本次共 91 项，保留原有
伤害、MP、命中/死亡清理和结算菜单行为。此前独立的 Boss 轮转与时间轴生命周期检查
没有为本次治疗效果重跑。
