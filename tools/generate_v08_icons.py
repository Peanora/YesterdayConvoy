"""Render original geometric v0.8 UI glyphs from the same SVG shape definitions.
No external artwork, fonts, or image inputs. Requires Pillow for PNG rasterization.
"""
from pathlib import Path
import math,uuid,xml.etree.ElementTree as E
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'docs/art/source-records/v08-ui-icons'
DEST=ROOT/'Assets/Art/UI/Synergies/Resources/UI/Synergies'
W='#f7f2e4'
def shape(tag,**a):return tag,{k.rstrip('_').replace('_','-'):str(v) for k,v in a.items()}
def poly(points,fill=W,stroke='none',width=0):return shape('polygon',points=' '.join(f'{x},{y}' for x,y in points),fill=fill,stroke=stroke,stroke_width=width)
def rect(x,y,w,h,fill=W,stroke='none',width=0):return shape('rect',x=x,y=y,width=w,height=h,fill=fill,stroke=stroke,stroke_width=width)
def circ(x,y,r,fill=W,stroke='none',width=0):return shape('circle',cx=x,cy=y,r=r,fill=fill,stroke=stroke,stroke_width=width)
def line(x,y,a,b,color=W,width=10):return shape('line',x1=x,y1=y,x2=a,y2=b,stroke=color,stroke_width=width)
ART={
'merchant':[poly([(80,54),(150,54),(135,94),(170,130),(174,198),(150,216),(58,216),(38,194),(44,132),(94,94)]),rect(80,92,64,10,'#242b2b'),circ(184,140,38,'#242b2b',W,10),line(184,118,184,162),line(169,140,199,140)],
'armory':[rect(42,184,172,20)]+[poly([(x,86),(x+16,56),(x+32,86)]) for x in [56,112,168]]+[rect(x,90,32,80) for x in [56,112,168]],
'painted-world':[rect(36,42,184,158,'none',W,12),poly([(56,174),(98,114),(132,150),(168,92),(202,174)],'none',W,9),circ(77,82,13),line(154,224,226,150,W,16)],
'steam':[poly([(52,112),(82,86),(174,86),(204,112),(204,204),(52,204)],'none',W,12),circ(99,145,13),circ(159,145,13),rect(100,178,56,10),line(92,70,82,38),line(128,67,128,26),line(164,70,174,38)],
'v08-crown':[poly([(42,72),(82,124),(128,52),(174,124),(214,72),(190,194),(66,194)],'#c6943e',W,7),rect(68,190,120,18,'#eac874'),circ(128,151,13,'#ab553e')],
'v08-shield':[poly([(128,38),(208,76),(196,164),(128,220),(60,164),(48,76)],'#4f91b1',W,8),line(128,68,128,174,W,12),line(91,118,165,118,W,12)],
'v08-cloak':[poly([(102,42),(154,42),(174,90),(218,214),(38,214),(82,90)],'#995451',W,8),line(102,105,84,190,'#c67c6a',8),line(154,105,172,190,'#c67c6a',8),circ(128,96,14,'#eac874',W,4)]}

def render(shapes):
 scale=4;im=Image.new('RGBA',(256*scale,256*scale));d=ImageDraw.Draw(im)
 def n(a,key):return float(a[key])*scale
 for tag,a in shapes:
  fill=a.get('fill','none');fill=None if fill=='none' else fill
  stroke=a.get('stroke','none');stroke=None if stroke=='none' else stroke
  width=round(float(a.get('stroke-width',0))*scale)
  if tag=='rect':d.rectangle([n(a,'x'),n(a,'y'),n(a,'x')+n(a,'width'),n(a,'y')+n(a,'height')],fill=fill,outline=stroke,width=width)
  elif tag=='circle':d.ellipse([n(a,'cx')-n(a,'r'),n(a,'cy')-n(a,'r'),n(a,'cx')+n(a,'r'),n(a,'cy')+n(a,'r')],fill=fill,outline=stroke,width=width)
  elif tag=='polygon':
   points=[tuple(float(v)*scale for v in point.split(',')) for point in a['points'].split()]
   if fill:d.polygon(points,fill=fill)
   if stroke:d.line(points+[points[0]],fill=stroke,width=width,joint='curve')
  elif tag=='line':d.line([n(a,'x1'),n(a,'y1'),n(a,'x2'),n(a,'y2')],fill=stroke,width=width)
 return im.resize((256,256),Image.Resampling.LANCZOS)

if __name__=='__main__':
 SOURCE.mkdir(parents=True,exist_ok=True)
 template=(DEST/'resonance.png.meta').read_text(encoding='utf-8')
 import re
 for name,shapes in ART.items():
  svg=E.Element('svg',{'xmlns':'http://www.w3.org/2000/svg','width':'256','height':'256','viewBox':'0 0 256 256'})
  for tag,a in shapes:E.SubElement(svg,tag,a)
  (SOURCE/(name+'.svg')).write_text(E.tostring(svg,encoding='unicode')+'\n',encoding='utf-8')
  render(shapes).save(DEST/(name+'.png'))
  meta=DEST/(name+'.png.meta')
  if not meta.exists():meta.write_text(re.sub(r'^guid: \w+', 'guid: '+uuid.uuid4().hex,template,flags=re.M),encoding='utf-8',newline='\n')
 print('V08_ORIGINAL_UI_ICONS_PASS:',len(ART),'SVG + transparent PNG, stable existing GUIDs retained.')
