import {mountLattice} from './shader.js';
const Q=new URLSearchParams(location.search);
const st={s:Q.get('s')||localStorage.rs||'welcome',theme:Q.get('theme')||localStorage.rt||'dark',lang:Q.get('lang')||localStorage.rl||'en',mode:'find',dl:Q.has('fail')?'fail':(Q.get('dl')||'run'),pct:+(Q.get('pct')||62)};
const T={en:{
brand:'Recall',local:'On this device',egress:'0 KB sent',
w_k:'Private memory engine · v1',w_h:'Everything you saw, said and wrote —<br>found in a sentence.',w_p:'Recall indexes the folders you choose and answers in plain language. Photos, voice notes, documents and code stay on this computer. Nothing is uploaded, ever.',
w_b1:'Choose folders',w_b2:'How it stays private',w_f:['No account','No cloud copy','Works offline','Delete anytime'],
step:'Step',of:'of',f_h:'Pick what Recall may read',f_p:'Only these folders are indexed. You can add or remove them later in Preferences.',f_add:'Add folder',f_items:'items',f_skip:'Skipped: system folders, other apps, anything you don’t pick.',next:'Continue',back:'Back',
m_h:'Download the on-device models',m_p:'One-time download. After this, search works without internet.',m_vis:'Vision + text embeddings',m_txt:'Language model',m_aud:'Speech-to-text',m_desk:'Desktop only',m_ok:'Verified',m_run:'Downloading',m_wait:'Queued',m_fail:'Checksum didn’t match — file discarded',m_retry:'Retry download',m_why:'We check every file against a SHA-256 hash before using it.',
r_h:'Your memory is ready',r_p:'12,480 items indexed from 3 folders. Indexing continues quietly in the background.',r_b:'Start searching',
nav:['Search','Lattice','Folders','Models','Preferences'],vault:'Personal vault',
q:'The red scooter I saw near the Colosseum last summer',modes:['Find','Group','Tag'],ctx:'Understood as',ctxs:['Rome, Italy','Voice memo','Vespa','Summer 2024'],
res:'Results',resn:'6 matches across 4 kinds',sort:'Best match',
img:'Photo',aud:'Voice memo',doc:'Document',code:'Code',vid:'Video',
c1t:'Red Vespa, Piazza del Colosseo',c1m:'Rome · 14 Jul 2024, 16:42',c1n:'“Remember to check this engine model later.”',match:'match',
c2t:'Voice memo · <bdi class="ltr">0:30</bdi>',c2q:'“Vespa rossa vicino al Colosseo, modello anni ’70 ben restaurato…”',c2tr:'Translated: “Red Vespa near the Colosseum, restored ’70s model.”',
c3t:'Scooter parts list.pdf',c3m:'Page 3 · “carburettor, 50cc”',
c4t:'Video frames · 3 moments',
un:'File unavailable',unp:'Moved or on a disconnected drive. Kept as a placeholder.',unb:'Locate',
au:'Audio search runs on desktop',aup:'2 voice memos weren’t searched on this phone.',
insp:'Details',why:'Why this matched',whyl:['Image: scooter, red, arch','Place: within 200 m','Voice memo on same day'],
open:'Open file',show:'Show in folder',stored:'Stored locally · never uploaded',
s_items:'items',s_idx:'Indexing paused while on battery',s_ready:'Ready',
lat:'Lattice',lat_p:'How your memories connect',
sync:'Device sync isn’t in this version.',
th:'Theme',ondev:'On-device',file:'File',size:'Size',lg:'العربية'},
ar:{
brand:'ريكول',local:'على هذا الجهاز',egress:'٠ ك.ب مُرسَلة',
w_k:'محرّك ذاكرة خاص · الإصدار ١',w_h:'كل ما رأيته وقلته وكتبته —<br>تجده بجملة واحدة.',w_p:'يفهرس ريكول المجلدات التي تختارها ويجيبك بلغة عادية. الصور والملاحظات الصوتية والمستندات والكود تبقى على هذا الكمبيوتر. لا يُرفع أي شيء، أبدًا.',
w_b1:'اختر المجلدات',w_b2:'كيف تبقى بياناتك خاصة',w_f:['بلا حساب','بلا نسخة سحابية','يعمل دون إنترنت','احذف متى شئت'],
step:'الخطوة',of:'من',f_h:'اختر ما يُسمح لريكول بقراءته',f_p:'تُفهرس هذه المجلدات فقط. يمكنك الإضافة أو الإزالة لاحقًا من التفضيلات.',f_add:'إضافة مجلد',f_items:'عنصر',f_skip:'مُستثنى: مجلدات النظام والتطبيقات الأخرى وكل ما لا تختاره.',next:'متابعة',back:'رجوع',
m_h:'نزّل النماذج على جهازك',m_p:'تنزيل لمرة واحدة. بعدها يعمل البحث دون إنترنت.',m_vis:'تضمينات الصور والنصوص',m_txt:'نموذج اللغة',m_aud:'تحويل الكلام إلى نص',m_desk:'للكمبيوتر فقط',m_ok:'تم التحقق',m_run:'جارٍ التنزيل',m_wait:'في الانتظار',m_fail:'البصمة غير مطابقة — حُذف الملف',m_retry:'أعد التنزيل',m_why:'نتحقق من كل ملف ببصمة SHA-256 قبل استخدامه.',
r_h:'ذاكرتك جاهزة',r_p:'فُهرس ١٢٬٤٨٠ عنصرًا من ٣ مجلدات. تستمر الفهرسة بهدوء في الخلفية.',r_b:'ابدأ البحث',
nav:['البحث','الشبكة','المجلدات','النماذج','التفضيلات'],vault:'الخزنة الشخصية',
q:'السكوتر الأحمر الذي رأيته قرب الكولوسيوم الصيف الماضي',modes:['ابحث','جمّع','وسِّم'],ctx:'فُهم على أنه',ctxs:['روما، إيطاليا','ملاحظة صوتية','فيسبا','صيف ٢٠٢٤'],
res:'النتائج',resn:'٦ نتائج من ٤ أنواع',sort:'الأقرب',
img:'صورة',aud:'ملاحظة صوتية',doc:'مستند',code:'كود',vid:'فيديو',
c1t:'فيسبا حمراء، ساحة الكولوسيوم',c1m:'روما · ١٤ يوليو ٢٠٢٤، ١٦:٤٢',c1n:'«تذكّر أن تتحقق من طراز المحرك لاحقًا.»',match:'تطابق',
c2t:'ملاحظة صوتية · ٣٠ ثانية',c2q:'“Vespa rossa vicino al Colosseo, modello anni ’70 ben restaurato…”',c2tr:'مترجم: «فيسبا حمراء قرب الكولوسيوم، طراز السبعينيات مُرمَّم.»',
c3t:'قائمة قطع السكوتر<bdi class="ltr">.pdf</bdi>',c3m:'صفحة ٣ · «كاربراتير، ٥٠ سي سي»',
c4t:'لقطات فيديو · ٣ لحظات',
un:'الملف غير متاح',unp:'نُقل أو على قرص غير متصل. يبقى كعنصر نائب.',unb:'حدّد موقعه',
au:'البحث الصوتي على الكمبيوتر',aup:'لم يُبحث في ملاحظتين صوتيتين على هذا الهاتف.',
insp:'التفاصيل',why:'لماذا ظهرت هذه النتيجة',whyl:['الصورة: سكوتر، أحمر، قوس','المكان: ضمن ٢٠٠ م','ملاحظة صوتية في اليوم نفسه'],
open:'افتح الملف',show:'اعرض في المجلد',stored:'مخزّن محليًا · لا يُرفع أبدًا',
s_items:'عنصر',s_idx:'الفهرسة متوقفة أثناء العمل على البطارية',s_ready:'جاهز',
lat:'الشبكة',lat_p:'كيف تترابط ذكرياتك',
sync:'المزامنة بين الأجهزة ليست في هذا الإصدار.',
th:'المظهر',ondev:'على الجهاز',file:'الملف',size:'الحجم',lg:'English'}};
const t=k=>T[st.lang][k];
const I={ // 1.6px stroke icons
search:'<path d="M11 18a7 7 0 1 0 0-14 7 7 0 0 0 0 14Zm9 3-4.3-4.3"/>',lat:'<circle cx="5" cy="6" r="2"/><circle cx="19" cy="6" r="2"/><circle cx="12" cy="18" r="2"/><path d="M7 6h10M6 8l5 8m2 0 5-8"/>',
folder:'<path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z"/>',chip:'<rect x="6" y="6" width="12" height="12" rx="2"/><path d="M9 2v4m6-4v4M9 18v4m6-4v4M2 9h4m-4 6h4m12-6h4m-4 6h4"/>',
gear:'<circle cx="12" cy="12" r="3"/><path d="M12 2v3m0 14v3M2 12h3m14 0h3M4.9 4.9 7 7m10 10 2.1 2.1M4.9 19.1 7 17m10-10 2.1-2.1"/>',
shield:'<path d="M12 3 4 6v6c0 4.5 3.4 8.3 8 9 4.6-.7 8-4.5 8-9V6Z"/><path d="m9 12 2 2 4-4"/>',check:'<path d="m5 12 5 5L20 7"/>',x:'<path d="M6 6l12 12M18 6 6 18"/>',
arrow:'<path d="M5 12h14m-6-6 6 6-6 6"/>',sun:'<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.4 1.4M17.6 17.6 19 19M5 19l1.4-1.4M17.6 6.4 19 5"/>',moon:'<path d="M20 15A8 8 0 1 1 9 4a7 7 0 0 0 11 11Z"/>',
globe:'<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c3 3.5 3 14.5 0 18M12 3c-3 3.5-3 14.5 0 18"/>',mic:'<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11a7 7 0 0 0 14 0M12 18v3"/>',
img:'<rect x="3" y="4" width="18" height="16" rx="2"/><circle cx="9" cy="10" r="2"/><path d="m21 16-5-5-9 9"/>',doc:'<path d="M6 3h8l4 4v14H6Z"/><path d="M14 3v4h4M9 13h6m-6 4h6"/>',
play:'<path d="M8 5v14l11-7Z"/>',alert:'<path d="M12 9v4m0 4h.01M10.3 3.9 2 18a2 2 0 0 0 1.7 3h16.6A2 2 0 0 0 22 18L13.7 3.9a2 2 0 0 0-3.4 0Z"/>',
retry:'<path d="M4 12a8 8 0 0 1 14-5.3L20 9M20 4v5h-5M20 12a8 8 0 0 1-14 5.3L4 15m0 5v-5h5"/>',plus:'<path d="M12 5v14M5 12h14"/>',lock:'<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V7a4 4 0 0 1 8 0v4"/>',
code:'<path d="m8 8-4 4 4 4m8-8 4 4-4 4"/>',film:'<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M7 4v16M17 4v16M3 9h4m10 0h4M3 15h4m10 0h4"/>',menu:'<path d="M4 7h16M4 12h16M4 17h16"/>'};
const ic=(n,c='w-[18px] h-[18px]')=>`<svg class="${c} shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${I[n]}</svg>`;
const N=n=>st.lang==='ar'?String(n).replace(/\d/g,d=>'٠١٢٣٤٥٦٧٨٩'[d]):n;
const logo=`<span class="w-7 h-7 rounded-lg grid place-items-center" style="background:color-mix(in srgb,var(--cyan) 16%,transparent);color:var(--cyan-ink);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--cyan) 40%,transparent)">${ic('lat','w-4 h-4')}</span>`;
const toggles=()=>`<button data-a="theme" class="w-9 h-9 grid place-items-center rounded-lg btn-g ink2" aria-label="${t('th')}">${ic(st.theme==='dark'?'sun':'moon')}</button>
<button data-a="lang" class="h-9 px-3 rounded-lg btn-g ink2 text-[13px] flex items-center gap-2">${ic('globe','w-4 h-4')}<span>${t('lg')}</span></button>`;
const privacy=`<span class="pill f-mono text-[11px] em" style="background:var(--emerald-bg);border-color:transparent">${ic('shield','w-3.5 h-3.5')}${t('local')} · <bdi>${t('egress')}</bdi></span>`;

