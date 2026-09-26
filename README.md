# TokenBaby

一只常驻 Windows 桌面的黄色宠物，显示当前 ChatGPT 账号的 Codex 套餐剩余额度。TokenBaby 是社区项目，与 OpenAI 官方无隶属关系。

## 功能

- 透明、始终置顶、可拖动；托盘菜单可隐藏、恢复和退出。
- 单击宠物展开额度面板，查看 5 小时与 7 天额度、重置时间及最近刷新时间。
- 只有 **5 小时剩余比例**决定宠物状态：≤20% 低落、>20% 且 ≤70% 平静、>70% 开心。
- 点击动作与状态对应：低额度擦泪、中额度回头看你、高额度捧腹大笑。待机时有轻微随机动作。
- 通过本机 Codex App Server 读取额度通知，并每分钟刷新。TokenBaby 不读取或保存登录令牌。

## 安装与运行

1. 使用 Windows 和 .NET Framework 4.8；安装 Codex 桌面版或 CLI，并在 Codex 中登录 **ChatGPT 账号**。
2. 从构建产物中解压 `TokenBaby-portable.zip`，保持 `TokenBaby.exe` 与 `assets` 文件夹在同一目录。
3. 运行 `TokenBaby.exe`。单击宠物可查看额度，右键宠物或托盘图标可打开菜单。

如果无法找到 `codex.exe`，可设置环境变量 `TOKENBABY_CODEX_PATH` 为其完整路径。TokenBaby 使用本机现有的 Codex 登录状态；API Key 登录不会返回所需的 ChatGPT 套餐额度。

## 从源码构建

在 Windows PowerShell 中运行：

```powershell
.\tests\run.ps1
.\build.ps1 -Package
```

可执行文件位于 `dist\release\TokenBaby.exe`，便携包位于 `dist\TokenBaby-portable.zip`。构建脚本使用 Windows 自带的 .NET Framework C# 编译器，不需要 NuGet 还原。`./tests/run.ps1 -Live` 可选用本机已登录的 Codex 执行只读联调；GitHub CI 仅运行离线测试。调试窗口可用 `./build.ps1 -DebugUi` 编译。

## 代码结构

| 路径 | 内容 |
| --- | --- |
| `src/App` | 启动、托盘、窗口位置存储 |
| `src/Core` | 额度模型与 JSON 数据解析 |
| `src/Infrastructure` | Codex App Server 子进程通信 |
| `src/UI` | 宠物窗口、点击动画、额度面板 |
| `assets` | 当前六张运行时图片及生成记录 |
| `tests` | 离线解析测试和可选的实时读取探针 |

TokenBaby 只在 `%APPDATA%\TokenBaby\position.txt` 保存宠物窗口位置。它通过 [Codex App Server 协议](https://learn.chatgpt.com/docs/app-server) 的 `account/rateLimits/read` 获取额度，不直接处理账号凭据。

## 许可证与素材

代码与仓库内素材按 [MIT 许可证](LICENSE) 发布。角色图片由 Codex imagegen 依据项目维护者提供、并确认可公开再发布的参考素材制作；原始参考照片未加入仓库。生成过程记录见 [assets/PROMPTS.md](assets/PROMPTS.md)。贡献新图片前请确认相同的再发布权利。

开发说明见 [CONTRIBUTING.md](CONTRIBUTING.md)。
