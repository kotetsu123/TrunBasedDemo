# Field Skill Page / 探索技能页

Updated / 更新：2026-11-10

## English

### Menu and Display

The portrait/Status system menu now has Status, Skill, Item, and Save / Load tabs.
The new Skill page is wired into both FildScene and Boss_FieldScene. It uses the
existing 960x600 canvas scaling policy without changing other panels' scalers.
`SkillPanel.prefab` and `SkillRow.prefab` are editable uGUI assets; rows reuse the
existing character portraits and `StatusMember` view. No runtime UI builder is
required by the game.

Status and Skill share `FieldCharacterStatsController`, so changing tabs keeps
the selected character. Clicking a party member, the arrow buttons, or A/D and
left/right arrows changes the caster outside target selection. The page lists
that runtime character's configured skills, omitting null and duplicate references.
It does not substitute the shared Player prefab's legacy test list when the
character has no assigned skills. Empty skills and empty parties have empty states.

Skill details show name, effect, MP cost, target scope, and availability/rejection
reason. Attack and Debuff rows are gray and read-only in exploration. They remain
selectable for inspection, but their Use button is disabled. No new SkillData
fields, enums, skill resources, costs, or character loadouts were introduced.

### Exploration Use

`FieldSkillService` is independent of battle controllers and BattleManager.
This first version permits Heal with Self, AllySingle, or AllyAll targets, and
Revive with AllyDeadSingle. Attack/Debuff and other combinations cannot be cast.
The caster must be the actual runtime party member, alive, and own the exact
skill resource; MP cost must be nonnegative, power positive, and sufficient MP
available. A copied character with the same ID is not a substitute party member.

Single-target Heal and Revive open a separate party target dialog. Selecting a
target does not change the caster; MP comes from the caster, HP goes to the target.
Only injured living allies can be healed, and only fallen allies can be revived.
Invalid targets are disabled. Conditions are rechecked at confirmation so a stale
target choice cannot bypass MP/life/party/ownership checks or spend resources.

SelfHeal applies directly to the caster. PartyHeal applies directly to all injured
living party members, including the caster, and charges MP once for the group.
It skips full-HP and fallen members. No valid effect means no MP charge. Healing
is capped at MaxHp; revival clears the down flag and clamps skill power to
1..MaxHp. A fallen caster cannot cast their own revival.

The service commits the entire cast's HP/MP state before sending existing MP and
HP notifications, and rejects reentrant casts during those notifications. The
panel refreshes the selected character, skill availability and target views;
Status and portrait HP/MP displays use their existing event/refresh paths. World
models, battle effects, inventory quantities, and source ScriptableObjects are
not modified. Runtime state is retained by the existing save system, not a new
skill-specific save format or skill-learning system.

### Pause, Navigation, and Lifetime

The existing system menu owns pause for all pages. Switching tabs does not
unpause. Escape/right-click first cancels a skill target choice, then closes the
menu on a later input. Closing a menu opened from ESC returns to that paused menu;
closing one opened from the portrait returns to exploration. Changing pages clears
pending skill targets without spending MP. The target overlay blocks underlying
controls; the system menu provides the single close button while the page is bound.

The Skill page subscribes to selection/resource changes only while shown.
Hiding, disabling, destruction, and scene unload remove those subscriptions and
hide target UI. Refresh after party reconstruction binds the new character by ID
rather than keeping a stale reference. Views are pooled within the page, not
recreated for every resource update. Existing Status, Item, and Save / Load page
enum values remain unchanged; Skill was appended to the enum.

### Verification (2026-11-10)

74 Unity Play Mode assertions passed in the installed 2022.3.62f2 editor's batch
mode. Checks use both authored field scenes, real prefabs and skills, uGUI button
events, runtime party copies and the native UI lifecycle. Coverage includes member
and skill switching, gray attack/Debuff inspection, invalid targets, full HP, MP,
KO, ownership, self/group/single healing, revival, notification order, reentrancy,
cancel/back/pause behavior, empty lists, rebind after load, disable/reenable,
Status/Item navigation, Save / Load confirmation gates and the tutorial gate.

The real save API was directed to an isolated Library directory for post-cast
HP/MP round trips; real player saves were not written. Actual scene-camera renders
at 1920x1080 and 800x600 were inspected, including the target dialog and gray Debuff
row. Pixel and TMP overflow checks passed. No Unity errors or destroyed-target
tween warnings were reported by the final run. Source character/skill resources,
Player/Enemy prefabs and BattleScene were unchanged.

Authoring/check helpers, reports, temporary saves and screenshots are archived
under ignored `Library/FieldSkill`, not shipped under Assets. Script compilation
was checked separately. No standalone player build or full demo playthrough was
performed; previous battle-specific suites were not rerun for this field-only change.

## 中文翻译

### 菜单与显示

