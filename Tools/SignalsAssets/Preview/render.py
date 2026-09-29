"""Render production layout exports offline; this is not an in-game screenshot."""
import json, math, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageChops

root = Path(sys.argv[1] if len(sys.argv) > 1 else 'artifacts/signals-ui')
font_path = Path('F:/SPT 4.1.x/Development/CJ-SDK/Assets/TextMesh Pro/Fonts/LiberationSans.ttf')
if not font_path.exists(): font_path = Path('C:/Windows/Fonts/arial.ttf')
scale = 2
def rgba(c): return tuple(round(max(0, min(1, v))*255) for v in c)
issues=[]
for scenario in ('receiver','pairing','unlocked','practice'):
    canvas=Image.new('RGBA',(1920*scale,1080*scale),(31,36,34,255))
    for node in json.loads((root/(scenario+'.json')).read_text()):
        x,y,w,h=[node[k]*scale for k in ('x','y','w','h')]
        p=node['payload']; draw=ImageDraw.Draw(canvas)
        if p['kind']=='image':
            if w>0 and h>0:
                tile=Image.new('RGBA',(math.ceil(w),math.ceil(h)),rgba(p['color']))
                canvas.alpha_composite(tile,(round(x),round(y)))
        elif p['kind']=='mesh':
            v=p['vertices']
            for k in range(0,len(p['indices']),3):
                verts=[v[i] for i in p['indices'][k:k+3]]
                pts=[(x+pt[0]*scale,y+pt[1]*scale) for pt in verts]
                l,t=math.floor(min(a for a,b in pts)),math.floor(min(b for a,b in pts))
                r,b=math.ceil(max(a for a,b in pts))+1,math.ceil(max(b for a,b in pts))+1
                if r<=l or b<=t: continue
                mask=Image.new('L',(r-l,b-t));ImageDraw.Draw(mask).polygon([(a-l,b-t) for a,b in pts],fill=255)
                top=min(verts,key=lambda v:v[1]); bottom=max(verts,key=lambda v:v[1]); span=(bottom[1]-top[1])*scale
                tile=Image.new('RGBA',mask.size); ink=ImageDraw.Draw(tile)
                for yy in range(b-t):
                    f=0 if span==0 else max(0,min(1,(t+yy-y-top[1]*scale)/span))
                    color=rgba([a+(b-a)*f for a,b in zip(top[2:],bottom[2:])])
                    ink.line((0,yy,r-l,yy),fill=color)
                tile.putalpha(ImageChops.multiply(tile.getchannel('A'),mask));canvas.alpha_composite(tile,(l,t))
        elif p['kind']=='text':
            font=ImageFont.truetype(str(font_path),round(p['size']*scale))
            lines=[]
            for line in (p['value'] or '').split('\n'):
                if p['wrap']:
                    current=''
                    for word in line.split():
                        candidate=(current+' '+word).strip()
                        if font.getlength(candidate)>w and current: lines.append(current);current=word
                        else: current=candidate
                    lines.append(current)
                else:
                    if font.getlength(line)>w+2: issues.append(f'{scenario}: text wider than field: {line}')
                    lines.append(line)
            leading=p['size']*scale*1.2
            if len(lines)*leading>h+5: issues.append(f'{scenario}: text taller than field: {p["value"]}')
            yy=y+(h-len(lines)*leading)/2
            for line in lines:
                length=font.getlength(line)
                xx=x+(w-length if p['align']=='MidlineRight' else (w-length)/2 if p['align']=='Center' else 0)
                draw.text((xx,yy),line,font=font,fill=rgba(p['color']),anchor='lt'); yy+=leading
    for width,height in [(1920,1080),(2560,1440),(3440,1440)]:
        factor=min(width/1920,height/1080)
        size=(round(1920*factor),round(1080*factor))
        image=Image.new('RGB',(width,height),(31,36,34)); image.paste(canvas.convert('RGB').resize(size,Image.Resampling.LANCZOS),((width-size[0])//2,(height-size[1])//2))
        image.save(root/f'{scenario}-{width}x{height}.png')
(root/'layout-checks.json').write_text(json.dumps({'states':4,'resolutions':3,'textIssues':issues,'scope':'Offline production layout and mesh rendering; Unity interaction and presentation require live acceptance.'},indent=2))
print('\n'.join(issues) if issues else 'All four states fit at 1080p, 1440p and ultrawide; text measurements passed.')
if issues: sys.exit(1)
