# 海浪防守原型 · 2026-11-03

## 玩法与边界

- 初始为布防，点击“迎战海浪”后开始三轮挑战，轮间休整 16 秒。
- 有限行程涌浪使用 64×64 四邻接网格，支持穿缺口、绕墙端和越过较低沙堤。退潮不修改原挖沟连通数据。
- 每轮对同一建筑结算一次冲击，塔楼按类型抵挡部分伤害。受击变色、飞溅、耐久条与音效同时反馈，耐久归零时建筑消失，后续涌浪可进入缺口。
- 守住青色旗台即可过关。暂停保留本轮状态；重新布防恢复本次迎浪前的建造快照，每轮也有撤销快照。
- 兼容旧 v1 存档，新存档包含建筑耐久。挑战进度不保存；撤销、重做、读取与重置回到布防。
- 当前为玩法原型，尚不模拟完整流体力学、沙地侵蚀或建筑刚体碎裂。未在实体手机上复验多指手感和持续帧率，本次未重新生成 Android APK。

## 验证

- 生产 WaveFlood 模型检查 **9/9**：无防守可达、封闭防线阻挡、缺口渗入、绕墙端、浪高与堤坝、有限行程、移除防线、地形不变、退潮清理。
- 实际 Unity Play Mode 集成检查 **8/8**：无防守失败、墙受损而目标受保护、快照复制耐久、暂停冻结倒计时、三轮可胜且弱墙冲毁、重新布防精确恢复、撤销恢复、飞溅复用同一个粒子系统。
- 帧数由 29 变到 35，job wait 观察到 2 帧并满足条件；另一次读数 3218，证明游戏真实运行。Console 零错误。
- WebGL 构建成功，零警告、零错误；见 WAVES_BUILD.json。
- 浏览器 390×844 与 375×667：按钮和耐久栏完整可见；真实 UI 三轮挑战以旗台 **34%** 耐久过关，挡浪 **7** 次、冲毁 **1** 段墙。
- 浏览器实际操作：重新布防恢复作品，暂停与继续，保存受损作品后撤销与读取，墙段 1→2、塔楼 2→3、挖沟后 basinWet=true；运行日志无警告或错误。

## 画面检查

第一版草图已检查。修正字体子集更新后的旧字形缓存，增加浪头高度和泡沫宽度，提高涌浪颜色辨识度，将水面重建限制到约 16 Hz。保持原调色板、相机与 uGUI 层级。

palette discipline: 4/5 — 新增海水、泡沫、旗台与受击色沿用 Palette。

style uniformity: 3/5 — 浪头、旗台、墙塔保持现有简化几何风格。

lighting depth: 3/5 — 保留暖沙与冷海分离及现有阴影。

silhouette: 3/5 — 青色旗台与塔旗可区分，浪脊显示推进方向。

UI intentionality: 3/5 — 安全区内对齐，迎浪按钮有足够触控尺寸，两种竖屏尺寸均可读。

feedback visibility: 4/5 — 截图可见浪头触墙、墙体受击变色与耐久条，结算显示剩余耐久。

截图：交付目录 screenshots/waves-defence.png、waves-victory.png。简化浪体仍能看到网格边缘。

## 工作流和资料

技能：game-design、unity:craft-game-floors（game-feel、vfx、game-audio、visual-scorecard）、unity:ui-ugui、unity:optimize-text-mesh-pro、unity:unity-cli、sites:sites。

使用 [ParticleSystem.Emit](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/particlesystem/emit) 的显式发射参数复用喷溅系统，音效使用 [AudioSource.PlayOneShot](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/audiosource/playoneshot)，浪脊和耐久条使用 [LineRenderer](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/linerenderer)。

新增 WaveFlood、SandWaves、SandDefenseVisual、TideWave shader 及两个 Editor 检查脚本；更新 HUD、建造快照、字体子集、短音效与 Web 可观察状态。没有安装新游戏包；Unity CLI 按技能要求从 beta.12 更新到 beta.15。Editor 回到停止状态，后台运行与 Pipeline 配置已恢复。


## 发布记录

URL: https://tidecraft-sand-play.wentaopeng714.chatgpt.site

Site source: d43924927ae62c7e78b756e07826c77a6ab4609c

Deployment: appgdep_6ae958730898819191d93792e13e0700 (succeeded)

Live startup and wave impact verified. Proof: screenshots/waves-online.png. After the check, the game was returned to Build with its original seed castle.

Palette scan: 0 default materials, 0 untinted panels; 2 audio sources playing, central gain layer verified; 1 particle system reused. See WAVES_CRAFT_SCAN.json.
