'use strict';
(()=>{
const byId=id=>document.getElementById(id),layer=byId('mobile-layer'),toggle=byId('mobile-toggle');
let enabled=matchMedia('(pointer: coarse)').matches&&navigator.maxTouchPoints>0;
let state=null,tab='aim',lastPortrait=false,heartbeat=null,lastBoundary='';
const held=new Map(),axes=['x','y','yaw','pitch','power','orbitX','orbitY','zoom'];
const holdButtons=Array.from(document.querySelectorAll('[data-hold]')),actionButtons=Array.from(document.querySelectorAll('[data-action]')),tabButtons=Array.from(document.querySelectorAll('[data-tab]'));
const combatActions=new Set(['fire','basic','special']),cameraActions=new Set(['viewOwn','viewEnemy']);
// Keep pointer-operated DOM controls from stealing the game's keyboard focus.
// Keyboard activation remains available because its click has no pointerdown.
for(const button of [...actionButtons,...tabButtons,toggle])button.addEventListener('pointerdown',event=>event.preventDefault());
function send(method,value){if(instance&&!quitting)instance.SendMessage('Mobile web controls',method,value===undefined?'':value);}
function portrait(){return window.innerHeight>window.innerWidth;}
function operational(){return enabled&&state&&state.ready&&!state.main&&!state.loading&&!state.paused&&!state.confirm&&!state.ended&&!portrait();}
function canHold(axis){return operational()&&(axis.startsWith('orbit')||axis==='zoom'?(state.cameraEditable===undefined?state.editable:state.cameraEditable):state.editable);}
function clearInput(notify=true){for(const entry of held.values())entry.button.classList.remove('held');held.clear();if(heartbeat!==null){clearInterval(heartbeat);heartbeat=null;}if(notify)send('ClearInput');}
function emitHeld(){if(!held.size)return;const values={x:0,y:0,yaw:0,pitch:0,power:0,orbitX:0,orbitY:0,zoom:0,epoch:state?state.inputEpoch:0};for(const entry of held.values()){if(!canHold(entry.axis)){clearInput();return;}values[entry.axis]=Math.max(-1,Math.min(1,values[entry.axis]+entry.value));}send('Hold',JSON.stringify(values));}
function holdStart(event,button){event.preventDefault();const [axis,raw]=button.dataset.hold.split(':');if(button.disabled||!axes.includes(axis)||!canHold(axis))return;held.set(event.pointerId,{axis,value:Number(raw),button});button.classList.add('held');if(button.setPointerCapture)button.setPointerCapture(event.pointerId);emitHeld();if(heartbeat===null)heartbeat=setInterval(emitHeld,80);}
function release(event){const entry=held.get(event.pointerId);if(!entry)return;held.delete(event.pointerId);if(!Array.from(held.values()).some(other=>other.button===entry.button))entry.button.classList.remove('held');if(held.size)emitHeld();else clearInput();}
for(const button of holdButtons){button.addEventListener('pointerdown',event=>holdStart(event,button));for(const type of ['pointerup','pointercancel','lostpointercapture'])button.addEventListener(type,release);button.addEventListener('contextmenu',event=>event.preventDefault());}
window.addEventListener('pointerup',release);window.addEventListener('pointercancel',release);
function actionAllowed(action){if(!enabled||!state||!state.ready||state.loading||portrait())return false;if(combatActions.has(action))return operational()&&state.editable&&(action!=='special'||state.specialRemaining>0&&!!state.specialName);if(cameraActions.has(action))return operational()&&(state.cameraEditable===undefined?state.editable:state.cameraEditable);if(['nextPlayer','nextOpponent','startAi','startLocal'].includes(action))return state.main&&!state.confirm;if(action==='resume')return state.paused&&!state.confirm;if(action==='confirm'||action==='cancel')return state.confirm;if(action==='restart')return state.ended;if(action==='main')return !state.main;return false;}
for(const button of actionButtons)button.addEventListener('click',()=>{const action=button.dataset.action;if(button.disabled||!actionAllowed(action))return;clearInput();send('Action',action);});
for(const button of tabButtons)button.addEventListener('click',()=>{clearInput();tab=button.dataset.tab;render();});
function text(id,value){byId(id).textContent=value==null?'':String(value);}
function rounded(value,digits=0){return Number.isFinite(Number(value))?Number(value).toFixed(digits):'—';}
function render(){document.documentElement.classList.toggle('mobile-mode',enabled);toggle.setAttribute('aria-pressed',String(enabled));byId('mobile').hidden=!enabled;layer.hidden=!enabled||!instance||quitting;
 const s=state,active=enabled&&s&&s.enabled!==false&&!!instance&&!quitting,isPortrait=enabled&&portrait();
 byId('mobile-rotate').hidden=!active||!isPortrait;
 byId('mobile-loading').hidden=!enabled||!instance||quitting||isPortrait||!!(s&&s.ready&&!s.loading);
 const ready=active&&s.ready&&!s.loading&&!isPortrait;
 byId('mobile-main').hidden=!(ready&&s.main&&!s.confirm);
 byId('mobile-confirm-panel').hidden=!(ready&&s.confirm);
 byId('mobile-result-panel').hidden=!(ready&&!s.main&&s.ended&&!s.confirm&&!s.paused);
 byId('mobile-pause-panel').hidden=!(ready&&!s.main&&s.paused&&!s.confirm);
 const combat=ready&&!s.main&&!s.paused&&!s.confirm&&!s.ended;
 byId('mobile-hud').hidden=!combat;byId('mobile-dock').hidden=!combat;
 for(const button of tabButtons){const selected=button.dataset.tab===tab;button.setAttribute('aria-selected',String(selected));button.setAttribute('aria-pressed',String(selected));byId('page-'+button.dataset.tab).hidden=!selected;}
 for(const button of holdButtons)button.disabled=!canHold(button.dataset.hold.split(':')[0]);
 for(const button of actionButtons)button.disabled=!actionAllowed(button.dataset.action);
 if(!s)return;
 text('mobile-player',(s.roleName||'내 전차')+' 변경');text('mobile-opponent',(s.opponentName||'상대 전차')+' 변경');text('mobile-role-description',(s.roleDescription||'').replace(/<[^>]*>/g,''));
 const hp=Array.isArray(s.hp)?s.hp:[],max=Array.isArray(s.maxHp)?s.maxHp:[];
 text('mobile-own-hp','아군 '+rounded(hp[0])+' / '+rounded(max[0]));text('mobile-enemy-hp','상대 '+rounded(hp[1])+' / '+rounded(max[1]));
 text('mobile-turn-text',(s.turn||1)+'턴 · '+(s.aiTurn?'AI 차례':s.editable?'조작 가능':'포격 진행 중'));
 text('mobile-wind',s.wind||'바람 —');text('mobile-move','이동 '+rounded(s.move,1)+'m');text('mobile-move-detail','남은 거리 '+rounded(s.move,1)+'m');
 text('mobile-aim','각도 '+rounded(s.pitch,1)+'° · 위력 '+rounded(s.power,1));
 text('mobile-ammo-basic',s.basicName||'일반탄');text('mobile-ammo-special',s.specialName?(s.specialName+' '+(s.specialRemaining||0)+'발'):'특수탄 없음');
 byId('mobile-ammo-basic').setAttribute('aria-pressed',String(!s.specialSelected));byId('mobile-ammo-special').setAttribute('aria-pressed',String(!!s.specialSelected));
 text('mobile-confirm-title',s.confirmationTitle||'확인');text('mobile-confirm-body',s.confirmationBody||'');text('mobile-result-text',s.resultText||'대전 종료');
}
function configure(){clearInput();send('Configure',enabled?'1':'0');render();if(enabled&&portrait()&&instance)pauseGame();}
toggle.addEventListener('click',()=>{enabled=!enabled;state=null;lastBoundary='';configure();});
window.addEventListener('fortress-game-ready',configure);window.addEventListener('fortress-game-ended',()=>{clearInput();state=null;render();});
window.addEventListener('fortress-mobile-state',event=>{let next=event.detail;try{if(typeof next==='string')next=JSON.parse(next);}catch{return;}if(!next||typeof next!=='object')return;const boundary=JSON.stringify([next.inputEpoch,next.generation,next.turn,next.main,next.loading,next.paused,next.confirm,next.ended,next.enabled]);if(lastBoundary&&boundary!==lastBoundary)clearInput(false);lastBoundary=boundary;state=next;render();});
function resized(){const nextPortrait=portrait();if(nextPortrait!==lastPortrait){clearInput();if(enabled&&nextPortrait&&instance)pauseGame();lastPortrait=nextPortrait;}render();}
window.addEventListener('resize',resized);window.addEventListener('orientationchange',()=>{clearInput();if(enabled&&instance)pauseGame();resized();});
window.addEventListener('blur',clearInput);window.addEventListener('fortress-clear-input',clearInput);document.addEventListener('visibilitychange',()=>{if(document.hidden)clearInput();});
lastPortrait=portrait();render();
})();
