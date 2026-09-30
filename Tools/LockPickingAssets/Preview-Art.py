"""Offline textured mesh preview. Uses the actual compiled geometry and atlas.
This verifies asset orientation/composition, not Unity shader or in-game acceptance.
Requires NumPy and Pillow. No application/editor process is started.
"""
import io
import json
import math
from pathlib import Path
import struct
import zipfile
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
archive = zipfile.ZipFile(ROOT/'Client/SkillsExtended.Client.Skills/Assets/LockPicking.assets')
atlas = np.asarray(Image.open(io.BytesIO(archive.read('AT.png'))).convert('RGB'), dtype=np.float32)/255


def mesh(name, scale=1, angle=0, tip=False, position=(0,0,0), tilt=0, bend=0):
    data = archive.read(name+'.mesh')
    nv, ni = struct.unpack('<II',data[:8])
    v = np.frombuffer(data,dtype='<f4',offset=8,count=nv*8).reshape(-1,8).copy()
    ids = np.frombuffer(data,dtype='<u4',offset=8+nv*32,count=ni).reshape(-1,3)
    v[:,:3] *= scale
    if tip: v[:,1] -= np.max(v[:,1])
    a = math.radians(angle)
    r = np.array([[math.cos(a),-math.sin(a),0],[math.sin(a),math.cos(a),0],[0,0,1]])
    t=math.radians(tilt); b=math.radians(bend)
    r=r @ np.array([[1,0,0],[0,math.cos(t),-math.sin(t)],[0,math.sin(t),math.cos(t)]]) @ np.array([[math.cos(b),-math.sin(b),0],[math.sin(b),math.cos(b),0],[0,0,1]])
    v[:,:3] = v[:,:3] @ r.T + np.array(position)
    v[:,3:6] = v[:,3:6] @ r.T
    return v, ids


def render(tier=1, lift=0, depth=0, unlocked=False):
    width,height=1000,310
    rgb=np.full((height,width,3),[.042,.049,.052],dtype=np.float32)
    zbuffer=np.full((height,width),-1000,dtype=np.float32)
    variant='02' if tier>=4 else '01'
    objects=[mesh('Housing_'+variant),mesh('Lock_'+variant,angle=90 if unlocked else 0),
             mesh('Pick_04',.32,123,False,(-.0015+depth*.001,.006+lift*.001,.045-depth*.002),tilt=12+lift*5,bend=lift*6),
             mesh('Screwdriver',.25,236,False,(.002,-.01,.037),tilt=10)]
    lights=[(np.array([-.22,.2,.3]),np.array([1,.94,.85]),1.05),
            (np.array([.22,-.02,.25]),np.array([.80,.89,1]),.55)]
    for vertices,triangles in objects:
        points=vertices[:,:3].copy()
        pixels=np.column_stack([width/2+points[:,0]*height/.27,height/2-(points[:,1]+.035)*height/.27])
        for tri in triangles:
            p=pixels[tri]; v=vertices[tri]
            xmin=max(0,int(np.floor(p[:,0].min())));xmax=min(width-1,int(np.ceil(p[:,0].max())))
            ymin=max(0,int(np.floor(p[:,1].min())));ymax=min(height-1,int(np.ceil(p[:,1].max())))
            if xmin>xmax or ymin>ymax:continue
            denom=(p[1,1]-p[2,1])*(p[0,0]-p[2,0])+(p[2,0]-p[1,0])*(p[0,1]-p[2,1])
            if abs(denom)<1e-5:continue
            yy,xx=np.mgrid[ymin:ymax+1,xmin:xmax+1];xx=xx+.5;yy=yy+.5
            a=((p[1,1]-p[2,1])*(xx-p[2,0])+(p[2,0]-p[1,0])*(yy-p[2,1]))/denom
            b=((p[2,1]-p[0,1])*(xx-p[2,0])+(p[0,0]-p[2,0])*(yy-p[2,1]))/denom;c=1-a-b
            z=a*v[0,2]+b*v[1,2]+c*v[2,2]
            visible=(a>=0)&(b>=0)&(c>=0)&(z>zbuffer[ymin:ymax+1,xmin:xmax+1])
            if not visible.any():continue
            interp=a[visible,None]*v[0]+b[visible,None]*v[1]+c[visible,None]*v[2]
            uv=interp[:,6:8]; tx=np.clip((uv[:,0]*atlas.shape[1]).astype(int),0,atlas.shape[1]-1);ty=np.clip(((1-uv[:,1])*atlas.shape[0]).astype(int),0,atlas.shape[0]-1)
            color=atlas[ty,tx]; normals=interp[:,3:6];normals/=np.maximum(.00001,np.linalg.norm(normals,axis=1,keepdims=True))
            illumination=np.full_like(color,.15);spec=np.zeros_like(color)
            for location,tint,strength in lights:
                direction=location-interp[:,:3];direction/=np.maximum(.001,np.linalg.norm(direction,axis=1,keepdims=True))
                diffuse=np.maximum(0,(direction*normals).sum(axis=1))
                half=direction+np.array([0,0,1]);half/=np.linalg.norm(half,axis=1,keepdims=True)
                highlight=np.maximum(0,(half*normals).sum(axis=1))**45
                illumination+=diffuse[:,None]*tint*strength
                spec+=highlight[:,None]*tint*.25
            rgb[ymin:ymax+1,xmin:xmax+1][visible]=np.clip(color*illumination+spec,0,1)
            zbuffer[ymin:ymax+1,xmin:xmax+1][visible]=z[visible]
    return Image.fromarray((rgb*255).astype(np.uint8))


