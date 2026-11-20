# Paul 圣光飞剑 / Holy Sword

## 当前配置

- 技能资源：`Assets/Scripts/Data/SkillScriptableObject/HolySword.asset`。
- 类型：Damage；目标：EnemySingle；暂定威力 `power = 20`、耗蓝 `mpCost = 10`。
- 当前公式仍是 `Attack + power`，不是倍率。Paul 攻击力 100 时造成 120 点伤害。
- Paul 的新游戏初始数据、角色数据库都已加入该技能，原有五个技能保留；Argo 不变。
- 探索 Skill 页沿用现有规则，攻击技能显示为不可使用。

## 执行过程

1. 使用现有双人互动镜头，等待镜头就位。
2. Paul 上方凝聚横向朝着敌人的白金色尖锐十字光刃，随后约 0.09 秒直线刺中，留下一道短暂的光痕。
3. 命中时通过原来的 `UseSkill` 扣蓝、计算一次伤害。
4. 播放光环和火花，清理临时网格和特效对象。

`BattleHolySwordFeedback` 负责表现，不直接修改 HP、MP。
`BattleManager.PlaySkillSequence` 只增加光剑的表现分支。
`Player.prefab` 上绑定技能、光剑材质以及已有的粒子材质。
匹配使用 SkillData 资源引用，不依赖技能名称或 Paul 的角色 ID。

## 修改入口

- 改伤害加值和耗蓝：选中 `HolySword.asset`，修改 Power、Mp Cost。
- 改大小和节奏：选中 `Player.prefab` 的 `BattleHolySwordFeedback`，修改 Sword Scale、Summon Time、Flight Time、Impact Time。
- 改颜色亮度：`BattleHolySword.mat` 的 Tint、Intensity；十字光刃的白金配色在 `BuildLightBlade` 中。
- 本次不包含 Argo 卡牌动作、泰德背刺、属性克制、音效或额外 Debuff。

## 验证

横向十字光刃初版在 Unity 2022.3.62f2 中通过 74 项断言，包括尖端命中位置、技能菜单、探索限制、单次伤害和扣蓝、组件禁用、FireBall 回归及致死结算。直刺版另外调整了飞行路径和时长。
在 1280×720、800×600 截图检查召唤效果和镜头范围。
临时验证脚本、截图和结果放在忽略的 `Library/HolySwordChecks`，不需要提交。

## English Summary

Adds Paul's single-target Holy Sword with a white-gold pointed cross of light, flight and impact effect.
The abstract blade appears horizontally, then snaps toward its target in a straight 0.09-second thrust.
Temporary balance is 20 power and 10 MP; damage remains Attack + power.
Existing skills, Argo's loadout, scenes and shared damage rules are unchanged.
The visual component owns temporary objects and meshes; the existing skill code resolves damage and MP once at impact.
