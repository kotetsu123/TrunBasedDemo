# Boss 火山熔岩带 / Boss Volcano Lava Flow

这是独立的熔岩带模型：从入口镜头右侧坡面的山口附近流到半山腰，宽度和边缘有变化。模型已经贴合 `BossVolcano_Graybox` 的表面，原点与山体一致。网格保持静态，Unity 材质通过 UV 噪声向下滚动，呈现缓慢流动的明暗热斑。

This separate ribbon fits the front-right slope of `BossVolcano_Graybox`, descending from near the rim to the middle slope. It shares the volcano's origin and has an irregular outline. The mesh stays static; its Unity material scrolls procedural UV noise downhill to create slowly flowing heat patches.

## Unity 配置 / Placement

1. 将 `Assets/Art/Environment/VolcanoCave/BossVolcano_LavaFlow.fbx` 拖到场景中的 `BossVolcano_Graybox` 根节点下面。
2. 将熔岩带的本地 Position / Rotation 都设为 `(0, 0, 0)`，Scale 设为 `(1, 1, 1)`。它会跟随山体的位置、旋转和缩放。
3. Mesh Renderer 的唯一材质槽使用 `Assets/Materials/BossVolcano_Lava.mat`。导入设置已经配置该材质映射。
4. 如果作为 `VolcanoBackdrop` 下与山体平级的物体，则复制山体的本地 Transform。当前场景山体位置是 `(0, 0, 45)`。
5. 这是远景装饰，无需 Collider。之前用于试验火山口亮面的圆盘可单独保留或关闭。

Parent the FBX to the placed volcano root with zero local position and rotation, and unit local scale. The single material slot is remapped to the existing `BossVolcano_Lava.mat`. As a sibling, copy the volcano's local transform instead. This decorative mesh needs no collider.

## 流动材质 / Flow Material

`Assets/Materials/BossVolcano_Lava.mat` 使用独立的 `VolcanoCave/Boss Lava Flow` Shader，不依赖洞穴的 `VC_Lava.mat`，也无需额外贴图或控制脚本。坡面与火山口圆盘共享该材质；圆盘使用自身 UV，所以只有坡面熔岩带保证沿山坡向下流动。

`Assets/Materials/BossVolcano_Lava.mat` uses the separate `VolcanoCave/Boss Lava Flow` shader, without depending on the cave's `VC_Lava.mat`, extra textures, or a controller script. The slope ribbon and crater disc share this material. The disc uses its own UVs, so only the ribbon guarantees downhill flow.

| Inspector 参数 / Property | 默认值 / Default | 作用 / Effect |
| --- | --- | --- |
| Downhill Flow Speed | 0.10 | 沿 V 方向的 UV 单位/秒；0 停止流动。/ UV units per second along V; 0 freezes the flow. |
| Pattern Scale | 1.5 | 越大，热斑越细。/ Larger values create smaller heat patches. |
| Heat Contrast | 1.6 | 控制暗红与亮橙、亮黄之间的对比。/ Contrast between cool and hot patches. |
| Edge Darkening | 0.65 | 两侧降温变暗，边缘不随纹理移动。/ Fixed cooler banks along the ribbon edges. |
| Smoothness | 0.25 | 表面反光程度。/ Surface smoothness. |

`Lava Color` 与 `Lava Emission` 保留原材质颜色；`Cool Crust Color` 控制暗部，`Hot Core Emission` 控制最热的亮部。流速正值沿熔岩带下坡方向移动，不改变模型位置。调参数时在默认入口镜头检查，也可以靠近确认方向。

`Lava Color` and `Lava Emission` retain the original material colors. `Cool Crust Color` sets the dark areas, while `Hot Core Emission` sets the brightest ones. Positive flow speed moves features downhill without moving the mesh. Check the default entrance view and a close-up when tuning.

## 文件与范围 / Files and Scope

- `BossVolcano_LavaFlow.blend`：包含熔岩带、参考山体、预览灯光和相机的独立源文件；只导出熔岩带。
- `preview_front.png` / `preview_side.png`：Blender 预览，不是 Unity 实机截图，也不包含 Unity 的雾和天空盒。
- `generate.py`：读取已保存山体，通过射线将熔岩带贴到坡面，并检查穿插和 FBX 回读结果。
- `manifest.json`：源模型指纹、面数、离坡面距离和验证结果。

The blend includes the reference mountain and preview setup, but the FBX contains only the ribbon. Previews are Blender renders, not Unity screenshots. The generator records surface clearance and FBX round-trip checks in the manifest.

UV 的 U 横跨熔岩带，V 沿下坡方向增加。Unity Shader 使用该 UV 生成热斑并滚动；Blender 源文件与预览仍是静态材质，不展示 Unity 的流动效果。材质自发光不会自动照亮周围物体，本版没有动态灯光或 Bloom。模型是贴合当前山体生成的，山体网格本身改变后需要重新检查贴合。

UV U runs across the ribbon and V increases downhill. The Unity shader generates and scrolls heat patches using these UVs. The Blender source and previews still use a static material and do not show the Unity animation. Emission does not automatically light nearby objects; dynamic lighting and bloom are not included. Recheck the fit after editing the volcano mesh itself.

## 重建 / Regenerate

在项目根目录执行；仅更新本目录生成文件和熔岩带 FBX，不修改 Unity 场景或原山体。

Run from the project root. Regeneration overwrites this folder's generated files and the lava FBX, preserving the Unity scene and original mountain.

```powershell
& 'D:\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python 'ArtSource/BossVolcanoLava/generate.py'
```
