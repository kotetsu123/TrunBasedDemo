# Boss 火山熔岩带 / Boss Volcano Lava Flow

第一版是独立的静态熔岩带模型：从入口镜头右侧坡面的山口附近流到半山腰，宽度和边缘有变化。模型已经贴合 `BossVolcano_Graybox` 的表面，原点与山体一致。

This first version is a separate static ribbon fitted to the front-right slope of `BossVolcano_Graybox`, descending from near the rim to the middle slope. It shares the volcano's origin and has an irregular outline.

## Unity 配置 / Placement

1. 将 `Assets/Art/Environment/VolcanoCave/BossVolcano_LavaFlow.fbx` 拖到场景中的 `BossVolcano_Graybox` 根节点下面。
2. 将熔岩带的本地 Position / Rotation 都设为 `(0, 0, 0)`，Scale 设为 `(1, 1, 1)`。它会跟随山体的位置、旋转和缩放。
3. Mesh Renderer 的唯一材质槽使用 `Assets/Materials/BossVolcano_Lava.mat`。导入设置已经配置该材质映射。
4. 如果作为 `VolcanoBackdrop` 下与山体平级的物体，则复制山体的本地 Transform。当前场景山体位置是 `(0, 0, 45)`。
5. 这是远景装饰，无需 Collider。之前用于试验火山口亮面的圆盘可单独保留或关闭。

Parent the FBX to the placed volcano root with zero local position and rotation, and unit local scale. The single material slot is remapped to the existing `BossVolcano_Lava.mat`. As a sibling, copy the volcano's local transform instead. This decorative mesh needs no collider.

## 文件与范围 / Files and Scope

- `BossVolcano_LavaFlow.blend`：包含熔岩带、参考山体、预览灯光和相机的独立源文件；只导出熔岩带。
- `preview_front.png` / `preview_side.png`：Blender 预览，不是 Unity 实机截图，也不包含 Unity 的雾和天空盒。
- `generate.py`：读取已保存山体，通过射线将熔岩带贴到坡面，并检查穿插和 FBX 回读结果。
- `manifest.json`：源模型指纹、面数、离坡面距离和验证结果。

The blend includes the reference mountain and preview setup, but the FBX contains only the ribbon. Previews are Blender renders, not Unity screenshots. The generator records surface clearance and FBX round-trip checks in the manifest.

材质只让表面发亮；本版没有滚动纹理、动态灯光或 Bloom。UV 的 U 横跨熔岩带，V 沿下坡方向增加，后续可接入流动效果。模型是贴合当前山体生成的，山体网格本身改变后需要重新检查贴合。

Emission makes the surface bright. Scrolling textures, dynamic lighting, and bloom are not included. UV U runs across the ribbon and V increases downhill for future flow animation. Recheck the fit after editing the volcano mesh itself.

## 重建 / Regenerate

在项目根目录执行；仅更新本目录生成文件和熔岩带 FBX，不修改 Unity 场景或原山体。

Run from the project root. Regeneration overwrites this folder's generated files and the lava FBX, preserving the Unity scene and original mountain.

```powershell
& 'D:\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python 'ArtSource/BossVolcanoLava/generate.py'
```
