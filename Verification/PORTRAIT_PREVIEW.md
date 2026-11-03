# 竖屏在线预览 · 2026-11-03

- 继续使用原在线 Site 与访问范围。网页容器改为竖屏，手机填满可用视口，电脑居中显示竖屏画面。
- HUD 使用上方 2×4 操作区、底部五工具栏，安全区域随屏幕重排；初始镜头可看到预设沙堡、凹池与海岸。
- Unity PlayerSettings.SplashScreen.show 和 showUnityLogo 均为 false；构建步骤不包含 Unity Splash。网页使用现有沙岸工坊启动页。
- Unity 6000.3.24f1 WebGL 构建成功，零警告、零错误。
- 在真实浏览器 390×844 与 375×667 视口检查：无横向滚动，工具和弹窗可见；实际画墙、放塔、引水、撤销与重做成功。控制台无警告或错误。此为手机尺寸浏览器检查，尚未在实体手机上复验。
- 既有 Android 0.1 APK 为旧横屏版本，本次未重新打包 Android。工程已更新竖屏默认设置。

在线发布成功： https://tidecraft-sand-play.wentaopeng714.chatgpt.site

Site source commit: ba0c9b2ef2b44a814fb5c5fde28edcc8a41d13a7
Deployment: appgdep_6ae953a9888c819199763f6aa0dc53d0
