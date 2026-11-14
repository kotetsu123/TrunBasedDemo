# Argo Battle Model / Argo 战斗模型

## English

This page records the model-only checkpoint `beb538b6`. The subsequent blink
approach/strike/return implementation is documented in [BATTLE_MELEE_FEEDBACK.md](BATTLE_MELEE_FEEDBACK.md).

### Model-Only Checkpoint

This checkpoint replaces only `Argo_002` in battle with the Lycaon model and
looping Idle animation. Paul, enemies, Field models, portraits, character stats,
skills, save data and battle UI remain unchanged. Attack-animation work is
deliberately excluded from this commit.

`BattleScene` has one `BattleSpawner.playerPrefabOverrides` entry mapping
`Argo_002` to `Assets/Prefabs/Argo_Lycaon.prefab`. Lookup uses character ID, not
party slot or display name. Other characters keep their existing prefab. Remove
this entry to return Argo to the cube without changing character data.

The Argo prefab is a variant of Player. Its collider-free
`VisualRoot/AttackRoot` contains the skinned model instead of the cube renderer.
The model is approximately 1.85 units tall, with its bottom aligned to local
Y = -0.5. Only Argo's click collider, floating-text anchor and turn-camera anchor
are adjusted for the taller display.

### Included Assets and Deferred Attacks

The model source is `E:/BaiduNetdiskDownload/041_lai_ka_en`. This checkpoint includes
one Idle FBX, four diffuse maps, four built-in Standard materials, and an Animator
controller containing only Idle. The FBX supplies its mesh, Generic avatar and
embedded animation. Original download files are not modified. The excluded
housing and `kc2` directories are not used.

There is no `BattleModelAnimation` component or dependency on the Attack/Run
clips. `BattleAttackFeedback` is unchanged from the previous commit; its original
feedback is retained temporarily. FireBall, healing, Burn and recoil are inherited
from Player. The source game's original toon shader is not reproduced here.

The intended future melee flow is: move to the selected enemy, perform the hit,
then return to the original formation position. Running in place and a kick from
the original standing position are not the approved final choreography. Cameras
and animation root displacement must support the intended movement, rather than
replace it with a stationary action.

The previous attack draft, full prefab/controller snapshots, clips and script
are backed up under ignored
`Library/LycaonIntegration/AttackWip/2026-11-11-model-only`. This is a local draft,
not an upload requirement or a permanent backup; deleting Library also removes
it. The source download files remain available separately.

### Validation and Distribution

Ten focused Unity Play Mode assertions passed on 2026-11-11: Argo-only replacement,
Paul unchanged, no missing/deferred component, Idle-only controller, animated
bones, supported textured materials, unchanged stats/skills, one original attack
damage application, recovery, and no captured Unity errors. Edit-time dependency
inspection also confirmed that the prefab does not require the deferred attack
script or clips. No standalone build or full-demo playthrough is claimed.

No license document was present in the supplied model folder. Confirm permission
for public distribution before publishing the model, animation or textures.

## 中文翻译

本页记录仅模型提交 `beb538b6` 的状态。后续瞬移接近、攻击、瞬移返回的实现见
[BATTLE_MELEE_FEEDBACK.md](BATTLE_MELEE_FEEDBACK.md)。

### 仅模型的提交节点

这个提交节点只将战斗中的 `Argo_002` 替换为莱卡恩模型，并播放循环待机动画。
Paul、敌人、探索模型、头像、角色属性、技能、存档数据和战斗 UI 均保持不变。
攻击动画相关工作明确不包含在本次提交中。

`BattleScene` 的 `BattleSpawner.playerPrefabOverrides` 中配置了一条映射：
`Argo_002` 对应 `Assets/Prefabs/Argo_Lycaon.prefab`。查找依据是角色 ID，而不是
队伍位置或显示名称。其他角色保持原 Prefab。删除这条映射即可让 Argo 恢复方块，
不需要修改角色数据。

Argo Prefab 是 Player 的变体。无碰撞体的 `VisualRoot/AttackRoot` 下使用蒙皮模型
代替方块 Renderer。模型高度约为 1.85 个单位，底部对齐局部 Y = -0.5。
只有 Argo 的点击碰撞体、飘字锚点和回合镜头锚点针对较高的模型进行了调整。

### 包含的资源与延后的攻击

源目录为 `E:/BaiduNetdiskDownload/041_lai_ka_en`。本次包含一个 Idle FBX、四张
漫反射贴图、四个内置 Standard 材质，以及只有 Idle 状态的 Animator 控制器。
FBX 提供网格、Generic Avatar 和内嵌动画。下载目录中的原文件没有修改，也没有使用
排除的住房或 `kc2` 目录。

当前 Prefab 不包含 `BattleModelAnimation` 组件，也不依赖 Attack/Run 动画。
`BattleAttackFeedback` 与上一次提交保持一致，暂时沿用其原有反馈。FireBall、治疗、
灼烧和受击晃动从 Player 继承。本次没有还原源游戏原本的卡通 Shader。

后续近战的目标流程是：移动到选中的敌人面前，攻击命中，再返回原阵型位置。
原地跑动、站在原位置踢击，不是已认可的最终演出。镜头和动画自带位移应配合这个流程，
而不是把正常接近目标的动作替换为原地攻击。

之前的攻击草稿、完整 Prefab/控制器快照、动画和脚本备份在忽略的
`Library/LycaonIntegration/AttackWip/2026-11-11-model-only`。它是本地草稿，不是
上传所需内容，也不是永久备份；删除 Library 会一并删除它。原始下载文件另外保留。

### 验证与发布

2026-11-11 的 Unity Play Mode 针对性检查有 10 项断言通过：仅 Argo 替换模型、
Paul 不变、没有丢失或待定的组件、控制器只有 Idle、骨骼动画有效、带贴图的材质受支持、
属性和技能不变、原普攻伤害只结算一次、动作恢复，以及没有捕获到 Unity 错误。
编辑模式下的依赖检查也确认 Prefab 不需要延后的攻击脚本和动画。
本次不声称完成独立构建或整个 Demo 的完整游玩测试。

提供的模型目录中没有授权文件。公开发布模型、动画或贴图之前，需要确认相应分发许可。
