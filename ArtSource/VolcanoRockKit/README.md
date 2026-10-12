# Volcano Rock Kit / 火山岩石资源组

Reusable low-poly rocks extracted from `ArtSource/VolcanoCave/VolcanoCave.blend`.
The original cave source and FBX are not modified. No scene is changed by generation.

从洞穴源文件中提取的独立低多边形岩石。生成过程不会修改原洞穴文件、原 FBX 或任何游戏场景。

## Assets / 模型

Unity models are in `Assets/Art/Environment/VolcanoCave/Rocks/`.
Drag an FBX from the Project window into the scene; no unpacking of the cave model is needed.

Unity 模型位于上述 `Rocks` 文件夹，直接把 FBX 从 Project 窗口拖进场景即可，无需拆开洞穴大模型。

| Model / 模型 | Use / 用途 | Approximate Size / 大致尺寸 (X × Y × Z, m) |
| --- | --- | --- |
| `VC_Boulder_A` | Tall rock formation / 较高的大岩石组 | 3.5 × 5.8 × 3.5 |
| `VC_Boulder_B` | Shorter rock formation / 较矮的大岩石组 | 3.9 × 4.0 × 4.2 |
| `VC_Rock_Small_A` | Upright small rock / 竖向小岩石 | 0.9 × 1.6 × 0.8 |
| `VC_Rock_Small_B` | Flat small rock / 扁平小岩石 | 1.7 × 0.5 × 1.9 |
| `VC_Rubble_A` | Five loose stones / 五块散落碎石 | 1.9 × 0.4 × 2.3 |
| `VC_Rubble_B` | Five loose stones / 五块散落碎石 | 1.4 × 0.3 × 2.3 |

All models have a bottom-center pivot, meter scale, baked vertex colors, and one material slot
remapped to the existing `VC_Rock` material. The `.blend` arranges them for inspection;
each exported FBX is at the origin with unit scale. Exact dimensions and source object names
are recorded in `manifest.json`.

所有模型的原点均位于底部中央，以米为单位，保留原顶点颜色，只使用一个材质槽，
并通过 `.meta` 引用已有的 `VC_Rock` 材质。`.blend` 中为方便查看而分开展示，
各个 FBX 导出时的位置为原点、缩放为 1。准确尺寸与原始对象名称记录在 `manifest.json` 中。

## Placement / 摆放

Use the large formations along the arena edges, small rocks at their feet, and rubble sparingly.
Keep the central approach and the Boss/volcano silhouette clear. Colliders are not generated:
decorative rocks outside the movement boundary do not need them; reachable solid rocks need
a suitable Box Collider or other collider configured in Unity.

大岩石沿场地边缘摆放，小岩石放在其脚边，碎石少量点缀。保留中央通路，避免遮挡 Boss 和火山。
模型没有自动生成碰撞体：移动边界外的装饰无需碰撞体；玩家能够接触且不能穿过的岩石，
需在 Unity 中配置合适的 Box Collider 或其他碰撞体。

## Regenerate / 重新生成

Run from the project root in a new Blender background process:

从项目根目录运行，使用独立的 Blender 后台进程：

```powershell
& 'D:/Steam/steamapps/common/Blender/blender.exe' --background --factory-startup --python-exit-code 1 --python ArtSource/VolcanoRockKit/generate.py
```

This regenerates the kit's FBXs, `.blend`, preview and manifest. Existing importer `.meta`
files are preserved. Geometry and vertex colors are checked by reimporting every FBX;
this does not replace an in-game placement check.

这会重新生成本资源组的 FBX、`.blend`、预览图和清单，但保留已有的导入设置 `.meta`。
脚本会重新读取每个 FBX 来检查几何与顶点颜色；这些检查不能代替场景内的实际摆放测试。
