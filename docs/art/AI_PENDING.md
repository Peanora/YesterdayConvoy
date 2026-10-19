# AI 缺口与可复制提示词

## 最新风格与缺口（2026-10-19）

维什戴尔已有用户提供的走路/静态待机/普攻/技能GIF，本轮不需要AI重画或补帧。之后所有原创角色/卡面/特效以ART_STYLE_GUIDE最新粗黑轮廓、平涂、夸张漫画Q版为基准；下方旧非像素暗色幻想/像素AI提示词仅为历史，不可直接复用。克洛丝同风格动作、死亡/部署专属动作和整套地图/UI统一仍是缺口，本轮未生成或承诺批量完成。

2026-10-18 地图更新：地图方向已固定为《明日方舟》相近的正向斜俯视工业战术场景。本轮已找到并接入满足基础搭建的 CC0 模型/材质，**本轮没有 AI 地图生成任务**。下文旧“破败王都/城墙背景”候选不再作为地图缺口；若后续确需独有景物，仍必须服从 GDD 第 9 节，不可借 AI 自行更换题材或生成带文字的整张游戏界面。

2026-10-17 更新：用户之后明确选择同人卡面并授权生成系列实机美术，已实际生成并接入六张图（含此前认可卡面），见 GENERATION-2026-10-17.md。下文“没有生成”是首次调查的历史记录；石壳近战/灯笼法系概念没有被批准或制作，不作为当前角色选择。

当前没有生成或导入 AI 图片。环境具备实际图像生成工具，但本轮优先 Phase 1–4；风格尚未锁定，不批量生成立绘。以下是**非像素主路线的条件性候选**，不是已完成资源。若选 0x72 像素路线，先复用其角色，取消不必要的 AI 战斗图，卡面是否采用像素展示另定。

目前完整免费非像素角色系列未覆盖近战/远程/肉盾/高速/法系/辅助与精英/首领。先验证近战与法系两种，之后才扩充。已有骑士/僵尸等原包候选能满足外形时优先使用，不因下表就跳过现成资源。AI 条款需在真正调用时记录平台、日期、输出文件和当时条款；本轮无 AI 使用许可被冒称已核实。

## 两角色首轮

目标 1：原创石壳近战怪，块状前肢、低重心、冷灰石甲与小面积青绿核心；目标 2：原创灯笼法系怪，纤细高轮廓、斗篷/灯笼、同样青绿核心。身份为美术概念，不新增种族、技能或正式名称。

卡面中文模板（两种各一张）：

> 原创暗色幻想怪物卡牌立绘，全身，{角色外形描述}，轮廓清晰，简化绘制，低饱和冷灰与克制的青绿色能量，高级但简洁的游戏概念美术，3:4 竖构图，背景简洁，不带任何文字、数值、卡框、UI、水印，不模仿现有商业游戏角色。

Card illustration English:

> Original dark fantasy monster, full body, {character description}, clear distinctive silhouette, restrained cool stone-gray palette with small teal energy accents, simplified non-pixel painted game art, readable anatomy, vertical 3:4 composition, subdued uncluttered background, no text, no numbers, no card border, no interface, no watermark, no existing game character.

战斗 Sprite 中文模板：

> 原创 2.5D 斜俯视战场用怪物，{角色外形描述}，直立展示的简化非像素角色，全身、脚底完整、轻微三分之四视角、面向画面右侧，冷灰和克制青绿配色，轮廓在小尺寸清晰，统一中性光照，透明背景，无地面、无投影、无场景、无界面、无文字、无水印。一张中性姿态，不要精灵表。

Battle sprite English:

> Original simplified non-pixel monster sprite for an oblique 2.5D tactical battlefield, {character description}, full body with complete feet, upright character in a mild three-quarter view facing screen-right, clear small-scale silhouette, cool gray palette with restrained teal accents, consistent neutral lighting, transparent background, no ground, no shadow, no scenery, no interface, no text, no watermark, one neutral pose, not a sprite sheet.

目标输出：卡面 768×1024（允许更大原图等比处理）；Sprite 可先 512×512，透明 PNG，脚底中心和统一画布，再按可见高度配置世界尺寸；首轮不生成动画表。Alpha 必须读取通道验证，不能因提示词写 transparent 就勾完成。

## 后续条件性缺口

精英/首领统一模板：沿用上面提示词，替换为“同系更大轮廓、独特头冠或背部造型、明显攻击前摇可用的姿态”；卡面 768×1024，战斗首领原图更高质量后等比缩放。另需技能预警和边框，由 Unity 控件/粒子实现，不能整张 UI 用 AI 生成。

背景若现成地图与实际镜头测试仍无法适配：只生成无文字的远景破败城墙氛围图，不生成要求精确拼接的 Tilemap。英文附加词：`distant ruined royal city walls, desaturated blue-gray haze, subdued contrast, empty tactical foreground, no characters, no interface`。预计 1920×1080；仅用作远景，不覆盖通行路径或蓝门提示。
