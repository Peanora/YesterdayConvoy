"""Package actual FormationBattle captures into an offline review gallery.

Usage: python tools/package_formation_showcase.py <capture-directory> [--concat]
Only Python's standard library and the installed ffmpeg/ffprobe are required.
The source manifest and individual captures are never modified or removed.
"""
from __future__ import annotations

import argparse
from fractions import Fraction
import json
import math
from pathlib import Path
import shutil
import subprocess
import sys


KINDS = {"monster", "synergy", "variant"}


def finite_number(value, label, *, positive=False):
    if isinstance(value, bool):
        raise ValueError(f"{label} must be a number")
    try:
        result = float(value)
    except (TypeError, ValueError) as error:
        raise ValueError(f"{label} must be a number") from error
    if not math.isfinite(result) or result < 0 or (positive and result <= 0):
        raise ValueError(f"{label} must be finite and {'positive' if positive else 'nonnegative'}")
    return result


def clip_path(directory, relative):
    if not isinstance(relative, str) or not relative or "\n" in relative or "\r" in relative:
        raise ValueError("clip.file must be a nonempty relative path without newlines")
    candidate = Path(relative)
    if candidate.is_absolute() or ":" in relative or "\\" in relative:
        raise ValueError(f"clip.file must use a relative path with forward slashes: {relative!r}")
    resolved = (directory / candidate).resolve()
    try:
        resolved.relative_to(directory)
    except ValueError as error:
        raise ValueError(f"clip.file escapes the capture directory: {relative!r}") from error
    return resolved


def executable(name, supplied=None, companion=None):
    if supplied:
        found = shutil.which(supplied)
        if found:
            return found
        path = Path(supplied).expanduser()
        if path.is_file():
            return str(path.resolve())
        raise ValueError(f"Cannot find {name}: {supplied}")
    found = shutil.which(name)
    if found:
        return found
    if companion:
        path = Path(companion).with_name(name + (".exe" if sys.platform == "win32" else ""))
        if path.is_file():
            return str(path)
    raise ValueError(f"Cannot find {name}; install it or pass --{name} with its executable path")


def run(command):
    result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                            encoding="utf-8", errors="replace", check=False)
    if result.returncode:
        raise RuntimeError(f"{Path(command[0]).name} exited {result.returncode}:\n{result.stderr[-6000:]}")
    return result.stdout


def probe(ffprobe, path):
    data = json.loads(run([ffprobe, "-v", "error", "-show_streams", "-show_format",
                           "-of", "json", str(path)]))
    videos = [stream for stream in data.get("streams", []) if stream.get("codec_type") == "video"]
    if len(videos) != 1:
        raise ValueError(f"Expected one video stream, got {len(videos)}: {path.name}")
    stream = videos[0]
    rate = stream.get("avg_frame_rate", "0/1")
    try:
        fps = float(Fraction(rate))
    except (ValueError, ZeroDivisionError) as error:
        raise ValueError(f"Invalid video frame rate {rate}: {path.name}") from error
    duration = finite_number(stream.get("duration", data.get("format", {}).get("duration")),
                             f"{path.name} duration", positive=True)
    frames = stream.get("nb_frames")
    return {
        "codec": stream.get("codec_name"), "width": stream.get("width"),
        "height": stream.get("height"), "fps": fps, "duration": duration,
        "frames": int(frames) if frames and str(frames).isdigit() else None,
        "audioStreams": sum(s.get("codec_type") == "audio" for s in data.get("streams", [])),
        "bytes": path.stat().st_size,
    }


