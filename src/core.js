export const clamp=(x,a,b)=>Math.min(b,Math.max(a,x));
export const distance=(a,b)=>Math.hypot(a.x-b.x,a.z-b.z);
export const checkpoints=[{x:0,z:-45},{x:55,z:-65},{x:85,z:5},{x:35,z:70},{x:-45,z:50},{x:-55,z:-20}];
export class Survival {
  constructor(){this.health=100;this.hunger=100;this.fuel=45;this.integrity=100;this.scrap=5;this.food=3;this.canisters=3;this.driving=false;this.race={active:false,index:0,elapsed:0,completed:false};}
  get alive(){return this.health>0;}
  get engineAvailable(){return this.alive&&this.driving&&this.fuel>0&&this.integrity>0;}
  tick(dt,throttle=0){if(!this.alive)return;this.hunger=clamp(this.hunger-dt*.07,0,100);if(this.hunger===0)this.health=clamp(this.health-dt*.5,0,100);if(this.engineAvailable)this.fuel=clamp(this.fuel-dt*(.008+Math.abs(throttle)*.04),0,45);if(this.race.active)this.race.elapsed+=dt;}
  damage(amount,vehicle=false){if(!Number.isFinite(amount)||amount<0)return;if(vehicle)this.integrity=clamp(this.integrity-amount,0,100);else this.health=clamp(this.health-amount,0,100);}
  interact(player,car,speed){if(!this.alive)return 'Вы погибли. Enter — новая игра.';if(this.driving){if(Math.abs(speed)>1)return 'Остановитесь перед выходом.';this.driving=false;return 'Вы вышли из багги.';}if(distance(player,car)>4)return 'Подойдите к багги.';this.driving=true;return 'Полный привод включён.';}
  service(kind,position,speed=0,carPosition=position){if(!this.alive)return 'Вы погибли.';if(distance(position,{x:0,z:8})>12)return 'Вернитесь в лагерь.';if(distance(carPosition,{x:0,z:8})>12||distance(position,carPosition)>5)return 'Подведите багги в лагерь и подойдите к нему.';if(Math.abs(speed)>1)return 'Остановитесь для обслуживания.';if(kind==='repair'&&this.scrap>0&&this.integrity<100){this.scrap--;this.integrity=clamp(this.integrity+35,0,100);return 'Багги отремонтирован.';}if(kind==='fuel'&&this.canisters>0&&this.fuel<45){this.canisters--;this.fuel=clamp(this.fuel+15,0,45);return 'Добавлено 15 л топлива.';}return 'Нет ресурсов или обслуживание не требуется.';}
  eat(){if(!this.alive||this.food<=0||this.hunger>=100)return false;this.food--;this.hunger=clamp(this.hunger+35,0,100);return true;}
  startRace(){if(!this.alive||!this.driving)return false;this.race={active:true,index:0,elapsed:0,completed:false};return true;}
  updateRace(position){if(!this.race.active||!this.driving||!this.alive)return false;if(distance(position,checkpoints[this.race.index])>7)return false;this.race.index++;if(this.race.index===checkpoints.length){this.race.active=false;this.race.completed=true;this.scrap+=2;this.canisters++;this.food++;}return true;}
}
export function enemyStep(enemy,target,dt,driving){const d=distance(enemy,target);enemy.cooldown=Math.max(0,enemy.cooldown-dt);if(d<enemy.sight&&d>2){enemy.x+=(target.x-enemy.x)/d*enemy.speed*dt;enemy.z+=(target.z-enemy.z)/d*enemy.speed*dt;}if(d<3&&enemy.cooldown===0){enemy.cooldown=1.2;return {amount:enemy.damage,vehicle:driving};}return null;}
// Explicitly unavailable: no undocumented Melty calls or simulated success.
export function meltyCompatibility(){return {available:false,reason:'Melty SDK and The Forest host contract not supplied or verified.'};}
