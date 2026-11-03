沙岸工坊 · Loopit HTML5 网页运行包

入口是 ZIP 根目录的 index.html。保留 Build/、cover.jpg、licenses/ 的相对位置。
包中带齐游戏、模型、字体、声音和 WebAssembly 运行文件，不需要 CDN、API Key 或 Unity 编辑器。
打开页面会自动加载，手机竖屏。先画墙、放塔或筑沙堤，再点“迎战海浪”，保护青色旗台三轮。

导入：作为 HTML5 网页游戏导入此 ZIP，入口选择 index.html。
托管环境需要支持 WebGL 2、WebAssembly 与相对资源请求。Build 中使用普通 .wasm/.js/.data 文件，无需自定义 Content-Encoding。
可编辑 Unity 工程在 source/Unity/，不是网页入口。
本地预览需使用 HTTP 服务：python3 -m http.server 8000，然后打开 http://localhost:8000。

已按独立静态网站与 iframe 嵌入方式进行浏览器验证。尚未在 Loopit 导入器内实测；平台若限制 WebAssembly，需要按其限制另行适配。
