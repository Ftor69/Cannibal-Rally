import * as C from 'cannon-es';
export function createPhysics(){
 const world=new C.World({gravity:new C.Vec3(0,-9.81,0)});world.broadphase=new C.SAPBroadphase(world);world.defaultContactMaterial.friction=.7;
 const ground=new C.Body({mass:0,shape:new C.Box(new C.Vec3(150,1.5,150)),position:new C.Vec3(0,-1.5,0)});world.addBody(ground);
 const chassis=new C.Body({mass:650,shape:new C.Box(new C.Vec3(.82,.28,1.5)),position:new C.Vec3(0,1.2,0)});chassis.angularDamping=.6;
 const vehicle=new C.RaycastVehicle({chassisBody:chassis,indexRightAxis:0,indexUpAxis:1,indexForwardAxis:2});
 for(const x of [-1,1])for(const z of [-1.15,1.15])vehicle.addWheel({radius:.52,directionLocal:new C.Vec3(0,-1,0),axleLocal:new C.Vec3(-1,0,0),chassisConnectionPointLocal:new C.Vec3(x,0,z),suspensionStiffness:30,suspensionRestLength:.35,frictionSlip:3.5,dampingRelaxation:2.3,dampingCompression:4.4,maxSuspensionForce:100000,rollInfluence:.08,maxSuspensionTravel:.25,customSlidingRotationalSpeed:-30,useCustomSlidingRotationalSpeed:true});
 vehicle.addToWorld(world);
 function controls(throttle,steering,brake,enabled){for(let i=0;i<4;i++){vehicle.applyEngineForce(enabled?throttle*1300:0,i);vehicle.setBrake(brake?75:enabled?0:8,i);vehicle.setSteeringValue(i===0||i===2?steering*.42:0,i);}}
 function obstacle(x,y,z,sx,sy,sz){const b=new C.Body({mass:0,shape:new C.Box(new C.Vec3(sx/2,sy/2,sz/2)),position:new C.Vec3(x,y,z)});world.addBody(b);return b;}
 return {world,chassis,vehicle,controls,obstacle};
}