def load_manifest(directory):
    data = json.loads((directory / "manifest.json").read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict) or not isinstance(data.get("clips"), list) or not data["clips"]:
        raise ValueError("manifest.json must contain a nonempty clips array")
    for key in ("width", "height"):
        value = finite_number(data.get(key), key, positive=True)
        if not value.is_integer():
            raise ValueError(f"{key} must be an integer")
        data[key] = int(value)
    data["fps"] = finite_number(data.get("fps"), "fps", positive=True)
    if data.get("audio") is not False:
        raise ValueError("This silent capture package expects manifest.audio=false")
    seen_ids, seen_files = set(), set()
    for index, clip in enumerate(data["clips"]):
        if not isinstance(clip, dict):
            raise ValueError(f"clips[{index}] must be an object")
        if not isinstance(clip.get("id"), str) or not clip["id"] or clip["id"] in seen_ids:
            raise ValueError(f"Missing or duplicate clip id at clips[{index}]")
        seen_ids.add(clip["id"])
        if clip.get("kind") not in KINDS:
            raise ValueError(f"Unknown clip kind for {clip['id']}: {clip.get('kind')!r}")
        if not isinstance(clip.get("name"), str) or not clip["name"]:
            raise ValueError(f"Missing name for {clip['id']}")
        path = clip_path(directory, clip.get("file"))
        if str(path).casefold() in seen_files:
            raise ValueError(f"Duplicate clip file: {clip['file']}")
        seen_files.add(str(path).casefold())
        for stage in clip.get("stages", []):
            if not isinstance(stage, dict) or not isinstance(stage.get("label"), str):
                raise ValueError(f"Malformed stage for {clip['id']}")
            finite_number(stage.get("start"), f"{clip['id']} stage.start")
            finite_number(stage.get("duration", 0), f"{clip['id']} stage.duration")
    return data


def validate_clips(directory, data, ffprobe):
    records, errors = [], []
    for clip in data["clips"]:
        record = {"id": clip["id"], "file": clip["file"], "passed": False}
        try:
            if clip.get("error"):
                raise ValueError(f"Capture reported: {clip['error']}")
            path = clip_path(directory, clip["file"])
            if not path.is_file():
                raise ValueError(f"Missing video: {clip['file']}")
            if path.stat().st_size == 0:
                raise ValueError(f"Empty video: {clip['file']}")
            actual = probe(ffprobe, path)
            record.update(actual)
            if actual["codec"] != "h264":
                raise ValueError(f"Expected H264, got {actual['codec']}")
            if (actual["width"], actual["height"]) != (data["width"], data["height"]):
                raise ValueError(f"Video dimensions {actual['width']}x{actual['height']} differ from manifest")
            if abs(actual["fps"] - data["fps"]) > .001:
                raise ValueError(f"Video FPS {actual['fps']} differs from manifest {data['fps']}")
            if actual["audioStreams"]:
                raise ValueError("Video has audio streams but manifest declares silent captures")
            expected_duration = clip.get("duration")
            if expected_duration is not None:
                expected_duration = finite_number(expected_duration, "clip.duration", positive=True)
                if abs(actual["duration"] - expected_duration) > max(.15, 2 / data["fps"]):
                    raise ValueError(f"Duration {actual['duration']:.3f}s differs from manifest {expected_duration:.3f}s")
            expected_frames = clip.get("frames")
            if expected_frames is not None and actual["frames"] is not None:
                if int(expected_frames) != actual["frames"]:
                    raise ValueError(f"Video has {actual['frames']} frames, manifest says {expected_frames}")
            if any(float(stage["start"]) > actual["duration"] + 1 / data["fps"]
                   for stage in clip.get("stages", [])):
                raise ValueError("A stage starts after the video ends")
            record["passed"] = True
        except (ValueError, RuntimeError, OSError) as error:
            record["error"] = str(error)
            errors.append(f"{clip['id']} {clip['name']}: {error}")
        records.append(record)
    return records, errors


def metadata_escape(value):
    text = " ".join(str(value).splitlines())
    for char in ("\\", "=", ";", "#"):
        text = text.replace(char, "\\" + char)
    return text


def concat_quote(path):
    # FFmpeg concat demuxer single-quote escaping, not shell escaping.
    return "'" + path.as_posix().replace("'", "'\\''") + "'"