/* ---------- onboarding ---------- */
function frame(stepN,body){return `<header class="h-header shrink-0 flex items-center justify-between px-5 md:px-8">
<div class="flex items-center gap-2.5">${logo}<span class="f-head font-semibold text-[16px]">${t('brand')}</span></div>
<div class="flex items-center gap-2">${stepN?`<span class="lbl hidden sm:inline">${t('step')} ${N(stepN)} ${t('of')} ${N(3)}</span>`:''}${toggles()}</div></header>
<main class="flex-1 overflow-auto grid place-items-center px-4 py-6">${body}</main>`}
function welcome(){return frame(0,`<section class="max-w-[760px] w-full text-center rise">
<div class="inline-flex mb-6">${privacy}</div>
<p class="lbl mb-4">${t('w_k')}</p>
<h1 class="f-head font-bold text-[34px] sm:text-[48px] md:text-[60px] leading-[1.05] mb-5">${t('w_h').replace('—<br>','—<br><span class="cy">').replace(/(.)$/,'$1')}</span></h1>
<p class="ink2 text-[16px] md:text-[17px] max-w-[560px] mx-auto mb-8">${t('w_p')}</p>
<div class="flex flex-col sm:flex-row gap-3 justify-center mb-10"><button data-go="folders" class="btn btn-p justify-center">${t('w_b1')}${ic('arrow','w-4 h-4 flip')}</button><a href="landing.html" class="btn btn-g justify-center">${ic('lock','w-4 h-4')}${t('w_b2')}</a></div>
<ul class="g2 rounded-2xl grid grid-cols-2 sm:grid-cols-4 text-[13px]">${t('w_f').map((f,i)=>`<li class="flex items-center gap-2 justify-center py-3.5 px-3 ${i?'sm:border-s bd':''} ${i>1?'border-t sm:border-t-0 bd':''}"><span class="em">${ic('check','w-4 h-4')}</span>${f}</li>`).join('')}</ul>
</section>`)}
function stepper(n){return `<ol class="flex gap-2 mb-6" aria-label="progress">${[1,2,3].map(i=>`<li class="h-1 flex-1 rounded-full" style="background:${i<=n?'var(--cyan-ink)':'var(--c3)'}"></li>`).join('')}</ol>`}
function folders(){const F=[['Pictures','C:\\Users\\Ahmed\\Pictures','8,214',1,'img'],['Documents','C:\\Users\\Ahmed\\Documents','3,902',1,'doc'],['Voice Memos','D:\\Recordings\\Voice','364',1,'mic'],['Projects','D:\\Code\\Projects','—',0,'code']];
return frame(1,`<section class="g2 rounded-2xl w-full max-w-[640px] p-5 sm:p-7 rise">${stepper(1)}
<h2 class="f-head font-semibold text-[24px] sm:text-[28px] mb-2">${t('f_h')}</h2><p class="ink2 mb-6">${t('f_p')}</p>
<ul class="flex flex-col gap-2 mb-4">${F.map(([n,p,c,on,i])=>`<li><label class="flex items-center gap-3 p-3 rounded-xl border bd hover:bd2 cursor-pointer" style="background:var(--c1)">
<span class="w-9 h-9 rounded-lg grid place-items-center ink2" style="background:var(--c2)">${ic(i)}</span>
<span class="flex-1 min-w-0"><span class="block font-medium">${n}</span><bdi class="ltr block f-mono text-[12px] ink3 truncate text-start">${p}</bdi></span>
<span class="f-mono text-[12px] ink2 hidden sm:block">${on?N(c)+' '+t('f_items'):''}</span>
<input type="checkbox" ${on?'checked':''} class="w-5 h-5 accent-[var(--cyan-ink)]"></label></li>`).join('')}</ul>
<button class="btn btn-g w-full justify-center mb-4 border-dashed">${ic('plus','w-4 h-4')}${t('f_add')}</button>
<p class="text-[13px] ink3 flex gap-2 mb-6">${ic('shield','w-4 h-4 mt-0.5')}${t('f_skip')}</p>
<div class="flex justify-between gap-3"><button data-go="welcome" class="btn btn-g">${t('back')}</button><button data-go="model" class="btn btn-p">${t('next')}${ic('arrow','w-4 h-4 flip')}</button></div></section>`)}
function model(){const fail=st.dl==='fail';const M=[[t('m_vis'),'SigLIP-2 · 412 MB','ok',100],[t('m_txt'),'Gemma-2 · 1.6 GB',fail?'fail':'run',st.pct],[t('m_aud'),'Whisper-small · 244 MB','wait',0,1]];
return frame(2,`<section class="g2 rounded-2xl w-full max-w-[640px] p-5 sm:p-7 rise">${stepper(2)}
<h2 class="f-head font-semibold text-[24px] sm:text-[28px] mb-2">${t('m_h')}</h2><p class="ink2 mb-6">${t('m_p')}</p>
<ul class="flex flex-col gap-3 mb-5">${M.map(([n,s,state,p,desk])=>`<li class="p-4 rounded-xl border ${state==='fail'?'':'bd'}" style="background:var(--c1);${state==='fail'?'border-color:color-mix(in srgb,var(--err) 50%,transparent)':''}">
<div class="flex items-center gap-3 mb-3"><span class="flex-1 min-w-0"><span class="block font-medium">${n}</span><bdi class="ltr f-mono text-[12px] ink3">${s}</bdi>${desk?` <span class="pill text-[11px] ink2 ms-1">${t('m_desk')}</span>`:''}</span>
<span class="f-mono text-[12px] flex items-center gap-1.5 ${state==='ok'?'em':state==='fail'?'er':state==='run'?'cy':'ink3'}">${state==='ok'?ic('check','w-4 h-4')+t('m_ok'):state==='fail'?ic('alert','w-4 h-4')+t('m_fail').split(' — ')[0]:state==='run'?`${t('m_run')} <bdi class="ltr" data-pct>${N(p)}%</bdi>`:t('m_wait')}</span></div>
<div class="h-1.5 rounded-full overflow-hidden" style="background:var(--c3)"><div data-bar="${state}" class="h-full rounded-full transition-[width] duration-300" style="width:${state==='fail'?100:p}%;background:${state==='ok'?'var(--emerald)':state==='fail'?'var(--err)':'linear-gradient(90deg,var(--cyan-ink),var(--violet))'}"></div></div>
${state==='fail'?`<div class="flex flex-wrap items-center justify-between gap-3 mt-3"><p class="text-[13px] er">${t('m_fail')}</p><button data-a="retry" class="btn btn-p !py-2 text-[13px]">${ic('retry','w-4 h-4')}${t('m_retry')}</button></div>`:''}</li>`).join('')}</ul>
<p class="text-[13px] ink3 flex gap-2 mb-6">${ic('lock','w-4 h-4 mt-0.5')}${t('m_why')}</p>
<div class="flex justify-between gap-3"><button data-go="folders" class="btn btn-g">${t('back')}</button><button data-go="ready" class="btn btn-p" ${fail||st.pct<100?'disabled style="opacity:.45"':''} data-cont>${t('next')}${ic('arrow','w-4 h-4 flip')}</button></div></section>`)}
function ready(){return frame(3,`<section class="text-center max-w-[560px] rise">${stepper(3)}
<div class="w-16 h-16 rounded-2xl mx-auto grid place-items-center mb-6 em g3">${ic('check','w-8 h-8')}</div>
<h2 class="f-head font-bold text-[32px] sm:text-[40px] mb-3">${t('r_h')}</h2><p class="ink2 text-[16px] mb-8">${t('r_p')}</p>
<button data-go="home" class="btn btn-p">${ic('search','w-4 h-4')}${t('r_b')}</button></section>`)}

