# Volcano Route Playtest Checklist / 火山路线测试清单

## Purpose / 目的

This checklist is for validating the first playable volcano blockout route before adding more systems or visual polish.

这份清单用于在继续添加系统或美术 polish 之前，验证火山灰盒路线是否已经可以作为第一版可玩流程跑通。

Test goal / 测试目标:

```text
Start
 -> Chest
 -> Slime Encounter
 -> Recruit Argo
 -> Group Encounter
 -> Boss Encounter
 -> Ending
```

## Test Build Notes / 测试版本说明

- Scene: `FildScene`
- Data: `FieldData_Test`
- Main focus: route flow, trigger correctness, runtime state writeback, and Save / Load safety.
- This checklist can be updated after each playtest pass with results and bugs.

- 场景：`FildScene`
- 数据：`FieldData_Test`
- 主要关注：路线流程、触发器正确性、运行时状态回写、Save / Load 安全性。
- 每次 playtest 之后，可以把通过结果、发现的问题和总结补进这份文档。



<!-- pagebreak -->

## 1. New Game / Field Start / 新游戏与 Field 起点

- [√] Player spawns at the volcano route start point. / 玩家出生在火山路线起点。
- [√] Camera starts in a usable angle for the route. / 相机初始角度适合观察路线。
- [√] Player can move without getting stuck on the first platform. / 玩家在起始平台移动时不会卡住。
- [√] ESC menu opens and closes correctly. / ESC 菜单可以正常打开和关闭。
- [√] Save button shows a Field Toast result. / Save 按钮会显示 Field Toast 结果提示。
- [√] Load button state matches whether a save file exists. / Load 按钮状态和是否存在存档文件一致。
- [√] Tutorial does not unexpectedly pause the Field when returning from Battle. / 从 Battle 返回 Field 时，Tutorial 不会意外让 Field 进入暂停状态。

Notes / 备注:

```text

```





<!-- pagebreak -->

## 2. Chest Flow / 宝箱流程

- [√] First chest is visible in the expected route position. / 第一个宝箱出现在预期路线位置。
- [√] Player cannot walk through the closed chest if collision is enabled. / 如果开启碰撞，玩家不能穿过关闭状态的宝箱。
- [√] E prompt appears when the player approaches the chest. / 玩家靠近宝箱时出现 E 互动提示。
- [√] Pressing E opens the chest. / 按 E 可以打开宝箱。
- [√] Chest reward is added to `InventoryRuntimeState`. / 宝箱奖励会加入 `InventoryRuntimeState`。
- [√] Field Toast displays the obtained item message. / Field Toast 会显示获得道具提示。
- [√] Opened chest visual/state remains correct after leaving and re-entering the area. / 离开并重新进入区域后，已打开宝箱的视觉和状态保持正确。
- [√] Save / Load preserves opened chest state. / Save / Load 后仍然保存宝箱已打开状态。

Notes / 备注:

```text

```




<!-- pagebreak -->

## 2.5 Inventory Flow / 背包流程

- [ ] Pressing B opens and closes the Field inventory panel. / 按 B 可以打开和关闭 Field 背包面板。
- [ ] Pressing ESC closes the inventory panel when it is open. / 背包打开时按 ESC 可以关闭背包。
- [ ] Inventory slots are generated in the expected order. / 背包格子会按预期顺序生成。
- [ ] Empty inventory slots hide icon and count text. / 空背包格会隐藏 icon 和数量文本。
- [ ] Clicking an item shows the description panel. / 点击道具后会显示说明面板。
- [ ] Use button appears only after a usable item is selected. / 选择可使用道具后才会显示 Use 按钮。
- [ ] Field item usage opens the party target panel. / 在 Field 使用道具时会打开队伍目标选择面板。
- [ ] HP recovery item restores the selected party member HP. / HP 恢复道具会恢复被选中队员的 HP。
- [ ] MP recovery item restores the selected party member MP. / MP 恢复道具会恢复被选中队员的 MP。
- [ ] Revive item can target a dead party member in Field. / 复活道具可以在 Field 中选择死亡队员。
- [ ] Item count decreases after successful use. / 道具成功使用后数量会减少。
- [ ] Dragging an item swaps or moves it to the expected slot. / 拖拽道具后会交换或移动到预期格子。
- [ ] Drag preview shows both item icon and count text. / 拖拽预览会同时显示道具 icon 和数量文本。
- [ ] Save / Load restores inventory item counts. / Save / Load 后会恢复背包道具数量。
- [ ] Save / Load restores inventory slot order. / Save / Load 后会恢复背包格子顺序。

Notes / 备注:

```text

``` -在打开背包的时候，按esc关闭背包的情况下，会直接弹出ESC菜单，而不是关闭背包。 这个可能需要修改一下逻辑，按esc关闭背包的时候，应该是先关闭背包，再按一次esc才会弹出菜单。
```





<!-- pagebreak -->

## 3. Slime Encounter / Slime 遭遇战

- [√] Slime field enemy spawns from `FieldData_Test`. / Slime 场景敌人由 `FieldData_Test` 正常生成。
- [√] Slime appears in the correct route position. / Slime 出现在正确路线位置。
- [√] Slime starts wandering correctly. / Slime 可以正常游荡。
- [√] Slime does not stay stuck against maze walls for too long. / Slime 不会长时间卡在迷宫墙上。
- [√] Slime can chase the player. / Slime 可以追踪玩家。
- [√] Collision with Slime starts the expected Battle encounter. / 与 Slime 碰撞后进入预期 Battle encounter。
- [√] Battle transition fade pauses Field movement. / 战斗转场 fade 期间 Field 移动会暂停。
- [√] Run / Escape returns the player to the Field position before battle. / Run / Escape 后玩家回到进入战斗前的 Field 位置。
- [√] Encounter cooldown prevents immediate re-entry into battle. / 遭遇冷却可以防止刚返回 Field 就立刻重新进战斗。
- [√] Winning the battle clears the correct `spawnId`. / 战斗胜利后会清除正确的 `spawnId`。
- [√] Cleared Slime does not respawn when configured as Permanent. / 如果配置为 Permanent，已清除的 Slime 不会重新生成。

Notes / 备注:
感觉追击有点太极端了。 现在的情况是，刚从场景转换回来，虽然怪物的位置会重新刷新回他们的生成点，但是如果人物距离生成点近的情况下， 人物回到场景当中需要时间但是怪物不需要。所以会在人物能操作之前就开始追击了，很容易造成追上在cooltime 当中无法触发战斗，但是怼着不让动、移动后又进入战斗的情况



<!-- pagebreak -->

## 4. Recruit Argo / Argo 入队

- [√] Argo RecruitPoint is generated from `FieldData_Test`. / Argo RecruitPoint 由 `FieldData_Test` 正常生成。
- [√] Argo visual appears in the expected route position. / Argo 视觉模型出现在预期路线位置。
- [√] Recruit interaction collider is active. / 入队交互 collider 处于启用状态。
- [√] E prompt appears near Argo. / 靠近 Argo 时出现 E 互动提示。
- [√] Pressing E plays pre-recruit dialogue if configured. / 如果配置了入队前对话，按 E 后会播放对话。
- [√] Dialogue completion recruits Argo. / 对话结束后 Argo 入队。
- [√] Argo is added to `PartyRuntimeState`. / Argo 被加入 `PartyRuntimeState`。
- [√] Field party HUD refreshes after recruitment. / 入队后 Field party HUD 会刷新。
- [√] Field Toast displays the party join message. / Field Toast 会显示入队提示。
- [√] Argo visual hides after recruitment when `disableAfterRecruit` is enabled. / 如果 `disableAfterRecruit` 启用，入队后 Argo 视觉模型会隐藏。
- [√] Save / Load preserves Argo recruitment state. / Save / Load 后仍然保存 Argo 入队状态。

Notes / 备注:

```text

```




<!-- pagebreak -->

## 5. Group Encounter / 联合遇敌

- [ ] Multiple field enemies spawn in the group encounter zone. / 联合遇敌区域内会生成多个场景敌人。
- [ ] Group enemies are close enough for group encounter detection. / 多个敌人的距离足够触发联合遇敌检测。
- [ ] Runtime group link line appears when enemies are chasing, if enabled. / 如果启用，敌人追踪时会显示运行时联合线条。
- [ ] Colliding with one enemy collects nearby group enemies. / 与其中一只敌人碰撞时，会收集附近的联合敌人。
- [ ] Battle starts with the expected combined enemy group. / Battle 中会生成预期的联合敌人组。
- [ ] Group encounter popup/message appears if configured. / 如果配置了提示，会显示联合遇敌 popup/message。
- [ ] Winning the battle clears all involved `spawnId` values. / 战斗胜利后会清除所有参与战斗的 `spawnId`。
- [ ] Cleared group enemies do not immediately respawn when configured as Permanent. / 如果配置为 Permanent，已清除的联合敌人不会立刻重新生成。

Notes / 备注:
联合战斗的部分，现在隔着墙也能进入战斗，我觉得可能需要修改一下索敌逻辑？还有联合逻辑，但其实也可以用就是。



<!-- pagebreak -->

## 6. Boss Encounter / Boss 遭遇战

- [ ] Boss field enemy spawns in the boss arena. / Boss 场景敌人生成在 Boss arena。
- [ ] Boss uses the expected `encounterId`. / Boss 使用预期的 `encounterId`。
- [ ] Boss encounter type is configured as Boss. / Boss encounter type 配置为 Boss。
- [ ] Battle starts successfully from the Boss field enemy. / 通过 Boss 场景敌人可以正常进入 Battle。
- [ ] Boss camera performance uses the Boss flow. / Boss camera 表现使用 Boss 流程。
- [ ] Reward / result UI appears after victory. / 胜利后会显示 reward / result UI。
- [ ] Returning to Field after Boss victory keeps Field state stable. / Boss 胜利后返回 Field，Field 状态保持稳定。

Notes / 备注:

```text