def concatenate(directory, data, records, ffmpeg, ffprobe):
    by_id = {record["id"]: record for record in records}
    work = directory / "chapters"
    work.mkdir(exist_ok=True)
    collections = []
    groups = [
        ("monsters", "00-全部怪物.mp4", "全部怪物 · 战斗与技能",
         lambda clip: clip["kind"] in {"monster", "variant"} and not clip["id"].endswith("-walk")),
        ("synergies", "01-羁绊联动.mp4", "羁绊与联动", lambda clip: clip["kind"] == "synergy"),
        ("walks", "02-逐人步态.mp4", "逐人步态 · 源素材与程序位移",
         lambda clip: clip["id"].endswith("-walk")),
    ]
    for key, filename, title, include in groups:
        clips = [clip for clip in data["clips"] if include(clip)]
        if not clips:
            continue
        listing = work / (key + ".concat.txt")
        listing.write_text("ffconcat version 1.0\n" + "".join(
            "file " + concat_quote(clip_path(directory, clip["file"])) + "\n" for clip in clips),
            encoding="utf-8")
        lines = [";FFMETADATA1", "title=" + metadata_escape(title)]
        elapsed = 0.0
        chapters = []
        for clip in clips:
            duration = by_id[clip["id"]]["duration"]
            start = round(elapsed * 1000)
            elapsed += duration
            end = round(elapsed * 1000)
            chapter_title = f"{clip['id']} · {clip['name']}"
            lines.extend(["[CHAPTER]", "TIMEBASE=1/1000", f"START={start}", f"END={end}",
                          "title=" + metadata_escape(chapter_title)])
            chapters.append({"id": clip["id"], "name": clip["name"], "start": start / 1000,
                             "duration": (end - start) / 1000})
        metadata = work / (key + ".ffmetadata")
        metadata.write_text("\n".join(lines) + "\n", encoding="utf-8")
        output = directory / filename
        partial = directory / (output.stem + ".partial.mp4")
        run([ffmpeg, "-hide_banner", "-v", "error", "-y", "-f", "concat", "-safe", "0",
             "-i", str(listing), "-f", "ffmetadata", "-i", str(metadata), "-map", "0:v:0",
             "-c", "copy", "-an", "-map_metadata", "1", "-map_chapters", "1",
             "-movflags", "+faststart", str(partial)])
        actual = probe(ffprobe, partial)
        if abs(actual["duration"] - elapsed) > max(.25, len(clips) / data["fps"]):
            raise ValueError(f"Combined video duration is inconsistent: {filename}")
        partial.replace(output)
        collections.append({"file": filename, "title": title, "clips": len(clips),
                            "chapters": chapters, **actual})
    return collections