/* ---------- shell ---------- */
function shell(main,active){const navI=['search','lat','folder','chip','gear'],go=['home','lattice','folders','model','home'];
return `<header class="g1 h-header shrink-0 flex items-center gap-3 px-3 md:px-4 border-b bd">
<div class="flex items-center gap-2.5 md:w-[200px]">${logo}<span class="f-head font-semibold text-[15px]">${t('brand')}</span></div>
<div class="flex-1"></div><span class="hidden md:inline-flex">${privacy}</span>${toggles()}
<span class="w-8 h-8 rounded-full grid place-items-center text-[12px] font-semibold" style="background:var(--violet-bg);color:var(--violet)">AS</span></header>
<div class="flex flex-1 min-h-0">
<nav class="g1 hidden md:flex flex-col w-rail lg:w-railx shrink-0 border-e bd p-2 gap-1" aria-label="Main">
<div class="hidden lg:flex items-center gap-2 px-3 py-2.5 mb-2 rounded-xl border bd text-[13px] font-medium"><span class="w-2 h-2 rounded-full" style="background:var(--emerald)"></span>${t('vault')}</div>
${t('nav').map((n,i)=>`<button data-go="${go[i]}" class="flex items-center gap-3 h-10 px-3 rounded-xl text-[14px] ${i===active?'cy font-medium':'ink2 hover:bg-[var(--c2)]'}" style="${i===active?'background:color-mix(in srgb,var(--cyan) 11%,transparent);box-shadow:inset 0 0 0 1px color-mix(in srgb,var(--cyan) 28%,transparent)':''}" aria-current="${i===active}">${ic(navI[i])}<span class="hidden lg:inline">${n}</span></button>`).join('')}
<div class="flex-1"></div>
<div class="hidden lg:block p-3 rounded-xl border bd text-[12px]"><div class="flex justify-between mb-2"><span class="lbl !text-[10px]">${t('ondev')}</span><span class="f-mono em text-[11px]">${t('s_ready')}</span></div>
<div class="h-1 rounded-full mb-2" style="background:linear-gradient(90deg,var(--cyan-ink),var(--violet))"></div><bdi class="ltr f-mono ink3 text-[11px]">SigLIP-2 + Gemma-2</bdi></div></nav>
<main class="flex-1 min-w-0 overflow-auto pb-20 md:pb-0">${main}</main></div>
<footer class="g1 h-status shrink-0 hidden md:flex items-center gap-5 px-4 border-t bd f-mono text-[11px] ink2">
<span class="flex items-center gap-1.5"><span class="w-1.5 h-1.5 rounded-full" style="background:var(--emerald)"></span>${N('12,480')} ${t('s_items')}</span><span class="am">${t('s_idx')}</span><span class="flex-1"></span><bdi class="ltr">Gemma-2 · SigLIP-2</bdi><span class="em">${t('s_ready')}</span></footer>
<nav class="g1 md:hidden fixed bottom-0 inset-x-0 z-20 border-t bd grid grid-cols-4 pb-[env(safe-area-inset-bottom)]">${[0,1,2,4].map(i=>`<button data-go="${go[i]}" class="flex flex-col items-center gap-1 py-2.5 text-[11px] ${i===active?'cy':'ink2'}">${ic(navI[i])}${t('nav')[i]}</button>`).join('')}</nav>`}
function card(cls,inner){return `<article class="card g2 ${cls}">${inner}</article>`}
const kind=(k,c,i)=>`<span class="pill f-mono text-[11px] ${c}" style="border-color:transparent">${ic(i,'w-3.5 h-3.5')}${t(k)}</span>`;
function home(){const modes=['search','lat','doc'];
const wave=Array.from({length:48},(_,i)=>`<span class="w-[3px] rounded-full" style="height:${8+Math.abs(Math.sin(i*.7)*Math.cos(i*.23))*30}px;background:${i<20?'var(--violet)':'var(--c4)'}"></span>`).join('');
return shell(`<div class="px-3 md:px-6 pt-4 md:pt-6 max-w-[1500px] mx-auto">
<div class="g3 rounded-2xl p-2 flex flex-wrap sm:flex-nowrap items-center gap-2 mb-3">
<div class="flex p-1 rounded-xl" style="background:var(--c1)" role="tablist">${t('modes').map((m,i)=>`<button data-mode="${i}" role="tab" aria-selected="${i===0}" class="h-8 px-2.5 sm:px-3 rounded-lg text-[13px] font-medium flex items-center gap-1.5 ${i===0?'btn-p':'ink2'}">${ic(modes[i],'w-4 h-4 hidden sm:block')}${m}</button>`).join('')}</div>
<div class="order-first sm:order-none basis-full sm:basis-auto flex-1 min-w-0 text-[15px] md:text-[17px] font-medium sm:truncate px-2 py-1.5 sm:py-0 caret" id="q">${t('q')}</div>
<span class="hidden sm:inline-flex pill f-mono text-[11px] cy" style="border-color:transparent;background:color-mix(in srgb,var(--cyan) 10%,transparent)"><bdi class="ltr">38 ms</bdi></span>
<kbd class="hidden lg:inline-flex pill f-mono text-[11px] ink2"><bdi class="ltr">Ctrl K</bdi></kbd></div>
<div class="flex items-center gap-2 mb-6 overflow-x-auto pb-1"><span class="lbl shrink-0">${t('ctx')}</span>${t('ctxs').map((c,i)=>`<span class="pill shrink-0 text-[12px] ink" style="background:var(--c1)">${c}<button class="ink3 hover:ink" aria-label="remove">${ic('x','w-3 h-3')}</button></span>`).join('')}</div>
<div class="flex items-baseline justify-between mb-4"><h1 class="f-head font-semibold text-[20px]">${t('res')} <span class="ink2 text-[14px] font-normal ms-2">${t('resn')}</span></h1><span class="pill text-[12px] ink2">${t('sort')}</span></div>
<div class="flex gap-5"><div class="flex-1 min-w-0 grid grid-cols-1 md:grid-cols-6 xl:grid-cols-12 gap-4 pb-8">
${card('md:col-span-6 xl:col-span-7 overflow-hidden rise',`<div class="relative aspect-[16/10] md:aspect-[2/1] xl:aspect-[16/9]"><img src="img/s1.jpg" alt="" class="absolute inset-0 w-full h-full object-cover"><div class="absolute inset-0" style="background:linear-gradient(180deg,transparent 35%,rgba(8,10,14,.92))"></div>
<div class="absolute top-3 inset-x-3 flex gap-2">${kind('img','tag-img !bg-[rgba(0,30,34,.75)] !text-[#5ff8ff]','img')}<span class="pill f-mono text-[11px] text-white" style="background:rgba(0,0,0,.55);border-color:transparent"><bdi class="ltr">98%</bdi> ${t('match')}</span></div>
<div class="absolute bottom-0 inset-x-0 p-4 md:p-5 text-white"><p class="f-mono text-[12px] text-[#C7D2DC] mb-1">${t('c1m')}</p><h3 class="f-head font-semibold text-[20px] md:text-[24px]">${t('c1t')}</h3></div></div>
<div class="p-4 md:p-5 flex flex-wrap gap-3 items-center justify-between"><p class="ink2 italic border-s-2 ps-3" style="border-color:var(--cyan-ink)">${t('c1n')}</p><span class="text-[12px] ink3 flex items-center gap-1.5">${ic('lock','w-3.5 h-3.5')}${t('stored')}</span></div>`)}
${card('md:col-span-6 xl:col-span-5 p-4 md:p-5 flex flex-col gap-4 rise',`<div class="flex justify-between">${kind('aud','tag-aud','mic')}<bdi class="ltr f-mono text-[12px] ink2">00:14 / 00:30</bdi></div>
<h3 class="f-head font-semibold text-[18px]">${t('c2t')}</h3>
<div class="flex items-center gap-3 p-3 rounded-xl" style="background:var(--c1)"><button class="w-10 h-10 rounded-full grid place-items-center shrink-0" style="background:var(--violet-bg);color:var(--violet)" aria-label="play">${ic('play','w-4 h-4 flip')}</button><div class="flex items-center gap-[3px] h-10 overflow-hidden" dir="ltr">${wave}</div></div>
<p class="text-[15px]" dir="ltr" lang="it">${t('c2q').replace('Colosseo','<mark class="rounded px-1" style="background:color-mix(in srgb,var(--cyan) 22%,transparent);color:inherit">Colosseo</mark>')}</p>
<p class="text-[13px] ink2 border-t bd pt-3">${t('c2tr')}</p>`)}
${card('md:col-span-3 xl:col-span-4 p-4 flex flex-col gap-3 rise',`<span class="self-start">${kind('doc','tag-doc','doc')}</span><h3 class="font-semibold"><bdi>${t('c3t')}</bdi></h3><p class="text-[13px] ink2">${t('c3m')}</p><div class="mt-auto h-16 rounded-lg p-2 flex flex-col gap-1.5" style="background:var(--c1)">${[90,70,80,40].map(w=>`<span class="h-1.5 rounded" style="width:${w}%;background:var(--c3)"></span>`).join('')}</div>`)}
${card('md:col-span-3 xl:col-span-5 p-4 flex flex-col gap-3 rise',`<div class="flex justify-between">${kind('vid','tag-vid','film')}<span class="f-mono text-[11px] ink3">MP4</span></div><h3 class="font-semibold">${t('c4t')}</h3>
<div class="grid grid-cols-3 gap-2">${['s2','s3','s4'].map((s,i)=>`<div class="relative rounded-lg overflow-hidden aspect-video ${i===1?'ring-2 ring-[var(--cyan-ink)]':''}"><img src="img/${s}.jpg" alt="" class="w-full h-full object-cover"><bdi class="ltr absolute bottom-1 start-1 f-mono text-[10px] text-white px-1 rounded bg-black/60">02:${14+i*7}</bdi></div>`).join('')}</div>`)}
${card('md:col-span-3 xl:col-span-3 p-4 flex flex-col gap-2 border-dashed !border-[var(--line-2)] rise',`<span class="am">${ic('alert')}</span><h3 class="font-semibold">${t('un')}</h3><bdi class="ltr f-mono text-[11px] ink3 truncate block">E:\\Trips\\rome_014.heic</bdi><p class="text-[13px] ink2">${t('unp')}</p><button class="btn btn-g !py-1.5 text-[13px] self-start mt-1">${t('unb')}</button>`)}
${card('md:col-span-3 xl:col-span-12 p-4 flex items-center gap-3 md:hidden xl:flex rise',`<span class="vi">${ic('mic')}</span><div><h3 class="font-semibold text-[14px]">${t('au')}</h3><p class="text-[13px] ink2">${t('aup')}</p></div>`)}
</div>
<aside class="hidden 2xl:flex flex-col gap-4 w-insp shrink-0 g2 rounded-2xl p-5 self-start sticky top-0">
<p class="lbl">${t('insp')}</p><img src="img/s1.jpg" class="rounded-xl aspect-video object-cover" alt=""><h3 class="f-head font-semibold text-[17px]">${t('c1t')}</h3>
<div><p class="lbl mb-2">${t('why')}</p><ul class="flex flex-col gap-2 text-[13px]">${t('whyl').map((w,i)=>`<li class="flex gap-2"><span class="${['cy','em','vi'][i]}">${ic('check','w-4 h-4')}</span>${w}</li>`).join('')}</ul></div>
<dl class="grid grid-cols-2 gap-y-1.5 text-[12px] f-mono border-t bd pt-3"><dt class="ink3">${t('file')}</dt><dd class="text-end"><bdi class="ltr">IMG_2041.jpg</bdi></dd><dt class="ink3">${t('size')}</dt><dd class="text-end"><bdi class="ltr">4.2 MB</bdi></dd></dl>
<div class="flex gap-2"><button class="btn btn-p flex-1 justify-center !py-2 text-[13px]">${t('open')}</button><button class="btn btn-g flex-1 justify-center !py-2 text-[13px]">${t('show')}</button></div></aside></div></div>`,0)}
function lattice(){const P=[[50,50,'cy',14,'Colosseum'],[28,30,'vi',9,'Voice'],[72,28,'em',9,'Parts.pdf'],[22,70,'cy',8,'Vespa'],[78,68,'am',8,'Video'],[50,18,'vi',6,''],[40,82,'em',6,''],[62,82,'cy',6,''],[12,46,'em',5,''],[88,46,'vi',5,'']];
return shell(`<div class="p-4 md:p-6 h-full flex flex-col"><div class="flex items-end justify-between mb-4"><div><p class="lbl mb-1">${t('lat')}</p><h1 class="f-head font-semibold text-[22px]">${t('lat_p')}</h1></div><p class="text-[12px] ink3 hidden sm:block">${t('sync')}</p></div>
<div class="g2 rounded-2xl flex-1 min-h-[420px] relative overflow-hidden" dir="ltr"><svg class="absolute inset-0 w-full h-full" viewBox="0 0 100 100" preserveAspectRatio="none">${P.slice(1).map(p=>`<line x1="50" y1="50" x2="${p[0]}" y2="${p[1]}" stroke="var(--line-2)" stroke-width=".25" vector-effect="non-scaling-stroke"/>`).join('')}</svg>
${P.map(([x,y,c,r,l])=>`<div class="absolute -translate-x-1/2 -translate-y-1/2 flex flex-col items-center gap-1.5" style="left:${x}%;top:${y}%"><span class="${c} rounded-full block" style="width:${r*2}px;height:${r*2}px;background:currentColor;box-shadow:0 0 ${r*2}px currentColor"></span>${l?`<span class="pill text-[11px] f-mono g1 ink">${l}</span>`:''}</div>`).join('')}</div></div>`,1)}