```




<!-- pagebreak -->

## 7. Ending Trigger / 结尾触发器

- [ ] Ending trigger exists after the Boss route. / Boss 路线之后存在 Ending trigger。
- [ ] Player can reach the ending trigger after clearing the route. / 玩家清理路线后可以到达 Ending trigger。
- [ ] Ending interaction / trigger condition works. / Ending 交互或触发条件可以正常工作。
- [ ] Ending dialogue or ending text displays if configured. / 如果配置了 Ending dialogue 或文字，会正常显示。
- [ ] Ending trigger does not fire before intended route completion. / Ending trigger 不会在预期路线完成前提前触发。

Notes / 备注:

```text

```




<!-- pagebreak -->

## 8. Save / Load Regression / Save 与 Load 回归测试

- [ ] Save records player position in the Field scene. / Save 会记录 Field 场景中的玩家位置。
- [ ] Load restores player position in the Field scene. / Load 会恢复 Field 场景中的玩家位置。
- [ ] Load restores inventory item counts. / Load 会恢复背包道具数量。
- [ ] Load restores party member HP / MP state. / Load 会恢复队伍成员 HP / MP 状态。
- [ ] Load preserves opened chest IDs. / Load 会保存已打开宝箱 ID。
- [ ] Load preserves recruited party members. / Load 会保存已入队角色。
- [ ] Load preserves cleared permanent enemy spawn IDs. / Load 会保存已清除的 Permanent 敌人 spawn ID。
- [ ] Load does not duplicate generated field objects. / Load 不会重复生成 Field 对象。
- [ ] Load does not leave Field in a hidden paused state. / Load 后 Field 不会停留在隐藏暂停状态。

Notes / 备注:

```text

```




<!-- pagebreak -->

## 9. Issues Found / 发现的问题

Use this section after testing.

测试后在这里记录发现的问题。

```text
- Field enemy chase after battle return still feels too aggressive.
  After returning from Battle, enemies respawn/reset at their spawn point faster than the player regains control.
  If the player returns near an enemy spawn point, the enemy can chase during cooldown, body-block the player,
  and then trigger another battle after cooldown ends.

- Field 敌人在战斗返回后的追击体验仍然偏激进。
  从 Battle 回到 Field 后，敌人会比玩家更早从生成点开始行动。
  如果玩家返回位置离敌人生成点太近，敌人会在 cooldown 期间贴住玩家，造成卡位，
  cooldown 结束后又容易立刻重新进入战斗。
```

<!-- pagebreak -->

## 10. Pass Summary / 测试总结

Use this section after testing.

测试后在这里记录本次结果。

```text
Date / 日期: 2026-08-25
Tester / 测试者: Kotetsu
Result / 结果: Partial pass
Summary / 总结:
- Chest Flow passed. Chest visibility, collision, E prompt, reward writeback, Field Toast, opened visual state, and Save / Load chest state were verified.
- Slime Encounter mostly passed. FieldData spawn, position, wander, chase, battle entry, Run / Escape return, encounter cooldown, spawnId clear, and Permanent no-respawn were verified.
- Remaining issue: enemy chase after battle return/cooldown can feel too aggressive and may body-block the player near spawn points.
- Recruit Argo, Group Encounter, Boss Encounter, Ending Trigger, and full Save / Load regression remain untested.

- 宝箱流程已通过。已验证宝箱显示、碰撞、E 提示、奖励写入、Field Toast、开启后视觉状态，以及 Save / Load 后宝箱状态。
- Slime 遭遇战大部分通过。已验证 FieldData 生成、位置、游荡、追击、进入战斗、Run / Escape 返回、遭遇冷却、spawnId 清除，以及 Permanent 不刷新。
- 剩余问题：战斗返回/cooldown 后敌人追击仍然偏激进，在靠近生成点时可能会贴住玩家造成卡位。
- Argo 入队、联合遇敌、Boss 遭遇战、Ending Trigger，以及完整 Save / Load 回归测试尚未测试。
```




