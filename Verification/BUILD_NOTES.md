# 验证记录 · 2026-11-03

## 工程与环境

Unity 6000.3.24f1 / URP 17.3.0 / Input System 1.20.1 / Cinemachine 3.1.7 / uGUI-TMP / Pipeline 0.9.1-exp.1。

最初启动被无效许可证阻止。随后 Unity Hub 恢复了有效 Personal 许可证；使用 CLI 保持启动会话后，Editor 正常导入。旧模板的 Input System 1.12 引用了该 Editor 已移除的枚举，已在官方 Package Manager 更新到推荐版本 1.20.1。没有修改包缓存源码来掩盖错误。

## 通过的检查

- 实际 Editor 导入、编译与 Play Mode 运行成功；job wait 证明 Time.frameCount 持续增长（613→619，以及后续 524→531）。
- 生产高度场/海洋连通算法：11/11，详见 model-checks.txt。
- 实际镜头屏幕坐标 → 地形拾取 → 工具输入 → 建造/引水的 Play Mode 检查：8/8，详见 play-checks.txt。
- 本地存档保存/加载后恢复 7 段墙、1 座塔和已连通水池，Cinemachine 实际启用。
- Play Mode 检查时 Console：compilationFailed=false、consoleErrors=0、consoleWarnings=0。
- Noto Sans SC 字体已裁剪为界面所需字符，OFL 许可证保留。TMP Essential Resources 已非交互导入。

## 视觉修正与人工评分

实际捕获标准游戏角度、塔楼近景和工具界面。第一张画面有沙地偏亮、标题行高不足的问题；已将网格顶点色转换到线性空间、扩大标题区域，并减少海面重复亮点。修正后的截图位于交付目录 screenshots/。另捕获了实际按住放塔工具时的绿色位置预览（tower-preview.png）；截图时仅暂停桌面鼠标悬停输入以保持预览，未修改建造规则。所有在场 Renderer 均无空材质或 Default-Material。最终运行复查 Console 仍为零错误。动态反馈仍需真机手感复查。

配色: 3/5 — 沙色、青绿色水面、深色工具栏和珊瑚旗帜统一；修正了过亮顶点色。
风格: 3/5 — 地形、沙堡和稀疏环境均为简洁低面数造型。
光照: 3/5 — 斜向暖光、冷色环境补光，墙体和垛口阴影可辨。
轮廓: 3/5 — 本版无角色，以塔楼为评分对象；塔身、垛口和旗帜在近景可辨。
界面: 3/5 — 中文工具栏、选中状态、撤销与保存入口完整；标题已修正，真机可点性仍需检查。
反馈: 3/5 — 地形即时改变、沟渠接海后水面覆盖、笔刷轮廓与工具提示可见；塔楼有落印缩放与合成音效。

这组分数仅为原型的人工自评，不代表已达到参考视频的美术精度或手机性能目标。

## Android 构建

首次构建因 NDK 缺失而失败（android-first-attempt.json）。通过 Unity CLI 安装对应 Android NDK r27c，source.properties 确认版本 27.2.12479018，随后重新构建。

最终构建成功（523976 ms），零错误、零警告；APK 实际文件约 44 MB。构建报告另存 android-build.json。已用 aapt 检查包名 com.tidecraft.sandbox、中文名称、最低 API 26、目标 API 36、arm64-v8a；apksigner 验证 v2 签名有效，ZIP 完整性校验通过。实体手机未连接，未做真机安装和触控/性能验收。开发构建没有包含 Pipeline 运行时控制组件（已从构建日志确认）。

## 使用的指导与资料

使用 Unity 插件技能 new-unity-project、unity-cli、ui / ui-ugui、unity-package-management、craft-game-floors；参考 art-direction、scene-lighting、camera-rigs、game-feel、game-audio、game-shell 和 visual-scorecard，以及 mobile-games。

核心玩法优先，标题/胜负界面、云服务和商业化未纳入本次原型。

核对的官方 API 文档：

- [Unity 6000.3 Mesh](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/mesh)
- [Unity 6000.3 Touch](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/touch)
- [Cinemachine 3.1 Camera](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineCamera.html)

读取本地缓存 TMP_PackageResourceImporter.cs 核实非交互导入及完成回调；读取 Pipeline 构建处理器确认运行时组件排除。
