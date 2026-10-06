# Boss 火山灰盒 / Boss Volcano Greybox

一个宽底、窄顶、带凹陷火山口的低面数背景山体。原点位于底部中心，宽约 40 米、高约 24 米，用于 Boss 场景构图。

A single low-poly background volcano with a recessed crater and a bottom-center pivot. Approximately 40 m wide and 24 m high, intended for Boss-area composition.

## 文件 / Files

- `BossVolcano_Graybox.blend`：可编辑源文件，包含预览相机和灯光。
- `../../Assets/Art/Environment/VolcanoCave/BossVolcano_Graybox.fbx`：Unity 模型，只包含一个山体网格。
- `preview.png` / `preview_front.png`：斜上方与较低正面视角的 Blender 预览，不是 Unity 实机截图。
- `generate.py`：重复生成脚本，主要外形参数位于 `build_model` 的 `rings` 中。
- `manifest.json`：模型尺寸、面数和网格检查结果。

## Unity 摆放 / Placement

1. 退出 Play Mode，把 FBX 拖到 `Environment/VolcanoBackdrop` 下，作为原圆柱的同级物体。
2. 新模型先使用 Scale `(1, 1, 1)`、Rotation `(0, 0, 0)`，Position 可从 `(0, 0, 60)` 试摆。原点已经在底部，Y 不需要提高半个山体高度。
3. 暂时禁用 `VolcanoMass_Graybox`，从 Game 相机看效果，再调整新山体的位置和大小。
4. 模型只有一个材质槽，可以替换为普通岩石材质。没有烘焙 `VC_Color` 顶点色，依赖顶点色的材质需要另行配置。
5. 当前用途是远景装饰，无需添加碰撞体。

Place the FBX beside the cylinder under `Environment/VolcanoBackdrop`. Start with unit scale and ground-level Y, then adjust using the Game camera. Preview lights, camera, and floor are excluded from the FBX. The generator does not edit Unity scenes.

## 重建 / Regenerate

在项目根目录执行 / Run from the project root:

```powershell
& 'D:\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python 'ArtSource/BossVolcanoGraybox/generate.py'
```

脚本只接受新的后台会话，会覆盖本目录的生成结果和对应 FBX。手工修改源模型后请另存为不同文件，避免重建覆盖。

Regeneration overwrites the generated outputs. Keep hand-edited variants in separate files.
