"""Generate a stylized, illustrative skeleton OBJ for the AnatoLens prototype.
Coordinates are metres, +Z faces forward. This is not medical-grade anatomy.
"""
from math import cos, pi, sin, sqrt
from pathlib import Path
OUT=Path(__file__).resolve().parent
v=[]; f=[]; current="bone"

def obj(name, mat="bone"):
    global current
    f.append(("o",name)); f.append(("m",mat)); current=mat
def vert(p):
    v.append(p); return len(v)
def face(ids): f.append(tuple(ids))
def add(a,b): return tuple(x+y for x,y in zip(a,b))
def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def mul(a,s): return tuple(x*s for x in a)
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def norm(a): return mul(a,1/sqrt(max(dot(a,a),1e-12)))
def lerp(a,b,t): return tuple(x+(y-x)*t for x,y in zip(a,b))

def ell(c,r,name="bone",mat="bone",n=16,k=10):
    obj(name,mat); rows=[]
    for j in range(k+1):
        lat=-pi/2+pi*j/k; row=[]
        for i in range(n):
            lon=2*pi*i/n
            row.append(vert((c[0]+r[0]*cos(lat)*cos(lon),c[1]+r[1]*sin(lat),c[2]+r[2]*cos(lat)*sin(lon))))
        rows.append(row)
    for j in range(k):
        for i in range(n): face((rows[j][i],rows[j][(i+1)%n],rows[j+1][(i+1)%n],rows[j+1][i]))

def tube(points,rs,name="bone",mat="bone",sides=9):
    obj(name,mat); rings=[]
    if isinstance(rs,(int,float)): rs=[rs]*len(points)
    for j,p in enumerate(points):
        tangent=norm(sub(points[min(j+1,len(points)-1)],points[max(0,j-1)]))
        ref=(0,0,1) if abs(tangent[2])<.85 else (0,1,0)
        a=norm(cross(tangent,ref)); b=norm(cross(tangent,a)); ring=[]
        for i in range(sides):
            q=add(p,add(mul(a,rs[j]*cos(2*pi*i/sides)),mul(b,rs[j]*sin(2*pi*i/sides))))
            ring.append(vert(q))
        rings.append(ring)
    for j in range(len(rings)-1):
        for i in range(sides): face((rings[j][i],rings[j][(i+1)%sides],rings[j+1][(i+1)%sides],rings[j+1][i]))

def bone(a,b,r,name):
    tube([lerp(a,b,t) for t in (0,.1,.4,.68,.92,1)],[r*.65,r,r*.72,r*.72,r,r*.65],name)
    ell(a,(r*.72,)*3,name+" joint",n=10,k=6); ell(b,(r*.72,)*3,name+" joint",n=10,k=6)

# Skull, face, jaw, eye sockets and nose.
ell((0,1.59,0),(.105,.135,.095),"Cranium")
ell((0,1.485,.018),(.074,.047,.072),"Face")
for s in (-1,1):
    ell((s*.041,1.565,.079),(.027,.032,.009),"Eye socket","cavity",12,8)
    tube([(s*.025,1.525,.079),(s*.055,1.505,.068),(s*.067,1.48,.05)],[.011,.012,.009],"Cheek bone")
ell((0,1.54,.082),(.014,.035,.018),"Nasal bone")
ell((0,1.466,.042),(.058,.016,.048),"Jaw")
for s in (-1,1): tube([(s*.052,1.482,.04),(s*.06,1.515,.025)],[.012,.009],"Jaw ramus")

# Spine and vertebrae.
for i in range(7):
    y=1.43-i*.034; ell((0,y,-.018),(.027,.018,.026),"Cervical vertebra",n=10,k=6)
for i in range(12):
    y=1.19-i*.027; ell((0,y,-.025),(.038,.018,.031),"Thoracic vertebra",n=10,k=6)
    bone((0,y,-.045),(0,y,-.095),.009,"Spinous process")
for i in range(5):
    y=.84-i*.035; ell((0,y,-.02),(.047,.021,.036),"Lumbar vertebra",n=10,k=6)
ell((0,.64,-.012),(.062,.09,.045),"Sacrum")

