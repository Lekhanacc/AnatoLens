from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
from math import sqrt
import random

root=Path(__file__).resolve().parent
verts=[]
triangles=[]
mat="bone"
for line in (root/"Skeleton.obj").open(encoding="utf-8"):
    p=line.split()
    if not p: continue
    if p[0]=="v": verts.append(tuple(map(float,p[1:4])))
    elif p[0]=="usemtl": mat=p[1]
    elif p[0]=="f":
        ids=[int(x.split("/")[0])-1 for x in p[1:]]
        for i in range(1,len(ids)-1):
            triangles.append((ids[0],ids[i],ids[i+1],mat))

W,H=1200,1400
im=Image.new("RGB",(W,H),(231,234,229))
d=ImageDraw.Draw(im)
# Decorative, irregular fine-line detail improves image-feature distribution around the bone silhouette.
rng=random.Random(28417)
for i in range(520):
    x=rng.randint(30,W-30); y=rng.randint(150,H-35)
    r=rng.randint(2,5)
    shade=rng.choice([(184,194,190),(193,201,197),(205,211,207)])
    if i%3==0:
        d.ellipse((x-r,y-r,x+r,y+r),outline=shade,width=2)
    else:
        d.line((x-r*2,y-r,x+r*2,y+r),fill=shade,width=2)
        d.line((x-r,y+r*2,x+r,y-r*2),fill=shade,width=1)
for i in range(260):
    x=rng.randint(45,W-45); y=rng.randint(160,H-155)
    dx=rng.randint(5,18); dy=rng.randint(5,18)
    shade=rng.choice([(195,203,198),(205,211,207),(216,220,214)])
    d.line((x,y,x+dx,y+dy,x+dx*2,y-dy//2),fill=shade,width=1)
for x in (38,1162):
    d.line((x,150,x,H-38),fill=(110,127,126),width=2)
    for y in range(180,H-50,26):
        d.line((x,y,x+(12 if x<100 else -12),y),fill=(135,150,148),width=2)
# Frame and heading.
d.rounded_rectangle((22,22,W-22,H-22),radius=28,outline=(48,69,75),width=5)
d.rectangle((40,40,W-40,137),fill=(25,42,51))
try:
    title=ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf",50)
    sub=ImageFont.truetype("C:/Windows/Fonts/arial.ttf",24)
    label=ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf",22)
except OSError:
    title=ImageFont.load_default(); sub=title; label=title
d.text((W//2,63),"HUMAN SKELETON",font=title,fill=(239,233,210),anchor="mm")
d.text((W//2,113),"ANATOMY EXPLORER  •  FRONT VIEW",font=sub,fill=(137,215,208),anchor="mm")

xs=[v[0] for v in verts]; ys=[v[1] for v in verts]
xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys)
scale=min(850/(xmax-xmin),1050/(ymax-ymin))
cx=W/2; cy=790
def project(p):
    return (cx+(p[0]-(xmin+xmax)/2)*scale,cy-(p[1]-(ymin+ymax)/2)*scale)
items=[]
for a,b,c,m in triangles:
    va,vb,vc=verts[a],verts[b],verts[c]
    z=(va[2]+vb[2]+vc[2])/3
    u=(vb[0]-va[0],vb[1]-va[1],vb[2]-va[2])
    v=(vc[0]-va[0],vc[1]-va[1],vc[2]-va[2])
    n=(u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0])
    nl=sqrt(sum(q*q for q in n)) or 1
    light=.58+.42*abs((n[0]*.25+n[1]*.38+n[2]*.89)/nl)
    base=(43,42,39) if m=="cavity" else (214,207,184)
    color=tuple(int(q*light) for q in base)
    items.append((z,(project(va),project(vb),project(vc)),color))
# Far faces first, front faces last.
items.sort(key=lambda item:item[0])
draw=ImageDraw.Draw(im)
for _,poly,color in items:
    draw.polygon(poly,fill=color)
# Add a slim footer with visual reference dimensions.
draw.rectangle((40,H-108,W-40,H-40),fill=(25,42,51))
d=ImageDraw.Draw(im)
d.text((W//2,H-74),"SCAN THIS IMAGE TO EXPLORE THE SKELETON IN AR",font=sub,
       fill=(239,233,210),anchor="mm")
out=root.parents[2]/"Skeleton.png"
im.save(out,optimize=True)
print(out,im.size)