PAGE = r'''<!doctype html>
<html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>昨日圆车 · 棋盘动作审片</title>
<style>
:root{color-scheme:dark;--bg:#10171b;--panel:#1a252b;--rule:#34434b;--text:#ecf0f0;--dim:#a8b8c0;--accent:#e5c474;--mint:#89d6cf}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.6 "Microsoft YaHei UI","Microsoft YaHei","PingFang SC",sans-serif}
button,input,select{font:inherit}button,a,input,select{touch-action:manipulation}button{cursor:pointer}button:disabled{cursor:default;opacity:.4}button:focus-visible,a:focus-visible,input:focus-visible,select:focus-visible,video:focus-visible{outline:2px solid var(--accent);outline-offset:3px}
a{color:var(--mint);text-underline-offset:4px}header{padding:24px 30px 18px;border-bottom:1px solid var(--rule);display:flex;gap:20px;align-items:flex-end;justify-content:space-between}h1{margin:0;font-size:25px;line-height:1.3;letter-spacing:.04em;font-weight:650}.subtitle{color:var(--dim);font-size:13px;margin:9px 0 0;max-width:75ch}.build{color:var(--dim);font:12px/1.7 Consolas,monospace;white-space:nowrap;text-align:right}.build strong{color:var(--mint);font-weight:400}.layout{display:grid;grid-template-columns:minmax(0,1fr) 318px;gap:24px;max-width:1680px;margin:auto;padding:24px 30px 32px}.review{min-width:0}video{display:block;width:100%;aspect-ratio:16/9;background:#050708;border:1px solid var(--rule)}.current{display:flex;justify-content:space-between;gap:20px;align-items:flex-start;padding-top:17px}h2{font-size:22px;line-height:1.4;margin:0;font-weight:600}.label{color:var(--accent);font-size:12px;display:block;margin-bottom:4px}.duration{color:var(--dim);font-size:13px;white-space:nowrap}.stagebar{display:flex;gap:7px;flex-wrap:wrap;margin:17px 0 12px}.stagebar button,.controls button,.controls select{border:1px solid var(--rule);background:var(--panel);color:var(--text);padding:6px 12px;min-height:37px}.stagebar button:hover,.controls button:hover{border-color:var(--accent)}.stagebar button small{font:11px Consolas,monospace;color:var(--dim);margin-right:7px}.controls{display:flex;gap:8px;align-items:center;flex-wrap:wrap;padding:11px 0 17px;border-bottom:1px solid var(--rule)}.controls .next{background:var(--accent);color:#192228;border-color:var(--accent);margin-left:auto}.auto{color:var(--dim);font-size:12px;display:flex;align-items:center;gap:5px;margin-left:4px}.notes{margin:17px 0;color:var(--text);font-size:14px;line-height:1.9;white-space:pre-wrap;max-width:85ch}.facts{display:flex;flex-wrap:wrap;gap:8px 24px;color:var(--dim);font-size:12px;margin:14px 0}.tags{font-size:13px;color:var(--mint);margin:12px 0;max-width:85ch}.scope{border-top:1px solid var(--rule);padding-top:14px;margin-top:22px;color:var(--dim);font-size:12px;max-width:90ch}.scope p{margin:0 0 8px}.scope summary{cursor:pointer;color:var(--mint)}.scope code{word-break:break-all}.library{border-left:1px solid var(--rule);padding-left:22px;min-width:0}.library h2{font-size:15px;display:flex;justify-content:space-between;align-items:center;margin-bottom:13px}.library h2 span{font:12px Consolas,monospace;color:var(--dim)}input[type=search]{width:100%;background:#111b20;color:var(--text);padding:9px 11px;border:1px solid var(--rule);border-radius:0}.filters{display:flex;gap:4px;border-bottom:1px solid var(--rule);padding:13px 0 10px}.filters button{padding:4px 10px;background:none;border:0;color:var(--dim);font-size:13px}.filters button[aria-pressed=true]{background:var(--accent);color:#192228}.cliplist{max-height:69vh;overflow:auto;scrollbar-color:#596a72 #152027}.clip{width:100%;text-align:left;background:none;color:var(--text);border:0;border-bottom:1px solid #2a383f;padding:13px 10px;border-left:3px solid transparent;display:block}.clip:hover{background:#202d34}.clip[aria-current=true]{border-left-color:var(--accent);background:#28363d}.clip .id{font:11px Consolas,monospace;color:var(--dim);display:block;margin-bottom:3px}.clip .name{font-size:14px;font-weight:600}.clip .hint{display:block;color:var(--dim);font-size:11px;line-height:1.5;margin-top:5px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}.clip .bad{color:#eda48f}.empty{color:var(--dim);font-size:13px;padding:24px 10px}.collections{display:grid;gap:9px;border-top:1px solid var(--rule);margin-top:20px;padding-top:16px}.collections a{font-size:13px}.collections small{color:var(--dim);text-decoration:none;font-size:11px;margin-left:8px}.error{color:#eda48f;white-space:pre-wrap;font-size:13px;line-height:1.8}.footer{font-size:11px;color:var(--dim);padding:18px 30px;border-top:1px solid var(--rule)}
@media(min-width:1500px){header,.layout,.footer{padding-left:40px;padding-right:40px}.layout{gap:30px;grid-template-columns:minmax(0,1fr) 345px}}
@media(max-width:960px){header{padding:20px;align-items:flex-start}.build{display:none}.layout{grid-template-columns:1fr;padding:20px;gap:28px}.library{border-left:0;border-top:1px solid var(--rule);padding:20px 0 0}.cliplist{max-height:440px}.footer{padding:16px 20px}}
@media(max-width:520px){h1{font-size:21px}h2{font-size:19px}.layout{padding:14px}.controls{gap:6px}.controls button,.controls select{font-size:12px;padding:6px 8px}.current{gap:10px}.duration{font-size:11px}.auto{margin-left:0}.controls .next{margin-left:0}}
</style></head><body>
<header><div><h1>昨日圆车 · 棋盘动作审片</h1><p class="subtitle">逐个查看实际战斗、普攻、技能与羁绊联动，或单独查看源步态。点击右侧角色切换，下方可跳分段；暂停后可逐帧看动作。</p></div><div class="build" id="build"></div></header>
<main class="layout"><section class="review" aria-label="演示视频"><video id="player" controls playsinline preload="metadata" aria-label="当前演示视频"></video><div class="current"><div><span class="label" id="kind"></span><h2 id="title"></h2></div><div class="duration" id="duration"></div></div><div id="error" class="error" role="status"></div><div class="stagebar" id="stages" aria-label="视频分段"></div><div class="controls"><button id="prev" type="button">上一段</button><button id="backframe" type="button">上一帧</button><button id="nextframe" type="button">下一帧</button><select id="rate" aria-label="播放速度"><option value="0.5">0.5× 慢放</option><option value="1" selected>1× 原速</option><option value="1.5">1.5×</option><option value="2">2×</option></select><label class="auto"><input type="checkbox" id="autonext">播完切下一段</label><button class="next" id="next" type="button">下一段 →</button></div><div class="facts" id="facts"></div><p class="tags" id="tags"></p><p class="notes" id="notes"></p><div class="scope"><p>这些视频录自当前棋盘的实机画面，使用预设展示夹具，且无音轨。它们用于定位现有动作和特效问题，不代表自然对局，也不代表最终美术已确认。「步态」分组使用源步态素材配合棋盘程序位移，与实际战斗AI分开查看。</p><details><summary>查看录制范围与版本</summary><p id="scope"></p><p>内容源码：<code id="commit"></code></p></details></div></section><aside class="library" aria-label="全部演示目录"><h2>演示目录 <span id="count"></span></h2><input id="search" type="search" placeholder="搜索角色、羁绊或问题" aria-label="搜索演示"><div class="filters" role="group" aria-label="筛选演示类型"><button type="button" data-filter="all" aria-pressed="true">全部</button><button type="button" data-filter="monster" aria-pressed="false">怪物</button><button type="button" data-filter="synergy" aria-pressed="false">羁绊</button><button type="button" data-filter="walk" aria-pressed="false">步态</button></div><nav class="cliplist" id="list" aria-label="演示选择"></nav><div id="collections" class="collections" hidden></div></aside></main><footer class="footer">本页可离线打开。反馈时写下角色名、视频时间与现象，例如「走路倒滑」「出手太快」「命中特效挡住身体」。</footer>
<script id="manifest" type="application/json">__MANIFEST__</script>
<script>
'use strict';
const data=JSON.parse(document.getElementById('manifest').textContent), clips=data.clips;
const byId=id=>document.getElementById(id), player=byId('player');
const mediaPath=value=>String(value).split('/').map(encodeURIComponent).join('/');
let current=0, filter='all', visible=[];
const isWalk=clip=>String(clip.id).endsWith('-walk');
const kindName={monster:'怪物 · 实际战斗与技能',variant:'角色变体 · 技能或形态',synergy:'羁绊 · 联动展示'};
const formatTime=value=>{const n=Math.max(0,Number(value)||0);return Math.floor(n/60)+':'+String(Math.floor(n%60)).padStart(2,'0');};
const listText=value=>Array.isArray(value)?value.join('、'):value==null?'':String(value);
const text=(id,value)=>{byId(id).textContent=value==null?'':String(value);};
function filtered(){const q=byId('search').value.trim().toLocaleLowerCase();return clips.map((clip,index)=>({clip,index})).filter(({clip})=>(filter==='all'||(filter==='walk'?isWalk(clip):filter==='monster'?clip.kind!=='synergy'&&!isWalk(clip):clip.kind==='synergy'))&&(!q||[clip.id,clip.name,listText(clip.units),listText(clip.activeSynergies),clip.notes,clip.error].join(' ').toLocaleLowerCase().includes(q)));}
function renderList(){visible=filtered();const list=byId('list');list.replaceChildren();text('count',visible.length+' / '+clips.length);if(!visible.length){const p=document.createElement('p');p.className='empty';p.textContent='没有匹配项。可换一个角色名或清空搜索。';list.append(p);}for(const {clip,index}of visible){const button=document.createElement('button');button.type='button';button.className='clip';button.setAttribute('aria-current',String(index===current));button.dataset.index=index;const id=document.createElement('span');id.className='id';id.textContent=clip.id+' · '+(isWalk(clip)?'步态':clip.kind==='synergy'?'羁绊':clip.kind==='variant'?'变体':'怪物');const name=document.createElement('span');name.className='name';name.textContent=clip.name;const hint=document.createElement('span');hint.className=clip.error?'hint bad':'hint';hint.textContent=clip.error?'录制或文件校验未通过':clip.notes||listText(clip.units)||'';button.append(id,name,hint);button.addEventListener('click',()=>select(index,true));list.append(button);}updateNavigation();}
function updateNavigation(){const at=visible.findIndex(entry=>entry.index===current);byId('prev').disabled=at<=0;byId('next').disabled=at<0||at>=visible.length-1;}
function navigate(offset,play=false){const at=visible.findIndex(entry=>entry.index===current), target=visible[at+offset];if(target)select(target.index,play);}
function select(index,play=false){current=index;const clip=clips[index];player.pause();player.removeAttribute('src');player.load();text('kind',isWalk(clip)?'步态素材 · 棋盘程序位移':kindName[clip.kind]||clip.kind);text('title',clip.name);text('duration',clip.duration?formatTime(clip.duration)+' · '+data.width+'×'+data.height:'');text('notes',isWalk(clip)?'步态素材段：使用当前源步态与棋盘程序位移，不代表自然战斗AI。\n'+(clip.notes||''):clip.notes||'此段未附额外说明。');text('tags',clip.activeSynergies&&clip.activeSynergies.length?'本段已激活：'+listText(clip.activeSynergies):'');text('error',clip.error?'本段尚未完整生成或校验失败：'+clip.error:'');const facts=byId('facts');facts.replaceChildren();for(const value of [clip.units&&clip.units.length?'成员 '+listText(clip.units):'',Number.isFinite(clip.skillCasts)?'实际施法 '+clip.skillCasts+' 次':'',Number.isFinite(clip.hits)?'实际命中 '+clip.hits+' 次':'']){if(value){const span=document.createElement('span');span.textContent=value;facts.append(span);}}const stages=byId('stages');stages.replaceChildren();for(const stage of clip.stages||[]){const button=document.createElement('button');button.type='button';const time=document.createElement('small');time.textContent=formatTime(stage.start);button.append(time,document.createTextNode(stage.label));button.disabled=Boolean(clip.error);button.addEventListener('click',()=>{player.currentTime=Number(stage.start)||0;player.play().catch(()=>{});});stages.append(button);}if(!clip.error){player.src=mediaPath(clip.file);player.load();player.playbackRate=Number(byId('rate').value);if(play)player.play().catch(()=>{});}renderList();}
function step(direction){if(!player.src)return;player.pause();const max=Number.isFinite(player.duration)?Math.max(0,player.duration-1/data.fps):Infinity;player.currentTime=Math.min(max,Math.max(0,player.currentTime+direction/data.fps));}
byId('search').addEventListener('input',renderList);document.querySelectorAll('[data-filter]').forEach(button=>button.addEventListener('click',()=>{filter=button.dataset.filter;document.querySelectorAll('[data-filter]').forEach(other=>other.setAttribute('aria-pressed',String(other===button)));renderList();}));byId('prev').addEventListener('click',()=>navigate(-1,true));byId('next').addEventListener('click',()=>navigate(1,true));byId('backframe').addEventListener('click',()=>step(-1));byId('nextframe').addEventListener('click',()=>step(1));byId('rate').addEventListener('change',()=>{player.playbackRate=Number(byId('rate').value);});player.addEventListener('ended',()=>{if(byId('autonext').checked)navigate(1,true);});player.addEventListener('error',()=>{if(clips[current]&&!clips[current].error)text('error','视频无法打开。请确认 clips 文件夹与本页面放在同一目录，且视频已录制完成。');});
text('scope',data.scope||'具体展示条件请查看每段说明。');text('commit',data.sourceCommit||'未登记');const build=byId('build');const strong=document.createElement('strong');strong.textContent=data.width+'×'+data.height+' / '+data.fps+' fps / 无音轨';build.append(strong,document.createElement('br'),document.createTextNode('源码 '+String(data.sourceCommit||'未登记').slice(0,12)));const collections=byId('collections');for(const group of data.collections||[]){const a=document.createElement('a');a.href=mediaPath(group.file);a.textContent=group.title+' ↗';const small=document.createElement('small');small.textContent=group.clips+' 段 · '+formatTime(group.duration);a.append(small);collections.append(a);}collections.hidden=!(data.collections||[]).length;
const requested=new URLSearchParams(location.search).get('clip');const initial=requested?clips.findIndex(clip=>clip.id===requested):0;select(initial>=0?initial:0);
</script></body></html>'''


