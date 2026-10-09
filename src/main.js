import * as T from 'three';
import {Survival,enemyStep,distance,checkpoints} from './core.js';
import {createPhysics} from './physics.js';
import {createBuggy} from './buggy.js';
import {bindInput} from './input.js';
const canvas=document.querySelector('#game');const renderer=new T.WebGLRenderer({canvas,antialias:true});renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.shadowMap.enabled=true;renderer.shadowMap.type=T.PCFSoftShadowMap;renderer.toneMapping=T.ACESFilmicToneMapping;
const scene=new T.Scene();scene.background=new T.Color(0x9aaaa3);scene.fog=new T.FogExp2(0x9aaaa3,.007);const camera=new T.PerspectiveCamera(60,1,.1,650);
scene.add(new T.HemisphereLight(0xe0efff,0x384128,2));const sun=new T.DirectionalLight(0xffdfb5,3);sun.position.set(-40,75,30);sun.castShadow=true;sun.shadow.mapSize.set(2048,2048);Object.assign(sun.shadow.camera,{left:-100,right:100,top:100,bottom:-100});scene.add(sun);
const physics=createPhysics(),{chassis,vehicle}=physics;const state=new Survival();const buggy=createBuggy();scene.add(buggy.body,...buggy.wheels);
const mat=(color)=>new T.MeshStandardMaterial({color,roughness:.85});const earth=mat(0x647049),bark=mat(0x574835),leaf=mat(0x263f2b),rockMat=mat(0x747c71);
function mesh(geo,material,x,y,z){const m=new T.Mesh(geo,material);m.position.set(x,y,z);m.castShadow=true;m.receiveShadow=true;scene.add(m);return m;}
mesh(new T.CylinderGeometry(145,150,3,128),earth,0,-1.5,0);const sea=mesh(new T.PlaneGeometry(1500,1500),new T.MeshStandardMaterial({color:0x426d72,metalness:.35,roughness:.25}),0,-1.7,0);sea.rotation.x=-Math.PI/2;
let seed=812;const rand=()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296;};const obstacles=[];
for(let i=0;i<190;i++){const x=(rand()-.5)*260,z=(rand()-.5)*260;if(Math.hypot(x,z)>135||Math.hypot(x,z)<17||checkpoints.some(p=>Math.hypot(x-p.x,z-p.z)<12))continue;const h=6+rand()*6;mesh(new T.CylinderGeometry(.25,.4,h,8),bark,x,h/2,z);mesh(new T.ConeGeometry(2.7,h,9),leaf,x,h*.95,z);physics.obstacle(x,h/2,z,.75,h,.75);obstacles.push({x,z,r:1});}
for(let i=0;i<22;i++){const x=(rand()-.5)*220,z=(rand()-.5)*220;if(Math.hypot(x,z)<18||checkpoints.some(p=>Math.hypot(x-p.x,z-p.z)<10))continue;mesh(new T.DodecahedronGeometry(1.6),rockMat,x,.9,z);physics.obstacle(x,.7,z,2.3,1.4,2.3);obstacles.push({x,z,r:2});}
// A small physical ramp lets wheel springs react to elevation.
const ramp=mesh(new T.BoxGeometry(5,.5,7),mat(0x7b6650),20,.85,-10);ramp.rotation.x=.22;const rb=physics.obstacle(20,.85,-10,5,.5,7);rb.quaternion.setFromEuler(.22,0,0);
mesh(new T.ConeGeometry(3,3,4),mat(0xbc7842),-6,1.5,8);mesh(new T.BoxGeometry(1.3,1,1),mat(0xc4a571),4,.5,8);
const campRing=mesh(new T.TorusGeometry(10,.12,8,64),mat(0xffa04c),0,.08,8);campRing.rotation.x=-Math.PI/2;
const gates=checkpoints.map(p=>{const g=new T.Group();for(const x of [-5,5]){const post=new T.Mesh(new T.BoxGeometry(.25,4,.25),mat(0xff8b32));post.position.set(x,2,0);g.add(post);}const top=new T.Mesh(new T.BoxGeometry(10,.25,.25),mat(0xff8b32));top.position.y=4;g.add(top);g.position.set(p.x,0,p.z);scene.add(g);return g;});
const player=mesh(new T.CapsuleGeometry(.35,1,5,10),mat(0xc8ae86),2,1,0);const enemies=[];
for(let i=0;i<10;i++){const mutant=i%3===0;const e={x:Math.cos(i)*55,z:Math.sin(i)*55,speed:mutant?3:2,sight:mutant?38:27,damage:mutant?9:4,cooldown:0,mutant};e.mesh=mesh(new T.CapsuleGeometry(mutant?.65:.38,mutant?1.8:1,5,10),mat(mutant?0x6e594d:0x9e806a),e.x,mutant?1.6:1,e.z);enemies.push(e);}
let noteTimer=0;function note(t){const m=document.querySelector('#message');m.textContent=t;m.style.display='block';noteTimer=3;}
const target=()=>state.driving?{x:chassis.position.x,z:chassis.position.z}:player.position;
const {keys}=bindInput(canvas,handleAction);
function handleAction(code){const speed=chassis.velocity.length();if(code==='KeyE'){const was=state.driving;const msg=state.interact(player.position,chassis.position,speed);if(was&&!state.driving){const offset=new T.Vector3(2.8,0,0).applyQuaternion(buggy.body.quaternion);let exit={x:chassis.position.x+offset.x,z:chassis.position.z+offset.z};if(obstacles.some(o=>distance(exit,o)<o.r+.6)){exit={x:chassis.position.x-offset.x,z:chassis.position.z-offset.z};}player.position.set(exit.x,1,exit.z);}note(msg);}if(code==='KeyF')note(state.service('repair',target(),speed,chassis.position));if(code==='KeyG')note(state.service('fuel',target(),speed,chassis.position));if(code==='KeyH')note(state.eat()?'Вы поели.':'Еда не требуется или закончилась.');if(code==='KeyR')note(state.startRace()?'Маршрут начат. Лимита времени нет.':'Сначала сядьте в багги.');if(code==='Enter')location.reload();}
chassis.addEventListener('collide',e=>{const v=Math.abs(e.contact.getImpactVelocityAlongNormal());if(v>3)state.damage((v-3)*2,true);});
function resize(){renderer.setSize(innerWidth,innerHeight);camera.aspect=innerWidth/innerHeight;camera.updateProjectionMatrix();}window.addEventListener('resize',resize);resize();
let previous=performance.now(),accumulator=0;const step=1/60;
function simulate(dt){const throttle=(keys.has('KeyW')?1:0)-(keys.has('KeyS')?1:0),turn=(keys.has('KeyA')?1:0)-(keys.has('KeyD')?1:0);state.tick(dt,throttle);physics.controls(throttle,turn,keys.has('Space')||!state.alive,state.engineAvailable);physics.world.step(dt);
 if(!state.driving&&state.alive){const direction=new T.Vector3(-turn,0,-throttle);if(direction.lengthSq()){direction.normalize();const next=player.position.clone().addScaledVector(direction,dt*4.5);if(Math.hypot(next.x,next.z)<140&&!obstacles.some(o=>distance(next,o)<o.r+.4))player.position.copy(next);player.rotation.y=Math.atan2(direction.x,direction.z);}}
 if(Math.hypot(chassis.position.x,chassis.position.z)>142){chassis.position.set(0,1.2,0);chassis.velocity.setZero();chassis.angularVelocity.setZero();chassis.quaternion.set(0,0,0,1);state.damage(20,true);note('Багги эвакуирован из воды. Потеря прочности: 20.');}
 for(const e of enemies){const hit=enemyStep(e,target(),dt,state.driving);if(hit&&state.alive)state.damage(hit.amount,hit.vehicle);e.mesh.position.set(e.x,e.mutant?1.6:1,e.z);e.mesh.lookAt(target().x,e.mesh.position.y,target().z);}
 if(state.updateRace(chassis.position))note(state.race.completed?'Финиш! Получены ресурсы.':'Контрольная точка пройдена.');}
