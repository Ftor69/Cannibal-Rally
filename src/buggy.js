import * as T from 'three';
// Original dimensioned prototype; no extracted game assets.
export function createBuggy(){
 const body=new T.Group(),wheels=[];
 const orange=new T.MeshStandardMaterial({color:0xf37722,metalness:.65,roughness:.32});
 const black=new T.MeshStandardMaterial({color:0x181b1b,metalness:.4,roughness:.6});
 const steel=new T.MeshStandardMaterial({color:0x889596,metalness:.85,roughness:.3});
 const rubber=new T.MeshStandardMaterial({color:0x111111,roughness:.95});
 function box(w,h,d,x,y,z,mat=orange){const m=new T.Mesh(new T.BoxGeometry(w,h,d),mat);m.position.set(x,y,z);m.castShadow=true;body.add(m);return m;}
 function bar(a,b,r=.045,mat=black,parent=body){const av=new T.Vector3(...a),bv=new T.Vector3(...b),delta=bv.clone().sub(av);const mesh=new T.Mesh(new T.CylinderGeometry(r,r,delta.length(),12),mat);mesh.position.copy(av.add(bv).multiplyScalar(.5));mesh.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),delta.normalize());mesh.castShadow=true;parent.add(mesh);}
 box(1.55,.3,2.9,0,0,0,black);box(1.62,.28,1.15,0,.28,-1.05);box(1.55,.35,.65,0,.32,1.15);box(.12,.55,1.7,-.79,.28,0);box(.12,.55,1.7,.79,.28,0);
 for(const x of [-.45,.45]){box(.5,.18,.5,x,.28,.25,black);const seat=box(.5,.65,.14,x,.55,.5,black);seat.rotation.x=-.12;}
 for(const x of [-.75,.75]){bar([x,.2,-.75],[x,1.25,-.35]);bar([x,1.25,-.35],[x,1.25,.7]);bar([x,1.25,.7],[x,.2,1.2]);bar([x,.2,1.2],[-x,1.25,.7],.025);}
 for(const z of [-.35,.7])bar([-.75,1.25,z],[.75,1.25,z]);
 box(1.65,.09,.95,0,1.3,.18);bar([-1,.0,-1.7],[1,0,-1.7],.08);bar([-1,0,1.65],[1,0,1.65],.08);
 const lamp=new T.MeshStandardMaterial({color:0xffe7bd,emissive:0xffd599,emissiveIntensity:1});for(const x of [-.56,.56])box(.24,.16,.08,x,.32,-1.66,lamp);
 for(const x of [-1,1])for(const z of [-1.15,1.15]){
  const wheel=new T.Group();const tire=new T.Mesh(new T.CylinderGeometry(.52,.52,.34,32),rubber);tire.rotation.z=Math.PI/2;wheel.add(tire);
  for(let i=0;i<24;i++){const a=i/24*Math.PI*2;const tread=new T.Mesh(new T.BoxGeometry(.38,.11,.14),rubber);tread.position.set(0,Math.cos(a)*.52,Math.sin(a)*.52);tread.rotation.x=a;wheel.add(tread);}
  const rim=new T.Mesh(new T.CylinderGeometry(.27,.27,.36,16),steel);rim.rotation.z=Math.PI/2;wheel.add(rim);wheels.push(wheel);
  bar([x*.6,0,z],[x,-.2,z],.055,steel);bar([x*.65,.2,z],[x,-.15,z],.065,orange);
 }
 return {body,wheels};
}