def write_gallery(directory, data, records, collections):
    view = dict(data)
    by_id = {record["id"]: record for record in records}
    view["clips"] = []
    for source in data["clips"]:
        clip = dict(source)
        record = by_id[clip["id"]]
        if record.get("error"):
            clip["error"] = record["error"]
        elif record.get("duration"):
            clip["duration"] = record["duration"]
        view["clips"].append(clip)
    view["collections"] = collections
    embedded = json.dumps(view, ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c").replace("&", "\\u0026")
    page = PAGE.replace("__MANIFEST__", embedded)
    (directory / "index.html").write_text(page, encoding="utf-8")


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path, help="Folder containing manifest.json and clips/")
    parser.add_argument("--concat", action="store_true", help="Also create chaptered, stream-copy MP4 collections for battles, synergies and walks")
    parser.add_argument("--ffmpeg", help="ffmpeg executable path or command")
    parser.add_argument("--ffprobe", help="ffprobe executable path or command")
    args = parser.parse_args(argv)
    directory = args.directory.expanduser().resolve()
    if not directory.is_dir():
        parser.error(f"Capture directory does not exist: {directory}")
    try:
        data = load_manifest(directory)
        ffmpeg = executable("ffmpeg", args.ffmpeg) if args.concat else None
        ffprobe = executable("ffprobe", args.ffprobe, companion=ffmpeg)
        records, errors = validate_clips(directory, data, ffprobe)
        collections = []
        if args.concat and not errors:
            try:
                collections = concatenate(directory, data, records, ffmpeg, ffprobe)
            except (ValueError, RuntimeError, OSError) as error:
                errors.append(f"Collections: {error}")
        write_gallery(directory, data, records, collections)
        expected_paths = {clip_path(directory, clip["file"]) for clip in data["clips"]}
        actual_files = list((directory / "clips").rglob("*.mp4")) if (directory / "clips").is_dir() else []
        report = {
            "passed": not errors, "sourceCommit": data.get("sourceCommit"),
            "scope": "File/codec/frame/duration validation and offline gallery packaging; not artistic or gameplay approval.",
            "expectedClips": len(data["clips"]), "validatedClips": sum(record["passed"] for record in records),
            "filesInClipsDirectory": len(actual_files),
            "unlistedFiles": [str(path.relative_to(directory)) for path in actual_files if path.resolve() not in expected_paths],
            "width": data["width"], "height": data["height"], "fps": data["fps"], "audio": False,
            "clips": records, "collections": collections, "errors": errors,
        }
        (directory / "package-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        for error in errors:
            print("ERROR: " + error, file=sys.stderr)
        print(f"SHOWCASE_PACKAGE_{'PASS' if not errors else 'FAIL'}: {report['validatedClips']}/{report['expectedClips']} clips; {len(collections)} collections; {directory / 'index.html'}")
        return 1 if errors else 0
    except (ValueError, RuntimeError, OSError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