头像/Status 系统菜单现在包含 Status、Skill、Item 和 Save / Load 页签。
新 Skill 页已接入 FildScene 与 Boss_FieldScene，沿用现有 960x600 Canvas 缩放规则，
没有修改其他面板的 CanvasScaler。`SkillPanel.prefab`、`SkillRow.prefab` 都是可编辑的
uGUI 资源，角色条目复用已有头像和 `StatusMember`。游戏运行不依赖 UI 生成脚本。

Status 与 Skill 共用 `FieldCharacterStatsController`，切换页签保留选中角色。
未处于选目标阶段时，点击队友、左右按钮，或使用 A/D、左右方向键可切换施法者。
技能列表读取该运行时角色配置，跳过空项和重复引用。角色没有配置技能时，不替换成
Player Prefab 上的旧测试列表。空技能和空队伍均有对应空状态。

详情显示技能名、效果、MP 消耗、目标范围和可用状态/禁用原因。
攻击与 Debuff 在探索中呈灰色，只能查看，使用按钮不可点击。
未新增 SkillData 字段、枚举、技能资源或消耗，也没有修改角色配招。

### 探索使用

`FieldSkillService` 不依赖战斗控制器或 BattleManager。
首版允许目标为 Self、AllySingle、AllyAll 的 Heal，以及目标为 AllyDeadSingle 的 Revive。
攻击、Debuff 和其他组合不可在探索施放。施法者必须是实际运行时队员、存活，
并拥有该技能资源；MP 消耗不能为负、效果强度必须为正，且 MP 足够。
即使 ID 相同，复制出来但不在队伍中的角色也不能代替真实队员。

单体 Heal 和 Revive 打开独立队友目标窗口。选目标不会切换施法者：
扣施法者 MP，改变受益者 HP。治疗只允许受伤的存活队友，复活只允许倒下队友，
无效目标置为不可点击。确认时再次检查 MP、存活、队伍身份和技能归属，
过期的选人状态不能绕过校验或消耗资源。

SelfHeal 直接作用于施法者；PartyHeal 直接治疗包括施法者在内的所有受伤存活队员，
全队只扣一次 MP。跳过满血和倒下队员；没有可生效的对象时不扣 MP。
治疗不超过 MaxHp，复活清除倒下标记，HP 将技能强度限制在 1..MaxHp。
倒下的施法者不能施放自己的复活技能。

服务先完成整次施法的 HP/MP 修改，再发送原有 MP/HP 通知，并在通知过程中拒绝重入施法。
面板刷新当前角色、技能可用性和目标列表；Status 与头像 HP/MP 显示沿用原事件/刷新路径。
不修改场景模型、战斗特效、背包数量或源 ScriptableObject。
运行时结果沿用现有存档系统保存，没有新增技能专用存档格式或技能学习系统。

### 暂停、导航与生命周期

所有页面的暂停由原系统菜单持有，切页不会解除暂停。
Escape/右键先取消技能选人，下一次输入再关闭菜单。
从 ESC 菜单进入时，关闭后返回仍暂停的 ESC 菜单；从头像进入时返回探索。
切页会取消待选技能目标，不扣 MP。目标遮罩阻止点击下层控件；
页面绑定系统菜单时，仅保留系统菜单的关闭按钮。

Skill 页只在显示期间订阅选择/资源变化，隐藏、禁用、销毁和场景卸载时解除订阅并关闭目标界面。
队伍重建后刷新会按 ID 绑定新角色，不继续持有旧引用。条目在页面内复用，
不是每次资源更新都销毁重建。Status、Item、Save / Load 的原枚举值保留，Skill 追加在最后。

### 验证（2026-11-10）

使用已安装的 Unity 2022.3.62f2 编辑器批处理运行，74 项 Play Mode 断言通过。
检查使用两张已接线的探索场景、真实 Prefab/技能、uGUI 按钮事件、运行时队伍副本和原生 UI 生命周期。
覆盖角色/技能切换、攻击与 Debuff 置灰查看、无效目标、满血、MP、KO、归属校验、
自疗/群疗/单体治疗、复活、通知顺序、重入、取消/返回/暂停、空列表、读档重绑、
禁用/重启、Status/Item 导航、Save / Load 确认保护及教程保护。

实际存档 API 被重定向到 Library 隔离目录，验证施法后 HP/MP 存读档往返，没有写真实玩家存档。
已检查 1920x1080、800x600 实际场景镜头渲染，包括选人窗口和置灰 Debuff 行，
像素与 TMP 文字溢出检查通过。最终运行没有报告 Unity 错误或销毁目标动画警告。
角色/技能源资源、Player/Enemy Prefab 和 BattleScene 均未改动。

制作/检查脚本、报告、临时存档与截图归档忽略的 `Library/FieldSkill`，不随 Assets 提交。
另行检查脚本编译；未运行独立 Player 构建或完整 Demo 通关，先前的战斗专项检查没有为本轮探索改动重跑。
