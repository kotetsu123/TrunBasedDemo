# Save / Load UI

Updated: 2026-10-22.

## English

- Both field scenes use the portrait system menu's Save / Load tab. The existing ESC Save and Load buttons open the same page in the corresponding mode. Closing returns to the original entry while preserving its pause behavior.
- Title Load opens the same list in load-only mode. The title's New Game and settings behavior is unchanged.
- There are five manual slots. Rows display the slot number, up to four party portraits and levels, area, and local save date/time. Thumbnails and slot deletion are not included.
- Empty slots cannot be loaded. Saving into an occupied or unreadable slot requires overwrite confirmation. Loading requires confirmation. ESC/right-click first cancels confirmation; another press closes the menu. The confirmation overlay blocks the page navigation.
- A successful UI load rebuilds the saved field scene, including same-scene loads. This lets existing field initialization reconstruct chests, recruits, enemies, and ending state rather than leaving old scene objects beside the restored runtime data.

### Storage and API

- Files live under `Application.persistentDataPath`: `save_01.json` through `save_05.json`. API indices are zero-based.
- If slot 1 has no numbered file, the old `save.json` is shown as a legacy save. Saving slot 1 creates the numbered file without deleting the legacy file.
- Saves are written to a sibling `.tmp` file, flushed, and then moved/replaced. A valid previous file is retained as `.bak`. When the main file is unreadable, a readable backup is listed with a Backup marker and used after load confirmation.
- `GetSlots()` and `GetSlotInfo(index)` read summaries without applying runtime state. `SaveSlot(index)` writes current progress. `LoadSlot(index, itemDatabase, characterDatabase, applyToCurrentScene)` validates the file and required IDs before applying it. Failures return false with `LastError`.
- The UI passes `applyToCurrentScene: false`, then loads the field scene so the saved transform remains available to `FieldCreator`. Compatibility methods `Save()` / `Load(...)` still target slot 1 for existing debug integrations.
- Existing schema version 1 remains readable. `savedAtUtc` is an optional new field; old summaries fall back to the file timestamp. Unsupported formats, malformed files, invalid positions, and missing required character/item references are rejected.
- Unity resource references are not serialized into save JSON. Portraits are resolved through the existing character database.

`SaveLoadPanelController` coordinates the page, modes, confirmations, and scene load. `SaveSlotView` renders one row. The authoring and Play Mode check helpers are temporary, not runtime dependencies. Editor checks redirect storage to an isolated directory under ignored `Library/SaveLoad`; they must not overwrite the user's real saves.

Verification: 40 Unity assertions passed for independent slots, legacy files, backup recovery, corrupt/missing-reference rejection without party mutation, restored HP/combat stats, menu pause/return, pointer blocking, both field scene bindings, and title-to-field loading. Rendered UI screenshots were inspected at 1920×1080 and 800×600. A standalone build and a complete game playthrough were not performed in this change.

## 中文

- 两个探索场景通过头像系统菜单的 Save / Load 页签进入。原 ESC 菜单的 Save、Load 按钮打开同一页面的对应模式。关闭后返回原入口，并保持该入口的暂停行为。
- 标题页的 Load 打开同一列表，但只提供读取。标题页的新游戏和设置行为不变。
- 初版为五个手动槽位。每行显示编号、最多四名队员的头像与等级、地点，以及本地日期和保存时间。不包含截图缩略图和删除槽位功能。
- 空槽位不能读取。覆盖已有或不可读的槽位需要确认，读取也需要确认。ESC／右键先取消确认，再次按下才关闭菜单。确认遮罩拦截页面导航的鼠标操作。
- UI 读取成功后会重建保存的探索场景，包括读取同一场景。这让已有初始化逻辑根据存档重建宝箱、入队点、敌人和结尾状态，避免仅恢复运行时数据后仍残留旧场景物件。

### 存储与接口

- 文件位于 `Application.persistentDataPath` 下，名称为 `save_01.json` 到 `save_05.json`。接口索引从零开始。
- 第一个槽位没有编号文件时，会把旧的 `save.json` 显示为旧版存档。保存第一个槽位会创建编号文件，不删除旧文件。
- 保存时先写同目录 `.tmp` 文件并刷新到磁盘，再移动或替换。上一个有效文件保留为 `.bak`。主文件不可读而备份可读时，列表标记 Backup，并在确认读取后使用备份。
- `GetSlots()`、`GetSlotInfo(index)` 只读取摘要，不应用运行时状态。`SaveSlot(index)` 保存当前进度。`LoadSlot(index, itemDatabase, characterDatabase, applyToCurrentScene)` 先检查文件及所需 ID，再应用数据。失败时返回 false，并通过 `LastError` 提供原因。
- UI 传入 `applyToCurrentScene: false` 后加载探索场景，让保存的位置仍能交给 `FieldCreator`。兼容接口 `Save()`／`Load(...)` 仍操作第一个槽位，供现有调试入口使用。
- 原 schema version 1 仍可读取。新增的 `savedAtUtc` 为可选字段，旧档摘要回退到文件时间。不支持的格式、损坏文件、非法位置以及缺失的角色／道具引用会被拒绝。
- 存档 JSON 不保存 Unity 资源引用。头像通过已有角色数据库解析。

`SaveLoadPanelController` 负责页面、模式、确认和场景读取，`SaveSlotView` 渲染单行。制作及 Play Mode 检查辅助脚本是临时工具，不是运行时依赖。编辑器检查将存档位置重定向到已忽略的 `Library/SaveLoad` 隔离目录，不能覆盖用户真实存档。

验证：40 项 Unity 断言通过，覆盖独立槽位、旧档、备份恢复、坏档／缺失引用拒绝且不修改队伍、HP 和战斗属性恢复、菜单暂停／返回、鼠标遮挡、两个探索场景绑定，以及标题读取进入探索。已检查 1920×1080 和 800×600 的 UI 渲染截图。本次未进行独立构建和完整通关测试。
