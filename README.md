# 沙岸工坊 · Tidecraft 0.1

Unity 手机横屏沙滩建造原型源码。围绕参考视频中的连续画墙、模具放塔、挖沟引水实现，使用自制几何与配色。

## 在线试玩

[打开沙岸工坊在线版](https://tidecraft-sand-play.wentaopeng714.chatgpt.site)，使用当前 ChatGPT 账号登录后点“开始试玩”。手机建议横屏。网页启动、三项核心玩法、撤销和跨刷新存档均已实测；具体记录见 Verification/WEB_PREVIEW.md。

## 当前交付状态

- 画城墙、三种塔楼、挖沟引水、手机触控、中文界面、撤销/重做、本地保存/读取已有实现。
- Unity 6000.3.24f1 中已成功导入、编译并运行，实际帧计数持续增长。
- 生产用地形/连通算法 **11/11** 检查通过；通过实际镜头拾取和工具入口进行的 Play Mode 检查 **8/8** 通过，涵盖画墙、吸附放塔、无效重叠、挖沟接海、撤销/重做、手势取消与 UI 阻挡。
- 修正画面后已复查实际截图；运行检查时 Console 零错误。截图见交付目录 screenshots。
- Cinemachine 3.1.7 已安装并确认实际启用。模板自带 Input System 1.12 不兼容本机 Editor，已通过 Unity Package Manager 更新到 1.20.1。
- **Android ARM64 APK 已构建成功**，约 44 MB，最低 Android 8.0，目标 API 36；归档与签名验证通过。构建零错误、零警告。安装包在交付目录 `Tidecraft-Android-0.1.apk`。
- **尚未在实体手机上验证触控手感、帧率和温升**。

## 打开工程

1. 在 Unity Hub 登录，确认 Licenses 中有有效 Personal 或现有许可证。
2. 用 **Unity 6000.3.24f1** 打开本目录（Assets、Packages、ProjectSettings 所在目录）。父目录内的“打开Unity原型.command”可使用本机已安装的 Editor 启动。
3. 等待 Package Manager 下载依赖、脚本和字体导入。首次完成导入后，编辑器脚本会生成并打开 `Assets/Tidecraft/Scenes/Beach.unity`。
4. 如果没有自动生成，点 **Tools → Tidecraft → Prepare playable scene**，然后点 Play。
5. Cinemachine 3.1.7 已通过包管理器安装，触控镜头已确认使用它。Pipeline 只用于编辑器开发，本次构建不包含其运行时控制组件。

编辑器搭建脚本在 `Assets/Tidecraft/Editor`，只在编辑器运行，不会随手机版发布。工程没有运行时云端依赖，不需要玩家账号；首次导入依赖包需要联网。

## 三个试玩步骤

1. 选择“画城墙”，在左侧沙地单指拖出路径。预设的 L 形城堡可继续扩建。起点靠近墙端或塔中心时会吸附。
2. 选择“放塔楼”，按住预览，松手放置。右下按钮切换圆塔、方塔、高塔。青色表示可放置，红色表示水面或已有塔楼占位。触屏预览会抬到手指上方。
3. 选择“挖水沟”，从中央干凹池（世界坐标 1,-1）的右边向右侧海面连续挖掘。通到海洋后，海水沿新沟渠逐格进入。只挖一段未接海的沟不会自动变成水。用“堆沙地”可封沟。

本版引水是固定海平面下的高度场连通与渐进显示，**不是完整流体模拟**。封闭水道后失去海洋连接的水会消失；暂不模拟积水保留、潮汐侵蚀或沙堡倒塌。独立墙体和塔楼的占地会阻水；移除后重新计算连通。

## 手机操作

- 单指拖动：当前工具。单指松手：完成一段墙或放塔。
- 双指拖动：平移；捏合：缩放；双指旋转：旋转视角。
- 开始双指操作会取消当前未完成的单指操作；松开一根手指不会误画。
- 底部切换工具；右下切换模具或笔刷；右上撤销、重做、保存、读取、恢复视角、声音、重置。
- 适配安全区域，横屏；进入后台自动保存已完成的编辑。

电脑调试：左键编辑，右键拖动旋转，中键拖动平移，滚轮缩放。1/2/3/4 切换城墙/塔楼/挖沟/堆沙，Cmd-Z 或 Ctrl-Z 撤销。

## 打包

Android：在 Hub 给同一 Editor 安装 Android Build Support、SDK/NDK、OpenJDK。本机已修复缺失的 NDK，安装 r27c（27.2.12479018）。

执行 **Tools → Tidecraft → Build Android APK**，成功后产物在 `Builds/Android/Tidecraft-0.1.apk`。配置为 ARM64、IL2CPP、最低 Android 8.0、自动选择已安装目标 SDK、开发构建。

iOS：安装同版本 iOS Build Support 后，执行 **Tools → Tidecraft → Export iOS Xcode project**，再用 Xcode 配置自己的签名和设备。尚未生成 iOS 包。

## 验证与文件

- `Assets/Tidecraft/Scripts/SandModel.cs`：129×129 高度场、笔刷、四邻接海洋洪泛。
- `SandWorld.cs`：地形网格、岸边裁切、水的渐进扩展、射线拾取。
- `MeshFactory.cs`：连续城墙、垛口和三类塔楼。
- `SandApp.cs`：工具、吸附、撤销、本地存档和阻水占地。
- `SandInput.cs` / `BeachCamera.cs`：触控手势与电脑调试。
- `SandHUD.cs` / `SandAudio.cs`：中文触控界面、合成音效、静音。
- `Assets/Tidecraft/Editor/ModelChecks.cs`：同一生产模型的自动检查；编辑器菜单也可运行。
- `Verification/`：本次离线验证记录及后续 Play Mode 验收清单。

中文字体来自 Google Fonts 的 Noto Sans SC，已裁剪成界面所需字符；OFL 许可证随字体保留。新增中文文案时需补充字形。模型、材质、图标和音效由本项目代码生成，没有使用视频中的原始资产。
