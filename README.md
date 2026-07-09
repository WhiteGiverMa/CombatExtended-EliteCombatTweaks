# CE Elite Combat Tweaks

Combat Extended 爽局 / 少人精英局数值补丁。

## 功能边界

- **瞄准时间**：`AimingDelayFactor` 的后处理曲线最低从 50% 放到 **1%**。
- **远程冷却**：操作能力、移动能力、呼吸能力提供额外远程冷却乘区，最多由本 mod 减少 75%，最终最低为基础冷却的 1%。
- **武器掌握 / weapon handling**：移除 CE `ShootingAccuracyPawn` 的 XML 平顶，Harmony 替换 `Verb_LaunchProjectileCE.ShootingAccuracy` 的 4.5 硬截断为递减曲线。
- **瞄准精度 / aiming accuracy**：移除 CE `AimingAccuracy` 的 XML 平顶，Harmony 替换 1.5 硬截断；超额精度继续降低 spread/sway，但会保护 CE 的 lead/range/visibility 误差不变成负数。

## 不包含

- 不包含过穿透玩法：见 `CEOverpenetration`。
- 不包含破墙 AI/LoS 兼容修复：见 `CEBreachingFix`。

## 曲线

### 瞄准时间

`AimingDelayFactor` 的后处理曲线改为：

```text
(0.01, 0.01) → (0.25, 0.25) → (0.75, 0.75) → (1.0, 1.0) → (1.25, 1.25) → (2.0, 1.5)
```

也就是最低瞄准时间倍率从 CE 的 `50%` 降到 `1%`。

### 远程冷却

原版 `RangedCooldownFactor` 和 `AimingDelayFactor` 的 `1%` 是 `StatDef.minValue`，不是最终 `Verb` 层统一兜底。`VerbProperties.AdjustedCooldown()` 只是把武器冷却、`RangedCooldownFactor` 等相乘后转为秒数；所以本 mod 在最终 ranged cooldown 秒数入口增加一个额外乘区，并按基础冷却的 `1%` 收底。

```text
M = Manipulation / 操作能力
V = Moving / 移动能力
B = Breathing / 呼吸能力

score =
  0.55 * max(0, M - 1)
+ 0.30 * max(0, V - 1)
+ 0.15 * max(0, B - 1)

eliteMultiplier = 1 - 0.75 * (1 - exp(-score / 0.65))

finalCooldown = max(baseCooldown * 0.01, currentCooldown * eliteMultiplier)
```

`eliteMultiplier` 渐近 `0.25`，表示**本 mod 自己最多减少 75%**；其他机制（如 `RangedCooldownFactor`）仍可以继续把剩余冷却压到基础冷却的 `1%`。

预期值：

| 操作 / 移动 / 呼吸 | 冷却乘区 | 本 mod 减少 |
|---:|---:|---:|
| 100% / 100% / 100% | 100% | 0% |
| 125% / 125% / 125% | 76% | 24% |
| 150% / 150% / 150% | 60% | 40% |
| 200% / 200% / 200% | 41% | 59% |
| 250% / 250% / 250% | 32% | 68% |
| 300% / 300% / 300% | 28% | 72% |
| 极限趋近∞ | 25% | 75% |

单项强化大致效果：

| 操作 / 移动 / 呼吸 | 冷却乘区 |
|---:|---:|
| 200% / 100% / 100% | 57% |
| 100% / 200% / 100% | 72% |
| 100% / 100% / 200% | 85% |
| 180% / 150% / 125% | 54% |
| 250% / 200% / 150% | 37% |

### 武器掌握 / weapon handling

CE 原逻辑把 `ShootingAccuracy` 硬钳到 `4.5`。本 mod 读取未平顶的 raw stat，然后使用：

```text
raw <= 4.5:
  effective = max(0, raw)

raw > 4.5:
  effective = 4.5 + 0.49 * (1 - exp(-(raw - 4.5) / 4))
```

所以它不再突然停在 `450%`，但会渐近 `4.99`。这是为了保护 CE 原后坐力公式：

```text
recoilMagnitude = Pow(5 - ShootingAccuracy, shotCountFactor)
```

`ShootingAccuracy < 5` 可以避免负底数、`NaN` 后坐力和弹道污染。

### 瞄准精度 / aiming accuracy

CE 原逻辑把 `AimingAccuracy` 硬钳到 `1.5`。本 mod 读取未平顶的 raw stat，然后使用：

```text
raw <= 1.5:
  effective = max(0, raw)

raw > 1.5:
  effective = 1.5 + ln(1 + raw - 1.5) * 0.25
```

CE 的 lead/range/visibility 误差公式仍以 `1.5`/`2.0` 为零点；超额精度不会让误差变成负数：

```text
accuracyFactor = max(0, finite((1.5 - aimingAccuracy) / sightsEfficiency))
visibilityShift = max(0, finite(environmentShift * distanceFactor * (2 - aimingAccuracy)))
```

超额精度继续压低散布和摇摆：

```text
spreadDegrees *= 1 / sqrt(1 + max(0, effective - 1.5) * 0.75)
swayDegrees   *= 1 / sqrt(1 + max(0, effective - 1.5) * 0.75)
```

### 极端值保护

- raw stat 是 `NaN`：回退到安全默认值。
- raw stat 是 `+Infinity`：转为 `float.MaxValue` 后走递减曲线。
- raw stat 是 `-Infinity`：按极低值处理，最终有效值不低于 0。
- 远程冷却当前值、基础值或能力值异常：转为非负有限值；最终冷却不低于基础冷却的 1%。
- `accuracyFactor` / `visibilityShift` 是负数、`NaN` 或 `-Infinity`：回 0。
- `accuracyFactor` / `visibilityShift` 是 `+Infinity`：转为 `float.MaxValue`，不产生 `NaN`。

## 启动自检

加载完成后会检查 XML patch 是否生效：

- 生效：打印 `[CE Elite Combat Tweaks] Stat XML patches active...`
- 失败：打印 `Log.Warning`，包含当前 `AimingDelayFactor@0.01`、`ShootingAccuracyPawn.maxValue`、`AimingAccuracy.maxValue`。

## 构建

```bash
cd Source/CEEliteCombatTweaks
dotnet build -c Release
```

DLL 输出到 `Assemblies/CEEliteCombatTweaks.dll`。
