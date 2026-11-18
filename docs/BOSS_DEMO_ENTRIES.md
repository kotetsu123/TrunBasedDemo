# Boss Demo Entries / Boss 双演示入口

## English

### Scope (2026-11-18)

`Boss_FieldScene` now has three vertically arranged debug controls: the existing
Argo recruit button, `Battle Demo`, and `Story Demo`. The title scene is unchanged.
The new controls are scene objects under the existing screen-space Canvas, not
runtime-generated UI or a second copy of the boss scene.

`BossDemoDebugController` is attached beside `PartyRecruitDebugTester`. Its scene
references select the existing boss encounter, player, transition, ending dialogue,
VN panel and Demo End controller.

### Battle Demo

- Uses the current party and inventory. Recruit Argo with the original button when
  a two-person showcase is wanted; the entry does not silently recruit, heal, or
  overwrite character templates.
- Starts the existing boss encounter in `BattleScene`, with normal boss AI,
  effects, victory/defeat rules, rewards and settlement. No forced result or
  numerical balance changes are introduced.
- Records the current Boss Field position for the existing return flow. The debug
  encounter intentionally has no field spawn IDs: a demo victory does not clear
  `boss_spawn_001` or unlock its automatic story event.
- Victory and escape follow the existing return flow. Defeat retains the normal
  retry/title/load choices. Retry keeps the selected encounter via the existing
  battle context.

### Story Demo

- Directly previews the existing ending VN, then calls `DemoEndController.ShowDemoEnd`.
- Does not require a boss victory or modify HP/MP, inventory, cleared-spawn IDs or
  automatic-event completion flags. The preview can be repeated.
- Pauses the field before opening the VN. Opening is deferred one frame so the
  button click cannot also advance the first dialogue line.
- Closing the Demo End panel restores the field controls; its existing return-to-title
  button is retained.

The debug group is hidden and noninteractive while the field is paused, during
scene transition, and while the story/ending is on screen. This includes existing
tutorials and system menus. Skip or complete an initial tutorial before using the
debug entries.

### Deliberately Deferred

These are two showcase entry points, not two implemented playthroughs. The future
first-playthrough ending is an authored forced retreat, not ordinary party KO.
The battle-time story takeover condition, forced-retreat sequence, final dialogue
and second-playthrough design are not implemented here. The pre-existing automatic
boss-clear ending and ordinary physical encounter triggers are unchanged.

### Validation

48 focused Unity Play Mode assertions passed on 2026-11-18. They cover scene
wiring, the original Argo recruit action, the new Paul portrait, pause and
double-start guards, VN completion and replay, missing references, cancelling a
pending preview, the actual Boss battle entry, normal victory/reward settlement,
return position, unchanged real-boss/story flags, and return to title. Captures at
1280x720 and 800x600 were inspected. No runtime errors or exceptions were observed.
The victory check uses a scripted lethal hit to exercise settlement, not a claim
that the unmodified fight has been strategically balanced or manually completed.
Temporary checks and captures are archived in ignored `Library/BossDemoChecks`.

## 中文

### 本次范围（2026-11-18）

在 `Boss_FieldScene` 原 Argo 入队调试按钮下方，增加 `Battle Demo`（战斗演示）
和 `Story Demo`（剧情演示），三颗按钮竖排在原 Canvas 内。没有修改标题界面，
没有复制 Boss 场景，也不是运行时临时生成 UI。

`BossDemoDebugController` 与原入队测试脚本放在同一物体上，通过 Inspector 引用
现有 Boss 遭遇、玩家、转场、结尾对话、VN 面板和 Demo 结束控制器。

### 战斗演示

- 使用当前队伍与背包。需要两人展示时，先点原入队按钮加入 Argo；不会偷偷入队、
  回满状态或修改角色模板。
- 直接进入现有 Boss 战，沿用 AI、技能、特效、普通胜负、奖励和结算，不强制胜利
  或失败，也不调整 Boss 数值。
- 保存当前地点，供现有结算流程返回 Boss Field；但不传入真实出生点 ID，因此
  演示胜利不会清除 `boss_spawn_001`，也不会因此解锁自动结尾剧情。
- 胜利、逃跑走原返回流程；失败仍有重试、标题和读取选项。重试沿用现有遭遇上下文。

### 剧情演示

- 直接预览现有结尾 VN，结束后打开 Demo End 面板。
- 不要求先击败 Boss，不修改角色 HP/MP、背包、Boss 清除记录或自动剧情完成标记，
  可以反复预览。
- 立即暂停探索，延后一帧打开 VN，避免打开按钮的点击同时跳过第一句。
- 关闭 Demo End 后恢复探索，也保留原来的返回标题功能。

探索暂停、转场、VN 和结束面板期间，整组调试按钮隐藏且不接收点击。原有教程和
系统菜单打开时也遵循这个规则；初次进场若出现教程，需要先完成或跳过。

### 暂未实现

这两个按钮只是 Demo 展示入口，不是已经实现了两个周目。一周目的设定是剧情杀后
被迫撤离，不等于普通全员阵亡。本次没有接战斗内剧情接管条件、强制撤离演出、
最终台词和二周目逻辑。原有的 Boss 清除后自动剧情及地图接触遇敌逻辑保持不变。

### 验证记录

2026-11-18 通过 48 项 Unity Play Mode 针对性断言，覆盖场景绑定、原 Argo 入队、
Paul 新头像、暂停与防重复点击、VN 完成与重复预览、缺失引用保护、待打开剧情的
取消、真实 Boss 战入口、正常胜利奖励结算、返回位置、Boss/剧情标记不变及回标题。
已检查 1280x720 和 800x600 截图，未观察到运行错误或异常。

胜利检查用测试脚本造成致死伤害来驱动结算，不表示本次已经手动打赢原数值 Boss，
也不声称完成了战斗平衡验收。临时检查和截图归档于忽略的 `Library/BossDemoChecks`，
不作为正式游戏资源上传。
