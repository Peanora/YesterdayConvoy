# 《昨日圆车》贡献与 Git / PR 流程

2026-10-20 最新试玩包约定：普通开发默认只提交源码、资源与验证记录；只有用户明确要求时才创建、更新或上传 Windows ZIP / Releases。必要的本地独立Player构建验证可以执行，但不得自动压缩或覆盖已有试玩ZIP。此约定覆盖本文旧的默认同步试玩包表述。

适用于三名开发者各自使用 Codex 的日常协作。游戏规则以 `docs/GDD.md` 为准，工程现状见 `docs/STATUS.md`；成员、环境配置与文件负责人见 [协作指南](docs/COLLABORATION.md)。Agent 自动执行边界放在各自本机根目录 `AGENTS.md`，本文件集中说明人工操作流程。

## 1. 首次准备与认领

每人使用自己的 GitHub 账号和独立工程目录；Unity 精确版本保持 `6000.6.5f1`。先按协作指南完成克隆和首次打开，不共享账号或互相覆盖整个工程。

`AGENTS.md` 继续仅在本地保存，已被 Git 忽略，不加入提交或试玩包。每名开发者首次让 Codex 工作前，应在自己的工程根目录创建该文件，至少写入以下引导；已有完整指令时合并，不覆盖原内容：

```markdown
# 本地 Agent 指令

开始任何任务前，读取并遵守根目录 CONTRIBUTING.md 的全部协作规则、docs/COLLABORATION.md 的文件责任、docs/STATUS.md 及 docs/GDD.md 相关章节。新功能由 Agent 自动检查 Git 状态、同步最新 main、创建独立 feature 分支，完成验证后自动提交、推送并创建 PR。发现既存未提交修改或冲突必须先报告，不得覆盖。不得直接在 main 开发，不得自动合并 PR；本地补充不能放宽共享协作规则。
```

这样其他开发者克隆仓库后也能加载共享流程，不依赖收到主导者本机完整 AGENTS。`docs/AGENTS.template.md` 是历史建项模板，不能覆盖现行规则。2026-10-20 用户已统一授权三名开发者的 Agent 自动完成新功能的分支提交、正常推送与 PR 创建，无需逐轮确认；危险操作不在授权范围内。试玩发布仍由主导者统一执行。

开工前在任务 Issue 中记录一个验收目标、负责人、所属模块、预计修改文件及测试入口，由主导者确认规则与范围。先读相关代码、架构和调用关系，优先复用已有实现。不得擅改他人模块、删除已有功能或做无关重构。公共接口、主场景、Prefab、Packages、ProjectSettings 的修改须先说明影响，并获得用户 / 对应负责人确认；已明确授权的具体范围无需重复确认。场景、Prefab、共享配置与包清单先约定唯一写入者，再开始编辑。

## 2. 检查工作区，从最新 main 开分支

先检查分支与三类修改，GitHub Desktop 中也要逐项查看 Changes 和当前分支：

```powershell
git status --short --branch
git branch --show-current
git diff --stat
git diff --cached --stat
```

开工检查或同步 / 切分支时存在既存未提交修改（含已暂存、未暂存、未跟踪文件）或冲突，Agent 必须先报告文件、分支及影响，暂停同步、切分支和受影响修改，确认归属与处理方案后继续。仅由原作者处理自己的工作；不要替他人覆盖、丢弃、暂存、提交或 stash，也不要为拉取 / 切分支而先清空工作区。不要顺手提交 Unity 自动生成的无关文件。当前任务自行产生的改动在交付时报告并核对后正常提交，不混入既存修改。

所有新功能均由 Agent 自动执行检查、同步 main 和创建独立 feature 分支，再开始实现；下列步骤也供人工复核或排障。确认工作区干净、Unity 已保存关闭后，在 Desktop 切到 main，Fetch origin → Pull origin，再创建功能分支。命令行等价步骤如下，每行成功后再执行下一行：

```powershell
git fetch origin
git switch main
git log --oneline origin/main..main
git pull --ff-only origin main
git switch -c codex/task-name
```

`git log --oneline origin/main..main` 应无输出；如有输出，表示 main 有本地独有提交，先报告并停止后续步骤。`task-name` 换成当前任务名；`codex/` 是 Agent 的 feature 分支前缀，手工分支也可沿用 `feat/huo/skill-icon`、`feat/song/deployment-feedback`。每人每任务独立分支，禁止直接在 main 开发或提交。拉取失败、网络不可用或出现冲突时先报告并查清，不用 reset 强行对齐，也不声称基于最新 main。

原目录有他人修改而需独立开工时，必须先报告并明确隔离方案，再执行只更新远端引用的 `git fetch origin`，由主导者协调独立目录，例如 `git worktree add -b codex/task-name ../YesterdayConvoy-task origin/main`；不要移动原目录的修改。worktree 仍须初始化本地 AGENTS，并各用独立 Unity 实例 / 工程目录。

