# 在线试玩 · 2026-11-03

地址：https://tidecraft-sand-play.wentaopeng714.chatgpt.site

Sites 私人预览，使用创建此项目的 ChatGPT 账号登录。原有安卓 APK 保留；网页使用同一 Unity 场景和玩法。

- Unity 6000.3.24f1，WebGL 构建成功，零错误；一项来自性能测试包生成元数据的警告。
- 运行资源压缩后 12.65 MiB。Gzip + Unity Decompression Fallback；不依赖托管端特殊 Content-Encoding 头。
- Web 内存初始 128 MB、最大 512 MB，Medium 托管剥离，BuildTimes 代码优化（此项是本机编辑器设置）。浏览器帧率使用 requestAnimationFrame；限制网页 DPR 为 1.5。
- Web 存档改为 PlayerPrefs；安卓版继续使用本地文件。Unity loader 开启 autoSyncPersistentDataPath。
- 本地 Chromium 内嵌浏览器实测：启动、画墙（1→2）、放塔（2→3）、挖沟引水（false→true）、撤销（true→false）、重做（false→true）、保存→刷新→读取后恢复 2 段墙、3 座塔、已连通凹池。
- 线上实际登录并启动成功，已再次拖动画墙确认 1→2。无浏览器运行错误。未进行 Safari 和实体手机触摸测试。
- 读取了 Sites 技能及 local preview 文档、Unity optimize-web 技能，以及 Unity 官方 Web 部署 / Build 文件夹文档；本次没有添加云存档或其他后端。

发布记录：
- project_id: appgprj_6ae93144b0048191b97965d1f36ff3e1
- deployment_id: appgdep_6ae9347c19d881919b10f6eee2198444
- Site source commit: 66765ed9b9766964b679665d7c15af501f356f7f
- Site source checkout: work/tidecraft-online（包含 .openai/hosting.json，后续更新复用同一个项目 ID）
- Unity export source commit: 本轮 Web 支持代码已提交到本地 Git；详见 git log。

重建：Tools → Tidecraft → Configure browser preview，然后 Tools → Tidecraft → Build browser preview。需安装对应 Unity Web Build Support 模块。
