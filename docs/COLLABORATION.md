# 三人协作开发指南

适用当前仓库：https://github.com/Peanora/YesterdayConvoy 。这是一起开发Unity源工程，游戏仍为离线单机，不是增加联机玩法。

2026-10-20：三名开发者分别使用 Codex。日常 Git / PR 和冲突处理的统一流程见根目录 [CONTRIBUTING.md](../CONTRIBUTING.md)，本指南保留成员、环境、文件责任、检查范围与发布分工，避免维护两套操作步骤。

## 1. 成员与权限

| 成员 | GitHub | 工作方式 |
| --- | --- | --- |
| 干宸骅 | Peanora | 仓库所有者；规则决定、集成、主场景维护、最终试玩与发布 |
| 霍利屹 | shigaharuki17 | 写入权限邀请已发出；接收邀请后独立分支开发 |
| 宋方壹 | 1164571767 | 写入权限邀请已发出；接收邀请后独立分支开发 |

邀请发出不等于已经接受，也不代表成员完成了开发。两位先登录各自GitHub账号，打开仓库或GitHub通知/邀请邮件，接受邀请。不要共享Peanora账号、密码或令牌。仓库保持当前公开状态。

## 2. 每个人第一次配置

1. 安装[GitHub Desktop](https://desktop.github.com/)，登录自己的GitHub账号。
2. File → Clone repository → URL，填`https://github.com/Peanora/YesterdayConvoy.git`，克隆到自己的本地文件夹。不要把三个人的工程放在同一个共享目录，也不要用QQ互传整个工程覆盖。
3. 安装Unity Hub和精确版本 **6000.6.5f1**，加装Windows Build Support（需要打包的人安装）；不要因Hub提示更新而升级项目。
4. Unity Hub → Add/Open project from disk，选择克隆后同时含Assets、Packages、ProjectSettings的根目录。首次导入需要等待，Library由本机生成。
5. 在Unity打开`Assets/_Game/Scenes/InitialDemo.unity`，或用Yesterday Convoy → Initial Demo → Open Current Scene；点Play验证。Game视图选Full HD并关闭Low Resolution Aspect Ratios。
6. 先看`docs/STATUS.md`当前状态、`docs/GDD.md`相关规则、本指南及[CONTRIBUTING](../CONTRIBUTING.md)。AGENTS.md不随仓库分发，克隆后缺失是正常的；每人使用Codex前按CONTRIBUTING第1节创建本地引导，已有完整指令时合并，共享流程不依赖主导者私有文件。

只试玩的人无需安装Unity，到仓库[Releases](https://github.com/Peanora/YesterdayConvoy/releases)下载Windows附件，完整解压后运行Game/YesterdayConvoy.exe。GitHub的Source code不是可玩包。

## 3. 每次开发照这个顺序

按[CONTRIBUTING第1—4节](../CONTRIBUTING.md)执行：认领与确认范围 → Agent自动检查工作区、同步最新main并创建独立功能分支 → 开发和验证 → Agent自动提交自己的任务文件、推送并创建PR → 人工审查与合并。新功能的提交 / 推送 / PR已有用户持续授权；发现既存未提交修改或冲突必须先报告，不得覆盖。Agent不得自动合并main。

## 4. 场景和Prefab怎样分工

Git不能让同一Unity场景安全地随意同时编辑。CODEOWNERS只是审查人，不是文件锁；在Issue里约定唯一写入者。

| 文件范围 | 当前约定 |
| --- | --- |
| InitialDemo.unity、已有共享Prefab、ManualDemo/WisadelVsKroos配置、ProjectSettings、Packages | 默认由干宸骅集成；其他成员要修改先认领，期间主导者也避免同时改 |
| 普通C#逻辑 | 可按不同脚本分工；共享接口先约定，不能各自改同一大类后最后硬拼 |
| 新角色/图标/音效 | 每人自己的新增资源目录与配套.meta；先核对统一风格与许可，再由主导者接入主场景 |
| 独立Prefab/试验场景 | 可各自新增，不直接覆盖主场景；Unity导入生成.meta后一起提交 |
| 试玩记录/验收材料 | 可独立记录版本和问题；不要把拟分工写成已完成 |

开始前推荐先分一个独立小目标：主导者继续主流程；霍利屹可选一项新角色资源或独立表现组件；宋方壹可选一项独立反馈功能/测试与试玩问题。这里只是建议，具体目标在Issue里确认，不要求两位立刻各写一套大系统。

## 5. 冲突处理

统一按[CONTRIBUTING第5节](../CONTRIBUTING.md)处理，保留双方有效修改；Unity共享资源交唯一写入者协调，解决后重新验证。不要擅自选一方整文件覆盖。

## 6. main保护和检查范围

协作配置目标：普通成员通过PR合入；1个审批且由CODEOWNERS指定的Peanora审查、讨论已解决、`repository-checks`通过并基于最新main；禁止强推/删除main，使用线性提交历史。main管理员保留人工应急能力，Agent不得使用该能力绕过保护或自动合并；人工应急由负责人决定并记录原因和验证。主导者自己的提交仍应检查通过并完成本机验证。

自动检查只读Git跟踪文件，检查缺失/孤立.meta、重复GUID、大小写路径冲突、未解决合并标记、误交缓存/本地AGENTS.md、精确Unity版本、Force Text/Visible Meta Files以及包JSON。入口`python tools/check_repository.py`；GitHub Actions自带Python执行，无需Unity许可或上传账号密钥。它不检查游戏逻辑正确性，不代替Unity编译、EditMode/PlayMode和实机试玩。

## 7. 谁上传试玩包

2026-10-20用户最新约定：只有用户明确要求时才制作、更新或上传试玩ZIP；普通开发默认只提交源码和验证记录，必要的本地独立Player验证仍可执行。用户要求发包时，由主导者在main集成并验证后统一打包、上传同仓库Releases，记录对应源码版本、操作说明、实际测试与限制。成员只提交源码/资源，不往Git仓库塞exe，也不分别发布互相冲突的“最新版”。当前未配置云端Unity编译/自动发包；发布由本机Unity验证和构建后执行，不需要成员提供Unity/GitHub令牌。

本次协作配置只改变开发流程，不改变玩法，因此沿用已有试玩版，不额外发布内容相同的包。第二台电脑的首次克隆、Unity导入、Play和离线包启动仍须两位实际执行，结果填入Issue。

参考：[Desktop创建PR](https://docs.github.com/en/desktop/working-with-your-remote-repository-on-github-or-github-enterprise/creating-an-issue-or-pull-request-from-github-desktop)、[保护分支](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)。GitHub UI语言可能不同，按同义按钮操作。