已有任务继续使用原功能分支。需要吸收 main 更新时，保存并关闭 Unity，在干净功能分支执行 `git fetch origin` 后 `git merge origin/main`；不要为更新远端 PR 擅自 rebase 已共享提交或强推。存在冲突时按第 5 节处理。

## 3. 验证并提交自己的改动

完成当前验收目标后，执行可行且相关的检查：仓库静态检查入口为 `python tools/check_repository.py`；运行时变更按风险执行 Unity 编译、配置 / 场景校验、Edit Mode / Play Mode 测试与实际交互，重大流程变更再做独立 Windows 构建。静态检查不证明 Unity 编译或玩法正确。入口与已有证据见 `docs/QA.md`，没有执行的项目写“未运行”。纯文档修改检查内容、链接、差异和规则一致性即可。

保存并关闭 Unity，在 Desktop Changes 中逐项选择自己的任务文件，或用 `git add -- <明确文件路径>` 指定文件。场景、Prefab、资源及必要 `.meta` 一起纳入；保留 GUID。不要使用 `git add .` / `git add -A` 混入他人改动；不提交 Library、Temp、Logs、Builds、账号文件或 AGENTS.md。

提交前检查 `git diff --cached`，确认暂存区没有其他人的工作。若发现他人已暂存内容，先协调，不擅自取消暂存或一起提交。确认当前分支为本任务功能分支后提交，每完成一个验证过的可运行功能就作本地提交；提交说明描述具体行为。

交付说明列出修改文件、实际测试结果、未测项 / 潜在问题，并简述关键类、接口和数据连接；按任务需要更新 STATUS / QA，不把文档核对记成编译、构建或试玩。

## 4. Agent 自动提交、Push 与创建 PR，人工合并

1. 新功能完成实现与相应验证后，由 Agent 按第3节在功能分支提交自己的任务文件，并自动 Push origin / Publish branch，或执行 `git push -u origin <本任务分支>`。本规则已提供持续授权，无需再次询问；当前任务明确禁止推送时不执行。
2. Agent 自动创建 Pull Request，base 选择 `main`，compare 选择自己的功能分支；已有 PR 则更新原 PR。按现有 PR 模板填写目标、文件范围、负责人确认、实际验证、关键连接和风险，关联任务 Issue。报告提交、分支和 PR 链接；任一步失败时报告实际状态，不称为已完成推送或 PR 交付。
3. 查看 `repository-checks`，失败时先处理第一条错误，在同一分支提交并正常 Push 更新原 PR。所需 Unity / 实机验证仍由本机完成，Actions 通过不能替代这些验证。
4. 由 Peanora 人工查看差异、协调共享文件，并在集成工程检查编译、引用与相关测试。满足仓库要求的审批、代码负责人审查、讨论解决、必需检查和最新 main 要求后，由人工执行 **Squash and merge**。提交者不能以自己的批准代替他人审查。
5. Agent 不得自动合并到 main，也不得使用管理员绕过保护。现有管理员应急能力不是 Agent 授权；确有人工应急需求时，由负责人决定并记录原因和验证。
6. 合并后保存并关闭 Unity，在干净工作区回 main → Fetch / Pull，再从最新 main 开下个任务分支，不继续向已合并分支提交。分支清理由原作者 / 负责人处理，Agent 不擅删他人分支。

主导者此前取得的持续源码同步授权按本流程作用于功能分支，不能解释为直接提交 main 或自动合并授权。试玩发布仅在用户明确要求、人工集成后的版本通过相应验证后，由主导者统一打包并上传既有仓库 Releases；普通开发不自动更新ZIP。

## 5. 合并冲突与危险操作

遇到冲突先查看 `git status`，必须先向用户报告冲突文件、双方分支及影响，再联系相关作者协调修改意图。不得擅自丢弃任意一方的修改；不要通过整文件 Accept All / ours / theirs、删除文件或 `.meta` 来消除冲突。

普通代码由双方核对兼容方案，保留双方有效意图；公共接口变化重新确认调用方影响。`.unity`、`.prefab`、共享 `.asset`、`.meta` 及包 / 项目设置冲突交唯一写入者协调处理，更新期间关闭 Unity。不能确认正确合并方案时暂停受影响文件，继续不依赖它的工作。解决后清除冲突标记，复查双方差异、GUID / 场景引用并运行相关编译与测试，再在功能分支提交。

普通 Push / PR 授权不包括危险操作。未经明确授权，禁止强制推送（包括 `--force-with-lease`）、重写 Git 历史、删除他人分支；不以 `reset --hard`、`clean -fd` 或整套旧 Assets 覆盖解决问题。需恢复时先与作者确认方案，不替别人清理工作区。
