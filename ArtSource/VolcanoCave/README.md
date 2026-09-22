# 火山山洞环境

源场景：`Assets/Scenes/FildScene.unity`。Blender：5.2.2 LTS。固定种子：20260920。

## 交付内容

- `VolcanoCave.blend`：可编辑的独立 VolcanoCave 场景、分段网格、材质、三个检查相机。
- `Assets/Art/Environment/VolcanoCave/VolcanoCave.fbx`：Unity 导入模型。
- `Assets/Art/Environment/VolcanoCave/VolcanoCaveEnvironment.prefab`：模型、材质、暖色灯光、火星/灰尘和局部遮挡淡出组件。
- `layout.json`：从 Unity 当前打开的场景提取的 26 段碰撞墙体及重要点位。坐标以世界米为单位。
- `manifest.json`：生成种子、网格数量和三角面数量。
- `Previews/`：Blender 渲染、Unity 环境相机及 Play Mode 检查截图。

## 修改与重建

1. 打开 Unity 的 FildScene，退出 Play Mode。菜单 **Tools > Volcano Cave > 1 Export Current Layout** 导出最新墙体。
2. 在 Blender 中打开源文件并启用 MCP。通过 MCP 执行 `generate.py`，或者用 Blender 的 Scripting 工作区执行该文件。脚本路径 `ROOT` 是本项目绝对路径，移动项目后修改该值。
3. 回到 Unity，等待导入/编译完成。依次执行菜单 **2 Build Environment**、**3 Verify Alignment and Route**、**4 Render Preview Images**。
4. **5 Run Play Mode Smoke Test** 运行可选的场景往返验证。它会进入并退出 Play Mode、执行运行时交互；不写玩家存档。

生成脚本只重建名为 VolcanoCave 的场景。该场景属于生成器；手工美术修改应另存副本，避免重建覆盖。Blender 内其他场景保持原样。

Unity 的构建工具只替换 Environment 下的 VolcanoCaveEnvironment；保留原 Walls 和 Ground_Plane 的碰撞，关闭它们的白盒渲染。若根层级存在同一个 FBX 的手工预览实例，保留该对象并关闭重复渲染。每次重建前，当前编辑场景复制到 `Library/VolcanoCave/SceneBackups/`。首次操作前的副本另保存在本目录的 `FildScene.before-volcano.unity`，这些备份不进入版本控制。

## 坐标、出口与表现

- Unity `(x,y,z)` 转为 Blender `(x,z,y)`，同时反转面绕序；FBX 使用 `-Z` forward、`Y` up 和 Unity `bakeAxisConversion`。根物体保持单位缩放。导入后对每段墙的世界位置校验，错误时停止接入。
- 出口岩拱位于原 BossFieldTransitionTrigger 的西侧边界，中心约 `(11.963,0,15.48)`，正对 BossExitReturnSpawnPoint 来路，通道朝 +X 延伸。原切换目标仍为 Boss_FieldScene / BossFieldEntry。
- 岩壁下部沿原碰撞轮廓，上部允许悬岩；熔岩和碎石为装饰，不引入地形伤害或新的阻挡体。
- 使用 Built-in 管线的 Rock / Lava shader。熔岩在世界坐标中缓慢流动；材质和地面不依赖 Blender 节点自动转换。
- 岩石与地面分色烘焙为线性顶点色 `VC_Color`，FBX 按 LINEAR 导出，Unity 的 VC_Rock 材质读取顶点色。请使用环境预制体或构建菜单接入，直接拖入 FBX 的默认 Standard 材质不会显示此调色效果。
- VolcanoCaveVisibility 在相机到角色的视线上按网格包围盒进行保守淡出，使用 MaterialPropertyBlock，不修改共享材质、不关闭碰撞。它在现有相机更新之后执行。

## 验证说明

`unity-validation.txt` 记录坐标、碰撞、着色器、预算和网格连通检查。
`playtest.txt` 记录实际 Play Mode 检查结果。宝箱与招募使用其运行时公开方法，换关使用物理触发。根据用户要求，战斗与战斗返回测试暂缓，避免强制 Tutorial 干扰。测试只在临时 Play Mode 会话中关闭敌人碰撞，退出后恢复，不保存这种状态。这是环境烟雾验证，不等同于完整的人工作战流程。

当前结果：45,328 三角面、129 个网格、131 个渲染器（包含两个粒子系统）、5 个实际使用的共享材质、4 盏无阴影暖色点光。模型材质槽由 524 个合并到 129 个。最新编辑器帧统计为 470 draw calls / 191,114 渲染三角面；该统计包含打开的编辑器视图及阴影、灯光多遍绘制，不是模型面数，也不是独立打包程序的性能基准。

26 段墙体位置与原碰撞边界校验通过；新环境无新增碰撞体；出生点至出口的网格连通检查通过。相机在入口、中央、出口各测试 12 个姿态，共 36 个，验证淡出及恢复。另用角色实际动态刚体从出生点经过中央区域行走到出口前，累计 108.0 米、228 个路径点，保持贴地且无卡墙。路径计算仅用于编辑器测试，不为游戏新增导航系统。正式发布前仍应做目标设备的独立运行性能检查。

原相机允许较低的俯仰角；局部洞顶采用开放设计，低角度可能看到暗色背景。这里交付的是适配现有探索玩法的首版风格化环境。
