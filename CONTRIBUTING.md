# 参与开发

欢迎提交问题和 Pull Request。Windows 开发时请先运行 `./tests/run.ps1`，再运行 `./build.ps1 -Package`；macOS 开发时请运行 `./build-macos.sh`，并确认生成的 `dist/TokenBaby.app` 可以启动。

代码目录：

- `src/App`：启动、托盘和窗口位置。
- `src/Core`：额度数据模型和纯解析逻辑。
- `src/Infrastructure`：与本机 Codex App Server 通信。
- `src/UI`：桌宠、点击动画和额度面板。
- `macos`：macOS 原生菜单栏、桌宠窗口和 Codex App Server 通信。
- `tests`：离线解析测试；`-Live` 参数会使用本机登录状态进行只读联调。

提交前请检查 `git status`，不要加入登录令牌、日志、个人配置、未获再发布权利的图片或原始参考照片。新增美术素材请在 `assets/PROMPTS.md` 记录来源和生成方式，并确认可以按仓库许可证再发布。