# Twelve curved pairs of ribs and sternum.
for pair in range(12):
    y=1.19-pair*.033; rx=.17-pair*.0045; rz=.155-pair*.002
    for s in (-1,1):
        points=[]
        for step in range(9):
            t=pi/2*step/8
            points.append((s*(.035+rx*sin(t)),y-.012*sin(t)+.005*sin(2*t),-.055+rz*(1-cos(t))))
        tube(points,[.009-pair*.00025]*len(points),"Rib %02d"%(pair+1),sides=7)
bone((0,1.25,.115),(0,.84,.13),.018,"Sternum")

# Shoulder girdle, arms, palms and fingers in a relaxed T pose.
for s in (-1,1):
    bone((0,1.35,0),(s*.18,1.34,.015),.018,"Clavicle")
    ell((s*.19,1.32,-.025),(.045,.055,.028),"Scapula")
    ell((s*.22,1.31,.02),(.043,.043,.043),"Shoulder joint")
    shoulder=(s*.22,1.31,.02); elbow=(s*.49,1.28,.015)
    bone(shoulder,elbow,.028,"Humerus")
    bone((s*.49,1.295,.035),(s*.72,1.285,.045),.013,"Radius")
    bone((s*.49,1.265,-.005),(s*.72,1.245,.005),.012,"Ulna")
    ell((s*.755,1.25,.025),(.038,.024,.027),"Wrist bones",n=10,k=6)
    ell((s*.80,1.245,.03),(.052,.018,.035),"Palm",n=10,k=6)
    for finger in range(4):
        z=.065-finger*.022; a=(s*.82,1.245,z); b=(s*(.89+(.015 if finger in (1,2) else 0)),1.238,z); m=lerp(a,b,.56)
        bone(a,m,.007,"Finger phalanx"); bone(m,b,.006,"Finger phalanx")
    bone((s*.79,1.245,0),(s*.83,1.22,-.045),.009,"Thumb")

# Pelvis, hips, femurs, kneecaps, lower legs and feet.
ell((0,.72,-.005),(.16,.09,.07),"Pelvis")
ell((0,.70,.018),(.055,.072,.046),"Pubic bone")
for s in (-1,1):
    ell((s*.13,.72,0),(.045,.064,.05),"Hip socket",n=12,k=8)
    hip=(s*.13,.68,0); knee=(s*.15,.34,.015); ankle=(s*.15,.07,0)
    bone(hip,knee,.042,"Femur")
    ell((s*.15,.34,.047),(.027,.034,.02),"Patella",n=10,k=6)
    bone((s*.15,.32,.005),ankle,.025,"Tibia")
    bone((s*.18,.32,-.018),(s*.17,.075,-.015),.012,"Fibula")
    ell((s*.15,.055,.035),(.045,.02,.08),"Foot",n=10,k=6)
    for toe in range(5):
        z=.085-toe*.026; bone((s*.15,.052,z),(s*(.19-toe*.004),.045,z+.055),.0065,"Toe phalanx")

with (OUT/"Skeleton.obj").open("w",encoding="utf-8",newline="\n") as out:
    out.write("# AnatoLens generated illustrative skeleton; units metres; +Z forward\nmtllib Skeleton.mtl\n")
    for x,y,z in v: out.write("v %.6f %.6f %.6f\n"%(x,y,z))
    lastmat=None
    for item in f:
        if item[0]=="o": out.write("o "+item[1].replace(" ","_")+"\n")
        elif item[0]=="m":
            if item[1]!=lastmat: out.write("usemtl "+item[1]+"\n"); lastmat=item[1]
        else: out.write("f "+" ".join(map(str,item))+"\n")
(OUT/"Skeleton.mtl").write_text("""newmtl bone
Kd 0.86 0.82 0.70
Ka 0.25 0.23 0.20
Ks 0.10 0.10 0.10
Ns 14

newmtl cavity
Kd 0.10 0.08 0.07
Ka 0.04 0.03 0.03
Ks 0.02 0.02 0.02
Ns 4
""",encoding="utf-8")
print("Wrote",OUT/"Skeleton.obj","with",len(v),"vertices; height 1.73 m.")

