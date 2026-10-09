const aliases={ArrowUp:'KeyW',ArrowDown:'KeyS',ArrowLeft:'KeyA',ArrowRight:'KeyD'};
const movement=new Set(['KeyW','KeyA','KeyS','KeyD','Space']);
const actions=new Set(['KeyE','KeyF','KeyG','KeyH','KeyR','Enter']);
export function bindInput(canvas,onAction){
 const keyboard=new Set(),pointers=new Map();
 const keys={has:code=>keyboard.has(code)||[...pointers.values()].includes(code)};
 const clear=()=>{keyboard.clear();pointers.clear();document.querySelectorAll('[data-drive]').forEach(b=>b.classList.remove('held'));};
 const focus=()=>canvas.focus({preventScroll:true});
 canvas.addEventListener('pointerdown',focus);
 window.addEventListener('keydown',e=>{
  if(e.ctrlKey||e.metaKey||e.altKey||e.target.closest?.('input,textarea,select,[contenteditable="true"]'))return;
  if(e.target.closest?.('button')&&(e.code==='Enter'||e.code==='Space'))return;
  const code=aliases[e.code]||e.code;
  if(!movement.has(code)&&!actions.has(code))return;
  e.preventDefault();
  if(movement.has(code))keyboard.add(code);
  else if(!e.repeat)onAction(code);
 });
 window.addEventListener('keyup',e=>keyboard.delete(aliases[e.code]||e.code));
 window.addEventListener('blur',clear);
 document.addEventListener('visibilitychange',()=>{if(document.hidden)clear();});
 document.querySelectorAll('[data-drive]').forEach(button=>{
  button.addEventListener('pointerdown',e=>{e.preventDefault();focus();button.setPointerCapture(e.pointerId);pointers.set(e.pointerId,button.dataset.drive);button.classList.add('held');});
  const release=e=>{pointers.delete(e.pointerId);if(![...pointers.values()].includes(button.dataset.drive))button.classList.remove('held');};
  button.addEventListener('pointerup',release);button.addEventListener('pointercancel',release);button.addEventListener('lostpointercapture',release);
 });
 document.querySelectorAll('[data-action]').forEach(button=>button.addEventListener('click',()=>{onAction(button.dataset.action);focus();}));
 return {keys};
}
