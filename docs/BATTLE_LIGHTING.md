# Battle Lighting / 战斗灯光

## English

### Purpose and Scope

The 2026-11-13 change improves visibility of the imported dark-clothed model in
BattleScene without replacing its materials or restoring its original toon shader.
The scene previously used one warm directional light and skybox-based ambient
lighting. A static front-view comparison showed that much of the shadow side was
crushed to black. No runtime script that progressively lowers battle lighting was
found; this is a scene-lighting adjustment, not a claim to fix cumulative dimming.

Only `Assets/Scenes/BattleScene.unity` and this document belong to the lighting
checkpoint. Pending melee code, animation clips, controllers, prefabs and melee
documents are separate work and should not be selected for this commit. The
lighting change has no dependency on the new melee implementation.

### Settings

Environment lighting uses Trilight instead of depending on the default skybox's
ambient probe. RGB values are:

| Setting | Value |
| --- | --- |
| Ambient sky | 0.46, 0.47, 0.48 |
| Ambient equator | 0.34, 0.35, 0.36 |
| Ambient ground | 0.22, 0.21, 0.20 |
| Key color | 1.00, 0.97, 0.92 |
| Key intensity | 1.00, unchanged |
| Key shadow strength | 0.80, previously 1.00 |
| Fill color | 0.90, 0.94, 1.00 |
| Fill intensity | 0.35 |
| Fill rotation | X 25, Y 150, Z 0 degrees |

`Battle Fill Light` is a realtime directional light with shadows disabled. It
provides a restrained opposite-side fill without adding a second shadow map. The
main light keeps its original rotation and soft shadows. Skybox material, fog,
reflection settings, camera poses, UI and project-wide quality settings are not
changed. The fill and ambient light also affect the battle floor; they are not
character-exclusive lighting. Retune these scene lights if the battle environment
or character materials change significantly.

### Verification

Nineteen targeted authoring checks passed. The same static Idle pose was rendered
before/after from front, back and the existing default battle shot at 1280x720.
No combat actions were executed and no new Play Mode melee test is claimed.
Model-silhouette luminance measurements in this Linear-color-space preview were:

| View | Before median, 0-255 | After median, 0-255 |
| --- | --- | --- |
| Front | 0.0 | 32.7 |
| Back | 46.8 | 69.4 |
| Default battle shot | 84.1 | 95.6 |

These numbers describe the captured static preview, not guaranteed runtime pixel
values. Clipped-bright silhouette pixels remained below 0.03% in those captures.
File hashes confirmed that FildScene, Boss_FieldScene, Argo_Lycaon.prefab, all four
Lycaon materials, GraphicsSettings and QualitySettings stayed unchanged.
The temporary model fixtures and camera poses were removed/restored before saving.
Screenshots, measurements and the one-off authoring script are archived under
ignored `Library/BattleLightingChecks`, not shipped as game assets.

## 中文翻译

### 目的与范围

2026-11-13 的调整旨在让 BattleScene 中导入的深色服装模型更容易看清，不替换角色材质，
也不尝试还原原游戏的卡通 Shader。场景之前使用一盏偏暖的方向光，以及基于天空盒的环境
光。静态正面视角的对比中，大部分背光细节压到了黑色。没有找到会持续降低战斗灯光的
运行时代码；本次是场景布光调整，并不声称修复了逐渐变暗的累积问题。

这次灯光提交只包含 `Assets/Scenes/BattleScene.unity` 和本文档。尚未提交的近战代码、
动作、控制器、Prefab 和近战文档属于另一部分，不应一并勾选。灯光改动不依赖新的近战
实现。

### 配置

环境光改为 Trilight 三色模式，不再依赖默认天空盒的环境探针。RGB 数值如下：

| 设置 | 数值 |
| --- | --- |
| 天空环境光 | 0.46, 0.47, 0.48 |
| 水平环境光 | 0.34, 0.35, 0.36 |
| 地面环境光 | 0.22, 0.21, 0.20 |
| 主光颜色 | 1.00, 0.97, 0.92 |
| 主光强度 | 1.00，保持不变 |
| 主光阴影强度 | 0.80，原为 1.00 |
| 补光颜色 | 0.90, 0.94, 1.00 |
| 补光强度 | 0.35 |
| 补光旋转 | X 25、Y 150、Z 0 度 |

`Battle Fill Light` 是关闭阴影的实时方向光，从相对另一侧提供适量补光，不增加第二张
阴影贴图。主光保持原来的旋转和柔和阴影。天空盒材质、雾、反射设置、相机姿态、UI 和
项目全局质量设置都没有修改。补光与环境光也会影响战斗地面，并非角色专用灯光。
以后战斗环境或人物材质明显变化时，应重新检查这套场景布光。

### 验证

19 项针对性生成检查通过。以同一个静态 Idle 姿势，分别从正面、背面和现有默认战斗
镜头渲染 1280x720 的前后对比。没有执行任何战斗动作，也不声称本次重新测试了 Play
Mode 近战。Linear 色彩空间预览中，模型轮廓内亮度的统计如下：

| 视角 | 调整前中位数，0-255 | 调整后中位数，0-255 |
| --- | --- | --- |
| 正面 | 0.0 | 32.7 |
| 背面 | 46.8 | 69.4 |
| 默认战斗镜头 | 84.1 | 95.6 |

这些数字描述的是保存的静态预览，不保证运行时每个像素相同。上述画面中模型轮廓内的
过亮截断像素始终低于 0.03%。文件哈希确认 FildScene、Boss_FieldScene、
Argo_Lycaon.prefab、四个莱卡恩材质、GraphicsSettings 和 QualitySettings 均保持
不变。保存前已删除临时模型、恢复原有对象激活状态和相机姿态。截图、测量及一次性生成
脚本归档在忽略的 `Library/BattleLightingChecks` 下，不作为游戏资源上传。