def cutaway(path, pins, outcome='Active'):
    # Exported production Graphic triangles; this is not a separately redrawn diagram.
    geometry=json.loads(path.read_text(encoding='utf-8'))
    image=Image.new('RGB',(2000,380));draw=ImageDraw.Draw(image)
    vertices=geometry['vertices']
    for tri in geometry['triangles']:
        points=[((vertices[i][0]+500)*2,(95-vertices[i][1])*2) for i in tri]
        color=tuple(round(c*255) for c in vertices[tri[0]][2:5])
        draw.polygon(points,fill=color)
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',26)
    draw.text((50,44),'SIDE VIEW',font=font,fill=(212,214,204),anchor='lm')
    label={'Active':'TENSION ON','Unlocked':'RELEASED','PickBroken':'PICK BROKEN'}[outcome]
    draw.text((1950,44),label,font=font,fill=(212,214,204),anchor='rm')
    for pin in range(pins):
        x=20+260*pin/(pins-1)
        draw.text(((x+500)*2,358),str(pin+1),font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',24),fill=(212,214,204),anchor='mm')
    return image.resize((1000,190),Image.Resampling.LANCZOS)


def preview():
    output=ROOT/'artifacts/lockpicking';output.mkdir(parents=True,exist_ok=True)
    art=render();art.save(output/'asset-closeup.png')
    image=Image.new('RGB',(1920,1080),(12,14,16));draw=ImageDraw.Draw(image)
    left,top=420,130;draw.rectangle((left,top,left+1080,top+820),fill=(11,13,13))
    image.paste(art,(460,235))
    cases=output/'cutaway-preview'
    source=cases/'cutaway-3-0-0-Active.json'
    if not source.exists():
        raise FileNotFoundError('Run lock-picking checks with artifacts/lockpicking/cutaway-preview as the output argument first.')
    cutaway(source,3).save(output/'cutaway-detail.png')
    image.paste(cutaway(source,3),(460,565))
    sheet=Image.new('RGB',(1500,3*125),(12,14,16))
    sheet_draw=ImageDraw.Draw(sheet)
    for column,pins in enumerate((3,4,5)):
        for row,outcome in enumerate(('Active','Unlocked','PickBroken')):
            sample=cutaway(cases/f'cutaway-{pins}-{pins-1}-1-{outcome}.json',pins,outcome)
            sheet.paste(sample.resize((500,95),Image.Resampling.LANCZOS),(column*500,row*125+30))
            sheet_draw.text((column*500+12,row*125+5),f'{pins} pins / {outcome} / maximum lift',fill=(212,214,204))
    sheet.save(output/'cutaway-states.png')
    def text(x,y,s,size=22,fill=(212,214,204),anchor='mm'):
        font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',size)
        draw.text((x,y),s,font=font,fill=fill,anchor=anchor)
    text(460,179,'LOCK PICKING',25,anchor='lm');text(1460,179,'TIER 1 / 3 PINS',19,anchor='rm')
    text(960,787,'Feel for the binding pin',24);text(960,819,'DEPTH 1/3    ·    0 SET',17)
    for x,label,fraction,color in [(660,'STRAIN',0,(216,153,71)),(1180,'PICK',1,(166,184,166))]:
        text(x-160,848,label,13,anchor='lm');draw.rectangle((x-95,846,x+185,851),fill=(38,41,43))
        if fraction:draw.rectangle((x-95,846,x-95+int(280*fraction),851),fill=color)
    text(960,893,'MOUSE ← →  Depth     MOUSE ↑ ↓  Lift     HOLD A  Tension     ESC  Leave',17)
    text(960,921,'Release tension to drop all pins. Forcing the pick causes lasting wear.',17)
    text(960,1030,'Geometry composition only — does not reproduce the Unity material, reflections or lighting.',16,fill=(133,139,143))
    image.save(output/'preview-1920x1080.png')
    for size in [(2560,1440),(3440,1440)]:
        scale=min(size[0]/1920,size[1]/1080)
        fitted=image.resize((round(1920*scale),round(1080*scale)),Image.Resampling.LANCZOS)
        canvas=Image.new('RGB',size,(12,14,16));canvas.paste(fitted,((size[0]-fitted.width)//2,(size[1]-fitted.height)//2));canvas.save(output/f'preview-{size[0]}x{size[1]}.png')
    render(5).save(output/'asset-tier5.png')
    print(output)


if __name__=='__main__':preview()