const V={welcome,folders,model,ready,home,lattice};
let L;function render(){const h=document.documentElement;h.lang=st.lang;h.dir=st.lang==='ar'?'rtl':'ltr';h.dataset.theme=st.theme;
document.getElementById('app').innerHTML=(V[st.s]||welcome)();L&&L.setTheme(st.theme);}
document.addEventListener('click',e=>{const g=e.target.closest('[data-go],[data-a],[data-mode]');if(!g)return;
if(g.dataset.go){st.s=g.dataset.go;localStorage.rs=st.s}
if(g.dataset.a==='theme'){st.theme=st.theme==='dark'?'light':'dark';localStorage.rt=st.theme}
if(g.dataset.a==='lang'){st.lang=st.lang==='en'?'ar':'en';localStorage.rl=st.lang}
if(g.dataset.a==='retry'){st.dl='run';st.pct=4;tick()}
if(g.dataset.mode){document.querySelectorAll('[data-mode]').forEach(b=>{const on=b===g;b.classList.toggle('btn-p',on);b.classList.toggle('ink2',!on);b.setAttribute('aria-selected',on)});return}
render()});
function tick(){if(st.s!=='model'||st.dl!=='run'||st.pct>=100)return;st.pct=Math.min(100,st.pct+3);const b=document.querySelector('[data-bar=run]'),p=document.querySelector('[data-pct]');if(b){b.style.width=st.pct+'%';p.textContent=N(st.pct)+'%'}if(st.pct>=100)render();setTimeout(tick,120)}
render();L=mountLattice(document.getElementById('bg'),{theme:st.theme,maxDpr:1.5});window.__app={st,render,L};