function animate(now){const dt=Math.min((now-previous)/1000,.1);previous=now;accumulator+=dt;while(accumulator>=step){simulate(step);accumulator-=step;}
 buggy.body.position.copy(chassis.position);buggy.body.quaternion.copy(chassis.quaternion);for(let i=0;i<4;i++){const contact=vehicle.wheelInfos[i].isInContact;vehicle.updateWheelTransform(i);vehicle.wheelInfos[i].isInContact=contact;buggy.wheels[i].position.copy(vehicle.wheelInfos[i].worldTransform.position);buggy.wheels[i].quaternion.copy(vehicle.wheelInfos[i].worldTransform.quaternion);}player.visible=!state.driving;
 const p=target(),q=new T.Quaternion().copy(chassis.quaternion);const offset=state.driving?new T.Vector3(0,4.3,8).applyQuaternion(q):new T.Vector3(0,5,8);const desired=new T.Vector3(p.x,1,p.z).add(offset);desired.y=Math.max(desired.y,2);camera.position.lerp(desired,1-Math.exp(-dt*5));camera.lookAt(p.x,1,p.z);
 gates.forEach((g,i)=>{g.visible=!state.race.active||i>=state.race.index;g.scale.setScalar(state.race.active&&i===state.race.index?1.2:1);});
 const interact=document.querySelector('[data-action=KeyE]');interact.textContent=state.driving?'Выйти · E':'Сесть в багги · E';
 document.querySelector('#hint').textContent=!state.alive?'Вы погибли. Enter — новая игра.':!state.driving?'Сначала сядьте: E или кнопка «Сесть в багги».':state.fuel<=0?'Нет топлива. G — заправка в лагере.':state.integrity<=0?'Багги сломан. F — ремонт в лагере.':'За рулём: W / ↑ — газ, A D / ← → — руль, S / ↓ — задний ход, Space — тормоз.';
 document.querySelector('#status').innerHTML=`${state.driving?'БАГГИ · AWD':'ПЕШКОМ'}<br>Скорость <b>${Math.round(chassis.velocity.length()*3.6)}</b> км/ч<br>Здоровье ${state.health.toFixed(0)}%<br>Сытость ${state.hunger.toFixed(0)}%<br>Прочность ${state.integrity.toFixed(0)}%<br>Топливо ${state.fuel.toFixed(1)} / 45 л<br>Запчасти ${state.scrap} · Канистры ${state.canisters}<br>Еда ${state.food}`;
 document.querySelector('#race').textContent=state.race.active?`МАРШРУТ ${state.race.index+1}/6 · ${state.race.elapsed.toFixed(1)} с · БЕЗ ЛИМИТА`:state.race.completed?'МАРШРУТ ЗАВЕРШЁН · R — повторить':'R — необязательная гонка';
 if(!state.alive)note('Вы погибли. Enter — новая игра.');else if(noteTimer>0){noteTimer-=dt;if(noteTimer<=0)document.querySelector('#message').style.display='none';}renderer.render(scene,camera);requestAnimationFrame(animate);}
camera.position.set(0,8,12);requestAnimationFrame(animate);
// Read-only diagnostics for local smoke checks; no host integration.
window.cannibalRally={snapshot:()=>({speed:chassis.velocity.length(),health:state.health,driving:state.driving,fuel:state.fuel,integrity:state.integrity,position:{x:chassis.position.x,y:chassis.position.y,z:chassis.position.z},wheels:vehicle.wheelInfos.map(w=>({contact:w.isInContact,suspension:w.suspensionLength})),enemies:enemies.length}),renderer:()=>renderer.info.render.calls};
