// ===== theme (day / night) =====
(function initTheme(){
  let t;
  try{ t=localStorage.getItem('ptc_theme'); }catch(e){}
  if(t==='dark') document.documentElement.setAttribute('data-theme','dark');
})();
function toggleTheme(){
  const html=document.documentElement;
  html.classList.add('theme-anim');
  const dark=html.getAttribute('data-theme')==='dark';
  if(dark) html.removeAttribute('data-theme');
  else html.setAttribute('data-theme','dark');
  try{ localStorage.setItem('ptc_theme', dark?'light':'dark'); }catch(e){}
  setTimeout(()=>html.classList.remove('theme-anim'),450);
}

// ===== mobile nav =====
function toggleNav(){document.querySelector('.nav-links')?.classList.toggle('show');}

// ===== accordions =====
document.addEventListener('click', e=>{
  const head = e.target.closest('.course-head');
  if(head){ head.parentElement.classList.toggle('open'); }
});

// ===== expand / collapse all =====
function toggleAll(btn){
  const courses=[...document.querySelectorAll('.course')];
  const anyClosed=courses.some(c=>!c.classList.contains('open'));
  courses.forEach(c=>c.classList.toggle('open',anyClosed));
  btn.textContent = anyClosed ? 'إغلاق الكل' : 'فتح الكل';
}

// ===== search filter (across semesters) =====
function filterCourses(q){
  q=q.trim().toLowerCase();
  const panels=document.querySelectorAll('.sem-panel');
  if(q && panels.length){ panels.forEach(p=>p.classList.add('show')); }
  else if(panels.length){
    panels.forEach((p,i)=>p.classList.toggle('show', p.dataset.sem==='0'));
    document.querySelectorAll('.sem-tab').forEach((t,i)=>t.classList.toggle('active',i===0));
  }
  document.querySelectorAll('.course').forEach(c=>{
    c.style.display = c.textContent.toLowerCase().includes(q) ? '' : 'none';
  });
}

// ===== reveal on scroll =====
const io=new IntersectionObserver(en=>{
  en.forEach(e=>{if(e.isIntersecting){e.target.classList.add('in');io.unobserve(e.target);}});
},{threshold:.1});
// نراقب الموجود الآن + دالة عامة لأي عنصر ينحقن بعدين (الإعلانات مثلاً).
// بدونها العناصر الجديدة تضلّ opacity:0 — موجودة بالـ DOM بس غير مرئية.
window.revealScan=function(root){
  (root||document).querySelectorAll('.reveal:not(.in)').forEach(function(el){
    if(el.getBoundingClientRect().top < window.innerHeight) el.classList.add('in');
    else io.observe(el);
  });
};
revealScan();
window.addEventListener('load',()=>setTimeout(()=>{
  document.querySelectorAll('.reveal:not(.in)').forEach(el=>{
    if(el.getBoundingClientRect().top<window.innerHeight) el.classList.add('in');
  });
},350));

// ===== semester tabs =====
function showSem(btn,i){
  btn.parentElement.querySelectorAll('.sem-tab').forEach(t=>t.classList.remove('active'));
  btn.classList.add('active');
  document.querySelectorAll('.sem-panel').forEach(p=>p.classList.toggle('show',+p.dataset.sem===i));
}

// ===== deep link: year page opened with #c_CODE or #sem0/#sem1 → open right semester =====
function openFromHash(){
  const h=location.hash;
  // open a specific semester tab
  if(h==='#sem0'||h==='#sem1'){
    const si=h==='#sem1'?1:0;
    const tabs=document.querySelectorAll('.sem-tab');
    document.querySelectorAll('.sem-panel').forEach(p=>p.classList.toggle('show',+p.dataset.sem===si));
    tabs.forEach((t,i)=>t.classList.toggle('active',i===si));
    if(tabs.length) window.scrollTo({top:0,behavior:'smooth'});
    return;
  }
  if(!h || h.indexOf('#c_')!==0) return;
  const target=document.getElementById(h.slice(1));
  if(!target) return;
  const panel=target.closest('.sem-panel');
  if(panel){
    const si=+panel.dataset.sem;
    document.querySelectorAll('.sem-panel').forEach(p=>p.classList.toggle('show',+p.dataset.sem===si));
    document.querySelectorAll('.sem-tab').forEach((t,i)=>t.classList.toggle('active',i===si));
  }
  target.classList.add('open','flash');
  setTimeout(()=>target.scrollIntoView({behavior:'smooth',block:'center'}),180);
  setTimeout(()=>target.classList.remove('flash'),2400);
}
window.addEventListener('DOMContentLoaded',openFromHash);
window.addEventListener('hashchange',openFromHash);


// ===== Adhkar (morning / evening) + daily reminders as in-site toasts =====
const MORNING_ADHKAR=[
  {t:"أَصْبَحْنَا وَأَصْبَحَ الْمُلْكُ لِلَّهِ، وَالْحَمْدُ لِلَّهِ", s:"من أذكار الصباح"},
  {t:"اللَّهُمَّ بِكَ أَصْبَحْنَا، وَبِكَ أَمْسَيْنَا، وَبِكَ نَحْيَا، وَبِكَ نَمُوتُ، وَإِلَيْكَ النُّشُورُ", s:"من أذكار الصباح"},
  {t:"سُبْحَانَ اللَّهِ وَبِحَمْدِهِ عَدَدَ خَلْقِهِ، وَرِضَا نَفْسِهِ، وَزِنَةَ عَرْشِهِ، وَمِدَادَ كَلِمَاتِهِ", s:"من أذكار الصباح"},
  {t:"اللَّهُمَّ إِنِّي أَسْأَلُكَ عِلْمًا نَافِعًا، وَرِزْقًا طَيِّبًا، وَعَمَلًا مُتَقَبَّلًا", s:"دعاء الصباح"},
  {t:"حَسْبِيَ اللَّهُ لَا إِلَهَ إِلَّا هُوَ، عَلَيْهِ تَوَكَّلْتُ، وَهُوَ رَبُّ الْعَرْشِ الْعَظِيمِ", s:"من أذكار الصباح · سبع مرات"},
];
const EVENING_ADHKAR=[
  {t:"أَمْسَيْنَا وَأَمْسَى الْمُلْكُ لِلَّهِ، وَالْحَمْدُ لِلَّهِ", s:"من أذكار المساء"},
  {t:"اللَّهُمَّ بِكَ أَمْسَيْنَا، وَبِكَ أَصْبَحْنَا، وَبِكَ نَحْيَا، وَبِكَ نَمُوتُ، وَإِلَيْكَ الْمَصِيرُ", s:"من أذكار المساء"},
  {t:"اللَّهُمَّ عَافِنِي فِي بَدَنِي، اللَّهُمَّ عَافِنِي فِي سَمْعِي، اللَّهُمَّ عَافِنِي فِي بَصَرِي", s:"من أذكار المساء"},
  {t:"أَعُوذُ بِكَلِمَاتِ اللَّهِ التَّامَّاتِ مِنْ شَرِّ مَا خَلَقَ", s:"من أذكار المساء · ثلاث مرات"},
  {t:"رَضِيتُ بِاللَّهِ رَبًّا، وَبِالْإِسْلَامِ دِينًا، وَبِمُحَمَّدٍ ﷺ نَبِيًّا", s:"من أذكار المساء"},
];

const NIGHT_ADHKAR=[
  {t:"﴿ تَبَارَكَ الَّذِي بِيَدِهِ الْمُلْكُ وَهُوَ عَلَىٰ كُلِّ شَيْءٍ قَدِيرٌ ﴾", s:"اقرأ سورة المُلك قبل نومك · تنجّي من عذاب القبر"},
  {t:"«مَن قَرَأَ سُورَةَ تَبَارَكَ كُلَّ لَيْلَةٍ مَنَعَهُ اللَّهُ بِهَا مِن عَذَابِ الْقَبْرِ»", s:"تذكير بقراءة سورة المُلك"},
  {t:"بِاسْمِكَ اللَّهُمَّ أَمُوتُ وَأَحْيَا", s:"من أذكار النوم"},
  {t:"اللَّهُمَّ قِنِي عَذَابَكَ يَوْمَ تَبْعَثُ عِبَادَكَ", s:"من أذكار النوم · ثلاث مرات"},
  {t:"آيةُ الكرسيّ: ﴿ اللَّهُ لَا إِلَٰهَ إِلَّا هُوَ الْحَيُّ الْقَيُّومُ ﴾", s:"من قرأها عند نومه لم يزل عليه من الله حافظ"},
  {t:"باسْمِكَ رَبِّي وَضَعْتُ جَنْبِي، وَبِكَ أَرْفَعُهُ", s:"من أذكار النوم"},
  {t:"سُبْحَانَ اللَّهِ (33)، الْحَمْدُ لِلَّهِ (33)، اللَّهُ أَكْبَرُ (34)", s:"تسبيح ما قبل النوم"},
];

function pick(arr){ // stable pick per day so it doesn't flicker across pages
  const now=new Date(); const day=Math.floor((now-new Date(now.getFullYear(),0,0))/86400000);
  return arr[(day+now.getHours())%arr.length];
}

function showToast(kind,text,sub,mins,link){
  let wrap=document.querySelector('.toast-wrap');
  if(!wrap){wrap=document.createElement('div');wrap.className='toast-wrap';document.body.appendChild(wrap);}
  const dur=(mins||45)*1000; // default 45s, longer as requested
  const el=document.createElement('div');
  el.className='toast'+(link?' clickable':'');
  el.innerHTML=`<div class="toast-glow"></div>
    <div class="th"><span class="tkind"><span class="tdot"></span>${kind}</span><button class="tclose" aria-label="إغلاق">×</button></div>
    <div class="ttext">${text}</div><div class="tsub">${sub}</div>
    ${link?'<div class="tgo">اضغط للانتقال ←</div>':''}
    <div class="toast-progress" style="animation-duration:${dur}ms"></div>`;
  wrap.appendChild(el);
  requestAnimationFrame(()=>el.classList.add('show'));
  const close=()=>{el.classList.add('closing');el.classList.remove('show');setTimeout(()=>el.remove(),550);};
  el.querySelector('.tclose').onclick=(e)=>{ e.stopPropagation(); close(); };
  if(link){
    el.addEventListener('click',e=>{
      if(e.target.closest('.tclose')) return;
      location.href=link;
    });
  }
  setTimeout(close,dur);
}

// decide reminder by time windows: fajr→8am morning, asr→maghrib evening, 9-11pm night, else daily
function maybeRemind(fajr,asr,maghrib){
  const now=new Date();
  const mins=now.getHours()*60+now.getMinutes();
  const toMin=t=>{const[h,m]=t.split(':').map(Number);return h*60+m;};
  let kind,item,dur=45,link='rawdah.html';
  const eightAM=8*60, ninePM=21*60, elevenPM=23*60;
  if(mins>=ninePM && mins<elevenPM){                       // 9–11 مساءً
    const v=pick(NIGHT_ADHKAR); kind="قبل النوم"; item={t:v.t,s:v.s}; dur=75; link='rawdah.html#night';
  } else if(fajr && mins>=toMin(fajr) && mins<eightAM){     // الفجر → 8 صباحاً
    const v=pick(MORNING_ADHKAR); kind="أذكار الصباح"; item={t:v.t,s:v.s}; dur=60; link='rawdah.html#morning';
  } else if(asr && maghrib && mins>=toMin(asr) && mins<toMin(maghrib)){ // العصر → المغرب
    const v=pick(EVENING_ADHKAR); kind="أذكار المساء"; item={t:v.t,s:v.s}; dur=60; link='rawdah.html#evening';
  } else {                                                  // بقية اليوم → الورد
    const v=pick(DAILY.length?DAILY:[{k:"ذِكر",a:"سُبْحَانَ اللَّهِ",s:""}]);
    kind="الورد اليومي · "+v.k; item={t:v.a,s:v.s}; dur=45; link='rawdah.html#hadith';
  }
  // throttle: once per 60 min per tab
  const key="ptc_last_toast";
  try{
    const last=+(sessionStorage.getItem(key)||0);
    if(Date.now()-last < 60*60*1000) return;
    sessionStorage.setItem(key,Date.now());
  }catch(e){}
  setTimeout(()=>showToast(kind,item.t,item.s,dur,link), 2600);
}

// ===== fixed location: Gaza (no geolocation prompt) =====
const GAZA_COORDS={lat:31.5017,lng:34.4668};
function getLocationOnce(cb){ cb(GAZA_COORDS.lat,GAZA_COORDS.lng); }

(function initReminder(){
  fetch(`https://api.aladhan.com/v1/timings?latitude=${GAZA_COORDS.lat}&longitude=${GAZA_COORDS.lng}&method=4`)
    .then(r=>r.json()).then(d=>{const t=d.data.timings;maybeRemind(t.Fajr,t.Asr,t.Maghrib);})
    .catch(()=>maybeRemind(null,null,null));
})();

// ===== daily reminder — verse / hadith / dhikr (changes each day, offline) =====
const DAILY=[
  {k:"آية", a:"﴿ وَقُل رَّبِّ زِدْنِي عِلْمًا ﴾", s:"طه: 114"},
  {k:"آية", a:"﴿ إِنَّمَا يَخْشَى اللَّهَ مِنْ عِبَادِهِ الْعُلَمَاءُ ﴾", s:"فاطر: 28"},
  {k:"آية", a:"﴿ يَرْفَعِ اللَّهُ الَّذِينَ آمَنُوا مِنكُمْ وَالَّذِينَ أُوتُوا الْعِلْمَ دَرَجَاتٍ ﴾", s:"المجادلة: 11"},
  {k:"حديث", a:"«مَن سَلَكَ طَرِيقًا يَلْتَمِسُ فِيهِ عِلْمًا سَهَّلَ اللَّهُ لَهُ بِهِ طَرِيقًا إِلَى الْجَنَّةِ»", s:"رواه مسلم"},
  {k:"آية", a:"﴿ وَمَن يَتَّقِ اللَّهَ يَجْعَل لَّهُ مَخْرَجًا ﴾", s:"الطلاق: 2"},
  {k:"آية", a:"﴿ إِنَّ مَعَ الْعُسْرِ يُسْرًا ﴾", s:"الشرح: 6"},
  {k:"حديث", a:"«إِذَا مَاتَ الإِنْسَانُ انْقَطَعَ عَنْهُ عَمَلُهُ إِلَّا مِنْ ثَلَاثٍ... أَوْ عِلْمٍ يُنْتَفَعُ بِهِ»", s:"رواه مسلم"},
  {k:"ذِكر", a:"«لَا حَوْلَ وَلَا قُوَّةَ إِلَّا بِاللَّهِ»", s:"كنز من كنوز الجنة"},
  {k:"آية", a:"﴿ وَأَن لَّيْسَ لِلْإِنسَانِ إِلَّا مَا سَعَىٰ ﴾", s:"النجم: 39"},
  {k:"دعاء", a:"«اللَّهُمَّ انْفَعْنِي بِمَا عَلَّمْتَنِي، وَعَلِّمْنِي مَا يَنْفَعُنِي، وَزِدْنِي عِلْمًا»", s:"رواه ابن ماجه"},
  {k:"آية", a:"﴿ وَقُلِ اعْمَلُوا فَسَيَرَى اللَّهُ عَمَلَكُمْ وَرَسُولُهُ وَالْمُؤْمِنُونَ ﴾", s:"التوبة: 105"},
  {k:"حديث", a:"«طَلَبُ الْعِلْمِ فَرِيضَةٌ عَلَى كُلِّ مُسْلِمٍ»", s:"رواه ابن ماجه"},
  {k:"ذِكر", a:"«سُبْحَانَ اللَّهِ وَبِحَمْدِهِ، سُبْحَانَ اللَّهِ الْعَظِيمِ»", s:"حبيبتان إلى الرحمن"},
  {k:"آية", a:"﴿ رَبَّنَا آتِنَا فِي الدُّنْيَا حَسَنَةً وَفِي الْآخِرَةِ حَسَنَةً ﴾", s:"البقرة: 201"},
  {k:"حديث", a:"«مَن دَلَّ عَلَى خَيْرٍ فَلَهُ مِثْلُ أَجْرِ فَاعِلِهِ»", s:"رواه مسلم"},
  {k:"آية", a:"﴿ وَتَوَكَّلْ عَلَى اللَّهِ ۚ وَكَفَىٰ بِاللَّهِ وَكِيلًا ﴾", s:"النساء: 81"},
  {k:"ذِكر", a:"«حَسْبُنَا اللَّهُ وَنِعْمَ الْوَكِيلُ»", s:"عند الشدائد"},
  {k:"دعاء", a:"«رَبِّ اشْرَحْ لِي صَدْرِي وَيَسِّرْ لِي أَمْرِي»", s:"طه: 25-26"},
  {k:"آية", a:"﴿ إِنَّ اللَّهَ مَعَ الصَّابِرِينَ ﴾", s:"البقرة: 153"},
  {k:"حديث", a:"«الْمُؤْمِنُ الْقَوِيُّ خَيْرٌ وَأَحَبُّ إِلَى اللَّهِ مِنَ الْمُؤْمِنِ الضَّعِيفِ»", s:"رواه مسلم"},
  {k:"آية", a:"﴿ فَاذْكُرُونِي أَذْكُرْكُمْ وَاشْكُرُوا لِي وَلَا تَكْفُرُونِ ﴾", s:"البقرة: 152"},
  {k:"آية", a:"﴿ وَمَا تَوْفِيقِي إِلَّا بِاللَّهِ ۚ عَلَيْهِ تَوَكَّلْتُ وَإِلَيْهِ أُنِيبُ ﴾", s:"هود: 88"},
  {k:"دعاء", a:"«اللَّهُمَّ لَا سَهْلَ إِلَّا مَا جَعَلْتَهُ سَهْلًا، وَأَنْتَ تَجْعَلُ الْحَزْنَ إِذَا شِئْتَ سَهْلًا»", s:"رواه ابن حبان"},
  {k:"حديث", a:"«إِنَّ اللَّهَ يُحِبُّ إِذَا عَمِلَ أَحَدُكُمْ عَمَلًا أَنْ يُتْقِنَهُ»", s:"رواه البيهقي"},
  {k:"آية", a:"﴿ وَبَشِّرِ الصَّابِرِينَ ﴾", s:"البقرة: 155"},
  {k:"ذِكر", a:"«لَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ»", s:"أفضل ما قاله النبيون"},
  {k:"دعاء", a:"«اللَّهُمَّ أَعِنِّي عَلَى ذِكْرِكَ وَشُكْرِكَ وَحُسْنِ عِبَادَتِكَ»", s:"رواه أبو داود"},
  {k:"آية", a:"﴿ قُلْ هَلْ يَسْتَوِي الَّذِينَ يَعْلَمُونَ وَالَّذِينَ لَا يَعْلَمُونَ ﴾", s:"الزمر: 9"},
  {k:"حديث", a:"«مَنْ غَدَا إِلَى الْمَسْجِدِ لَا يُرِيدُ إِلَّا أَنْ يَتَعَلَّمَ خَيْرًا... كَانَ لَهُ كَأَجْرِ حَاجٍّ تَامًّا حَجَّتُهُ»", s:"رواه الطبراني"},
  {k:"آية", a:"﴿ وَاللَّهُ يَعْلَمُ وَأَنتُمْ لَا تَعْلَمُونَ ﴾", s:"البقرة: 216"},
  {k:"دعاء", a:"«رَبِّ زِدْنِي عِلْمًا وَفَهْمًا، وَأَلْحِقْنِي بِالصَّالِحِينَ»", s:"من أدعية طلب العلم"},
];
(function(){
  const el=document.getElementById('ayah'), su=document.getElementById('surah'), kd=document.getElementById('kind');
  if(!el) return;
  const now=new Date();
  const start=new Date(now.getFullYear(),0,0);
  const day=Math.floor((now-start)/86400000);
  const v=DAILY[day % DAILY.length];
  el.textContent=v.a; if(su) su.textContent=v.s; if(kd) kd.textContent=v.k;
})();

// ===== homepage global course search =====
(function(){
  const input=document.getElementById('homeSearch');
  const box=document.getElementById('hsResults');
  if(!input || typeof COURSE_INDEX==='undefined') return;
  let active=-1, current=[];
  function render(list){
    current=list; active=-1;
    if(!list.length){ box.innerHTML='<div class="hs-empty">ما في مساق بهذا الاسم</div>'; box.classList.add('show'); return; }
    box.innerHTML=list.map((c,i)=>`<div class="hs-item" data-i="${i}" onclick="hsGo(${i})">
        <span class="hs-code">${c.code}</span>
        <span class="hs-meta"><span class="hs-en">${c.en}</span><span class="hs-ar">${c.ar}</span></span>
        <span class="hs-where">${c.year} · ${c.sname}</span>
      </div>`).join('');
    box.classList.add('show');
  }
  window.hsGo=function(i){
    const c=current[i]; if(!c) return;
    location.href=c.page+'#'+c.id;
  };
  window.clearSearch=function(){
    input.value=''; box.classList.remove('show');
    const clr=document.getElementById('hsClear'); if(clr) clr.classList.remove('show');
    input.focus();
  };
  input.addEventListener('input',()=>{
    const q=input.value.trim().toLowerCase();
    const clr=document.getElementById('hsClear');
    if(clr) clr.classList.toggle('show', input.value.length>0);
    if(!q){ box.classList.remove('show'); return; }
    const list=COURSE_INDEX.filter(c=>
      c.code.toLowerCase().includes(q)||c.en.toLowerCase().includes(q)||c.ar.includes(q)||(c.kw&&c.kw.toLowerCase().includes(q))
    ).slice(0,8);
    render(list);
  });
  input.addEventListener('keydown',e=>{
    const items=[...box.querySelectorAll('.hs-item')];
    if(e.key==='ArrowDown'){e.preventDefault();active=Math.min(active+1,items.length-1);}
    else if(e.key==='ArrowUp'){e.preventDefault();active=Math.max(active-1,0);}
    else if(e.key==='Enter'){ if(active>=0) hsGo(active); else if(current.length) hsGo(0); return;}
    else return;
    items.forEach((it,i)=>it.classList.toggle('active',i===active));
    if(items[active]) items[active].scrollIntoView({block:'nearest'});
  });
  document.addEventListener('click',e=>{ if(!e.target.closest('.home-search')) box.classList.remove('show'); });
})();

// ===== date / time strip (Gregorian + Hijri + live clock) =====
(function(){
  const dayEl=document.getElementById('dtDay'), gregEl=document.getElementById('dtGreg'),
        hijriEl=document.getElementById('dtHijri'), clockEl=document.getElementById('dtClock');
  if(!clockEl) return;
  const days=["الأحد","الإثنين","الثلاثاء","الأربعاء","الخميس","الجمعة","السبت"];
  function tick(){
    const now=new Date();
    if(dayEl) dayEl.textContent=days[now.getDay()];
    if(gregEl) gregEl.textContent=now.toLocaleDateString('ar-EG-u-nu-latn',{day:'numeric',month:'long',year:'numeric'});
    if(hijriEl){
      try{ hijriEl.textContent=new Intl.DateTimeFormat('ar-SA-u-ca-islamic-nu-latn',{day:'numeric',month:'long',year:'numeric'}).format(now); }
      catch(e){ hijriEl.textContent=''; }
    }
    let h=now.getHours();
    const ampm = h<12 ? 'ص' : 'م';
    h = h%12; if(h===0) h=12;
    const hh=String(h).padStart(2,'0');
    const mm=String(now.getMinutes()).padStart(2,'0');
    const ss=String(now.getSeconds()).padStart(2,'0');
    clockEl.innerHTML=`${hh}:${mm}:${ss} <span style="font-size:.6em;color:var(--copper)">${ampm}</span>`;
  }
  tick(); setInterval(tick,1000);
})();

// ===== next-prayer countdown (Gaza timing, homepage) =====
(function(){
  const nameEl=document.getElementById('pcName');
  if(!nameEl) return;
  const timeEl=document.getElementById('pcTime'), countEl=document.getElementById('pcCount');
  const GAZA={lat:31.5017,lng:34.4668};
  const NAMES={Fajr:'الفجر',Dhuhr:'الظهر',Asr:'العصر',Maghrib:'المغرب',Isha:'العشاء'};
  const ORDER=['Fajr','Dhuhr','Asr','Maghrib','Isha'];
  let times=null;
  function loadDay(offset){
    const d=new Date(); d.setDate(d.getDate()+offset);
    const dd=String(d.getDate()).padStart(2,'0'), mm=String(d.getMonth()+1).padStart(2,'0'), yy=d.getFullYear();
    return fetch(`https://api.aladhan.com/v1/timings/${dd}-${mm}-${yy}?latitude=${GAZA.lat}&longitude=${GAZA.lng}&method=4`)
      .then(r=>r.json()).then(j=>j.data.timings);
  }
  function toDate(base,hhmm){const[h,m]=hhmm.split(':').map(Number);const d=new Date(base);d.setHours(h,m,0,0);return d;}
  function computeNext(){
    const now=new Date();
    let next=null,nname=null;
    if(times){
      for(const k of ORDER){
        const t=toDate(now,times[k]);
        if(t>now){ next=t; nname=k; break; }
      }
    }
    if(!next && window._tomorrowFajr){ next=window._tomorrowFajr; nname='Fajr'; }
    return next?{when:next,name:nname}:null;
  }
  function render(){
    const nx=computeNext();
    if(!nx){ return; }
    nameEl.textContent=NAMES[nx.name];
    let ph=nx.when.getHours(); const pap = ph<12?'ص':'م'; ph=ph%12; if(ph===0)ph=12;
    const h=String(ph).padStart(2,'0'), m=String(nx.when.getMinutes()).padStart(2,'0');
    timeEl.textContent=`الأذان ${h}:${m} ${pap}`;
    let diff=Math.max(0,Math.floor((nx.when-new Date())/1000));
    const hh=String(Math.floor(diff/3600)).padStart(2,'0');
    const mm=String(Math.floor((diff%3600)/60)).padStart(2,'0');
    const ss=String(diff%60).padStart(2,'0');
    countEl.innerHTML=`${hh}:${mm}:${ss}`;
    if(diff===0){ setTimeout(init,60000); } // refresh after adhan
  }
  function init(){
    loadDay(0).then(t=>{
      times=t;
      return loadDay(1);
    }).then(t2=>{
      const d=new Date(); d.setDate(d.getDate()+1);
      window._tomorrowFajr=toDate(d,t2.Fajr);
      render();
    }).catch(()=>{ nameEl.textContent='تعذّر جلب المواعيد'; });
  }
  init();
  setInterval(render,1000);
})();

// ===== "مساقاتي الحالية" — cloud when signed in, local otherwise =====
(function(){
  const list=document.getElementById('mcList');
  if(!list || typeof COURSE_INDEX==='undefined') return;
  let cache=[];
  const cloud=()=> typeof PTCAuth!=='undefined' && PTCAuth.enabled() && PTCAuth.user;
  async function get(){
    if(cloud()) return await PTCAuth.getMyCourses();
    try{return JSON.parse(localStorage.getItem('ptc_mycourses')||'[]');}catch(e){return [];}
  }
  function setLocal(a){ try{localStorage.setItem('ptc_mycourses',JSON.stringify(a));}catch(e){} }
  function courseByCode(c){ return COURSE_INDEX.find(x=>x.code===c); }
  async function render(){
    cache=await get();
    if(!cache.length){ list.innerHTML='<div class="mc-empty">ما أضفت مساقات بعد</div>'; return; }
    list.innerHTML=cache.map(code=>{
      const c=courseByCode(code); if(!c) return '';
      return `<div class="mc-item"><a href="${c.page}#${c.id}" title="${c.en}">${c.ar}</a><button class="mc-del" onclick="mcRemove('${code}')" aria-label="حذف">×</button></div>`;
    }).join('');
  }
  window.mcRemove=async function(code){
    if(cloud()) await PTCAuth.removeMyCourse(code);
    else setLocal((await get()).filter(x=>x!==code));
    render();
  };
  window.mcOpenAdd=function(){
    const p=document.getElementById('mcPicker');
    const show=p.style.display==='none';
    p.style.display=show?'block':'none';
    if(show){ mcFilter(''); document.getElementById('mcSearch').focus(); }
  };
  window.mcFilter=function(q){
    const clr=document.getElementById('mcClear');
    if(clr) clr.classList.toggle('show', (q||'').length>0);
    q=(q||'').trim().toLowerCase();
    const mine=cache;
    const opts=COURSE_INDEX.filter(c=>!mine.includes(c.code) &&
      (!q || c.code.toLowerCase().includes(q)||c.en.toLowerCase().includes(q)||c.ar.includes(q))
    ).slice(0,40);
    document.getElementById('mcOptions').innerHTML = opts.length
      ? opts.map(c=>`<button class="mc-opt" onclick="mcAdd('${c.code}')"><span>${c.ar}</span><code>${c.code}</code></button>`).join('')
      : '<div class="mc-empty">لا نتائج</div>';
  };
  window.mcClearSearch=function(){
    const inp=document.getElementById('mcSearch');
    inp.value=''; mcFilter(''); inp.focus();
  };
  window.mcAdd=async function(code){
    if(cloud()) await PTCAuth.addMyCourse(code);
    else { const mine=await get(); if(!mine.includes(code)) mine.push(code); setLocal(mine); }
    await render();
    document.getElementById('mcSearch').value=''; mcFilter('');
  };
  render();
  // re-render when the user signs in/out
  window.addEventListener('ptc-auth-change',()=>render());
})();

// ===== compact date/time/prayer widget auto-injected on inner pages =====
(function(){
  // skip homepage (it already has the full side-stack)
  if(document.getElementById('sideStack')) return;
  if(!document.querySelector('.nav')) return; // only on site pages
  const wrap=document.createElement('div');
  wrap.className='mini-widget';
  wrap.innerHTML=`
    <button class="mw-toggle" id="mwToggle" aria-label="التاريخ والصلاة"><svg class="ic" width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3.5 2"/></svg></button>
    <div class="mw-panel" id="mwPanel">
      <div class="mw-time" id="mwClock">--:--:--</div>
      <div class="mw-day" id="mwDay">—</div>
      <div class="mw-line"></div>
      <div class="mw-greg" id="mwGreg">—</div>
      <div class="mw-hijri" id="mwHijri">—</div>
      <div class="mw-line"></div>
      <div class="mw-prayer"><span class="mw-pname" id="mwPName">—</span><span class="mw-pcount" id="mwPCount">--:--:--</span></div>
      <div class="mw-premain">المتبقّي للأذان القادم · غزة</div>
    </div>`;
  document.body.appendChild(wrap);
  document.getElementById('mwToggle').onclick=()=>wrap.classList.toggle('open');

  const days=["الأحد","الإثنين","الثلاثاء","الأربعاء","الخميس","الجمعة","السبت"];
  function clock(){
    const now=new Date();
    document.getElementById('mwDay').textContent=days[now.getDay()];
    document.getElementById('mwGreg').textContent=now.toLocaleDateString('ar-EG-u-nu-latn',{day:'numeric',month:'long',year:'numeric'});
    try{document.getElementById('mwHijri').textContent=new Intl.DateTimeFormat('ar-SA-u-ca-islamic-nu-latn',{day:'numeric',month:'long',year:'numeric'}).format(now);}catch(e){}
    let h=now.getHours();const ap=h<12?'ص':'م';h=h%12||12;
    document.getElementById('mwClock').innerHTML=`${String(h).padStart(2,'0')}:${String(now.getMinutes()).padStart(2,'0')}:${String(now.getSeconds()).padStart(2,'0')} <span style="font-size:.6em;color:var(--copper)">${ap}</span>`;
  }
  clock(); setInterval(clock,1000);

  // prayer countdown (Gaza)
  const NAMES={Fajr:'الفجر',Dhuhr:'الظهر',Asr:'العصر',Maghrib:'المغرب',Isha:'العشاء'};
  const ORDER=['Fajr','Dhuhr','Asr','Maghrib','Isha'];
  let T=null, tomF=null;
  function toDate(base,hhmm){const[h,m]=hhmm.split(':').map(Number);const d=new Date(base);d.setHours(h,m,0,0);return d;}
  function loadDay(off){const d=new Date();d.setDate(d.getDate()+off);const dd=String(d.getDate()).padStart(2,'0'),mm=String(d.getMonth()+1).padStart(2,'0');return fetch(`https://api.aladhan.com/v1/timings/${dd}-${mm}-${d.getFullYear()}?latitude=31.5017&longitude=34.4668&method=4`).then(r=>r.json()).then(j=>j.data.timings);}
  function nextP(){const now=new Date();if(T){for(const k of ORDER){const t=toDate(now,T[k]);if(t>now)return{when:t,name:k};}}if(tomF)return{when:tomF,name:'Fajr'};return null;}
  function pRender(){const nx=nextP();if(!nx)return;document.getElementById('mwPName').textContent=NAMES[nx.name];let diff=Math.max(0,Math.floor((nx.when-new Date())/1000));const hh=String(Math.floor(diff/3600)).padStart(2,'0'),mm=String(Math.floor((diff%3600)/60)).padStart(2,'0'),ss=String(diff%60).padStart(2,'0');document.getElementById('mwPCount').textContent=`${hh}:${mm}:${ss}`;if(diff===0)setTimeout(pInit,60000);}
  function pInit(){loadDay(0).then(t=>{T=t;return loadDay(1);}).then(t2=>{const d=new Date();d.setDate(d.getDate()+1);tomF=toDate(d,t2.Fajr);pRender();}).catch(()=>{document.getElementById('mwPName').textContent='—';});}
  pInit(); setInterval(pRender,1000);
})();

// ===== nav dropdown: hover on desktop, tap on mobile; close on outside click =====
(function(){
  const drop=document.querySelector('.nav-drop');
  const menu=document.getElementById('menu');
  if(!drop) return;
  const isMobile=()=>window.matchMedia('(max-width:820px)').matches;

  // desktop: click the trigger toggles a "pinned open" state too
  const btn=drop.querySelector('.nav-drop-btn');
  btn.addEventListener('click',e=>{
    e.stopPropagation();
    drop.classList.toggle('open');
  });

  // mobile: tapping a year toggles its semesters (instead of auto-showing all)
  drop.querySelectorAll('.dd-year').forEach(y=>{
    const link=y.querySelector('.dd-year-link');
    link.addEventListener('click',e=>{
      if(isMobile() && !y.classList.contains('open')){
        e.preventDefault();            // first tap opens semesters
        drop.querySelectorAll('.dd-year').forEach(o=>{ if(o!==y) o.classList.remove('open'); });
        y.classList.add('open');
      }
    });
  });

  // close everything when clicking anywhere outside
  document.addEventListener('click',e=>{
    if(!e.target.closest('.nav-drop')) drop.classList.remove('open');
    if(!e.target.closest('.nav-links') && !e.target.closest('.burger')){
      if(menu) menu.classList.remove('show');
      drop.querySelectorAll('.dd-year').forEach(y=>y.classList.remove('open'));
    }
  });
  // close on Escape
  document.addEventListener('keydown',e=>{
    if(e.key==='Escape'){ drop.classList.remove('open'); if(menu) menu.classList.remove('show'); }
  });
})();

// ===== Quran audio: page recitation that keeps playing across the site =====
// Uses the free/licensed AlQuran Cloud CDN (Mishary Alafasy, ayah-by-ayah).
const QuranAudio=(function(){
  const KEY='ptc_audio_state';
  let audio=null, queue=[], idx=0, page=null, playing=false;

  function ensure(){
    if(audio) return audio;
    audio=new Audio();
    audio.preload='auto';
    audio.addEventListener('ended',()=>{
      idx++;
      if(idx<queue.length){ audio.src=queue[idx]; audio.play().catch(()=>{}); save(); }
      else { playing=false; save(); emit(); }
    });
    audio.addEventListener('error',()=>{ // skip a bad ayah file
      idx++;
      if(idx<queue.length){ audio.src=queue[idx]; audio.play().catch(()=>{}); }
      else { playing=false; emit(); }
    });
    return audio;
  }
  function emit(){ window.dispatchEvent(new CustomEvent('ptc-audio-change')); renderBar(); }
  function save(){
    try{ sessionStorage.setItem(KEY,JSON.stringify({queue,idx,page,playing,t:audio?audio.currentTime:0})); }catch(e){}
  }
  function restore(){
    try{
      const s=JSON.parse(sessionStorage.getItem(KEY)||'null');
      if(!s||!s.playing||!s.queue||!s.queue.length) return;
      queue=s.queue; idx=s.idx; page=s.page;
      ensure(); audio.src=queue[idx];
      audio.currentTime=s.t||0;
      audio.play().then(()=>{playing=true;emit();}).catch(()=>{playing=false;emit();});
    }catch(e){}
  }
  function playPage(p){
    page=p; idx=0; playing=true;
    ensure();
    fetch(`https://api.alquran.cloud/v1/page/${p}/ar.alafasy`)
      .then(r=>r.json())
      .then(d=>{
        queue=(d.data.ayahs||[]).map(a=>a.audio).filter(Boolean);
        if(!queue.length){ playing=false; emit(); return; }
        audio.src=queue[0];
        audio.play().then(()=>{playing=true;save();emit();}).catch(()=>{playing=false;emit();});
      })
      .catch(()=>{ playing=false; emit(); });
    emit();
  }
  function pause(){ if(audio){audio.pause();} playing=false; save(); emit(); }
  function resume(){ if(audio&&queue.length){ audio.play().then(()=>{playing=true;save();emit();}).catch(()=>{}); } }
  function stop(){ if(audio){audio.pause();audio.src='';} queue=[];playing=false;page=null; try{sessionStorage.removeItem(KEY);}catch(e){} emit(); }

  // floating mini player bar (appears on every page while playing)
  function renderBar(){
    let bar=document.getElementById('qAudioBar');
    if(!playing && !queue.length){ if(bar) bar.remove(); return; }
    if(!bar){
      bar=document.createElement('div');
      bar.id='qAudioBar'; bar.className='q-audio-bar';
      bar.innerHTML=`<span class="qab-ic">🕌</span>
        <span class="qab-txt">تلاوة صفحة <b id="qabPage"></b></span>
        <button class="qab-btn" id="qabToggle" aria-label="تشغيل/إيقاف"></button>
        <button class="qab-btn" onclick="QuranAudio.stop()" aria-label="إغلاق">✕</button>`;
      document.body.appendChild(bar);
      bar.querySelector('#qabToggle').onclick=()=>{ playing?pause():resume(); };
    }
    const pg=bar.querySelector('#qabPage'); if(pg) pg.textContent=page||'—';
    const tg=bar.querySelector('#qabToggle'); if(tg) tg.textContent= playing?'⏸':'▶';
    bar.classList.toggle('is-playing',playing);
  }

  window.addEventListener('DOMContentLoaded',restore);
  window.addEventListener('beforeunload',save);

  return {playPage,pause,resume,stop,
    isPlaying:()=>playing,
    currentPage:()=>page};
})();

// ===== navbar auth widget (login link / user menu) =====
(function(){
  function render(){
    const slot=document.getElementById('authSlot');
    if(!slot || typeof PTCAuth==='undefined') return;
    const u=PTCAuth.user, p=PTCAuth.profile;
    if(!u){
      slot.innerHTML=`<a class="auth-link" href="login.html" title="تسجيل الدخول">
        <span class="nav-ic"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="8" r="4"/><path d="M4 21v-1a7 7 0 0 1 14 0v1"/></svg></span>
        <span class="auth-label">دخول</span></a>`;
      return;
    }
    const name=(p&&p.full_name)||u.email.split('@')[0];
    const initial=(name||'؟').trim().charAt(0);
    const staff=PTCAuth.isStaff();
    slot.innerHTML=`<div class="user-menu" id="userMenu">
      <button class="user-btn" type="button" aria-label="حسابي"><span class="user-av">${esc(initial)}</span></button>
      <div class="user-drop">
        <div class="ud-head"><b>${esc(name)}</b><span dir="ltr">${esc(u.email)}</span></div>
        ${p&&p.year?`<div class="ud-meta">السنة ${esc(['','الأولى','الثانية','الثالثة','الرابعة'][p.year]||p.year)}${p.student_id?' · '+esc(p.student_id):''}</div>`:''}
        <a href="journey.html">🎓 رحلة الطالب</a>
        ${staff?'<a href="admin.html">🛠️ لوحة التحكم</a>':''}
        <button type="button" onclick="ptcSignOut()">↩ تسجيل الخروج</button>
      </div>
    </div>`;
    const menu=slot.querySelector('#userMenu');
    menu.querySelector('.user-btn').onclick=e=>{e.stopPropagation();menu.classList.toggle('open');};
    document.addEventListener('click',e=>{ if(!e.target.closest('#userMenu')) menu.classList.remove('open'); });
  }
  window.ptcSignOut=async function(){ await PTCAuth.signOut(); location.href='login.html'; };
  window.addEventListener('ptc-auth-change',render);
  window.addEventListener('DOMContentLoaded',()=>setTimeout(render,400));
})();
// ============================================================
//  تحميل ملفات المساقات وعرضها في صفحات السنوات
// ============================================================
(function courseFilesLoader() {
    // نشتغل فقط في الصفحات التي تعرض مساقات (فيها عناصر .course)
    const courseEls = document.querySelectorAll('.course[id^="c_"]');
    if (!courseEls.length || typeof COURSE_INDEX === 'undefined') return;
    if (typeof PTCAuth === 'undefined') return;

    // خريطة: id الصفحة (c_EEE43254) → code القاعدة (EEE4 3254)
    const idToCode = {};
    COURSE_INDEX.forEach(c => { idToCode[c.id] = c.code; });

    const kindLabel = k => ({ pdf: 'PDF', doc: 'DOC', vid: 'فيديو', zip: 'ZIP', link: 'رابط' }[k] || (k || '').toUpperCase());
    const kindIcon = k => ({ pdf: 'file-text', doc: 'file-text', vid: 'video', zip: 'archive', link: 'link' }[k] || 'paperclip');

    function renderFiles(container, files) {
        if (!files || !files.length) {
            container.innerHTML = '<div class="empty-note"><i data-icon="paperclip"></i> لسا ما انضافت ملفات لهاي المادة — قريباً</div>';
            return;
        }
        container.innerHTML = files.map(function (f) {
            return '<a class="file-item" href="' + safeUrl(f.url) + '" target="_blank" rel="noopener noreferrer">' +
                '<span class="fi-ic"><i data-icon="' + escAttr(kindIcon(f.kind)) + '"></i></span>' +
                '<span class="fi-txt"><b>' + esc(f.title) + '</b>' +
                (f.size_label ? '<small>' + esc(f.size_label) + '</small>' : '') +
                '</span>' +
                '<span class="fi-kind">' + esc(kindLabel(f.kind)) + '</span>' +
                '</a>';
        }).join('');
        // إعادة رسم الأيقونات لو مكتبة icons متوفرة
        if (window.renderIcons) window.renderIcons();
    }

    // نموذج إضافة ملف من الطالب
    function addSubmitForm(container, code) {
        if (!PTCAuth.user) return;  // زائر بلا حساب
        if (PTCAuth.isStaff()) return;  // الطاقم بيضيف من اللوحة

        const btn = document.createElement('button');
        btn.className = 'file-submit-btn';
        btn.textContent = '📎 أضف ملف أو مرجع';
        btn.style.cssText = 'display:block;margin:10px auto 0;padding:8px 16px;border:1px dashed var(--line-2,#ccc);' +
            'border-radius:8px;background:transparent;color:var(--olive,#3c4a2f);cursor:pointer;' +
            'font-family:var(--font-ar,Tajawal);font-size:13px';

        const form = document.createElement('div');
        form.style.cssText = 'display:none;margin-top:10px;padding:14px;border:1px solid var(--line-2,#d5d0c6);' +
            'border-radius:10px;background:var(--paper,#faf8f4)';
        form.innerHTML =
            '<div style="margin-bottom:8px"><input id="sf_title_' + esc(code) + '" placeholder="عنوان الملف" ' +
                'style="width:100%;padding:8px 10px;border:1px solid var(--line-2,#ccc);border-radius:7px;font-family:inherit;font-size:13px"></div>' +
            '<div style="margin-bottom:8px"><input id="sf_url_' + esc(code) + '" placeholder="https://drive.google.com/..." dir="ltr" ' +
                'style="width:100%;padding:8px 10px;border:1px solid var(--line-2,#ccc);border-radius:7px;font-family:inherit;font-size:13px"></div>' +
            '<div style="margin-bottom:8px"><select id="sf_kind_' + esc(code) + '" ' +
                'style="padding:8px 10px;border:1px solid var(--line-2,#ccc);border-radius:7px;font-family:inherit;font-size:13px">' +
                '<option value="pdf">PDF</option><option value="doc">DOC</option><option value="vid">فيديو</option>' +
                '<option value="link" selected>رابط</option><option value="zip">ZIP</option></select></div>' +
            '<button class="sf-send" style="padding:8px 18px;border:none;border-radius:8px;background:var(--olive,#3c4a2f);' +
                'color:#fff;font-family:inherit;font-weight:700;font-size:13px;cursor:pointer">إرسال للمراجعة</button>' +
            '<span class="sf-msg" style="margin-right:10px;font-size:12px"></span>';

        btn.onclick = function () {
            form.style.display = form.style.display === 'none' ? '' : 'none';
        };

        form.querySelector('.sf-send').onclick = async function () {
            const title = document.getElementById('sf_title_' + code).value.trim();
            const url = document.getElementById('sf_url_' + code).value.trim();
            const kind = document.getElementById('sf_kind_' + code).value;
            const msg = form.querySelector('.sf-msg');

            if (!title) { msg.textContent = 'اكتب عنوان الملف'; msg.style.color = '#c0392b'; return; }
            if (!url) { msg.textContent = 'حط رابط الملف'; msg.style.color = '#c0392b'; return; }

            try {
                msg.textContent = 'جارٍ الإرسال...'; msg.style.color = '#777';
                await PTCAuth.submitFile(code, title, url, kind);
                msg.textContent = '✅ تم الإرسال — رح يظهر بعد موافقة المسؤول. بتقدر تضيف ملف تاني.';
                msg.style.color = '#2e7d32';
                document.getElementById('sf_title_' + code).value = '';
                document.getElementById('sf_url_' + code).value = '';
                // نخفي الرسالة بعد ٤ ثواني حتى يعرف إنه جاهز لإضافة جديدة
                setTimeout(function () { msg.textContent = ''; }, 4000);
            } catch (e) {
                msg.textContent = e.message || 'حصل خطأ';
                msg.style.color = '#c0392b';
            }
        };

        // النموذج لازم يكون بعد container.files مش جوّاه،
        // لأن renderFiles بيعمل innerHTML وبيمسح كل شي جوّا الـ container.
        // فبنحطه كأخ (sibling) بعده.
        var wrapper = container.parentElement;
        if (!wrapper) return;

        // لو الزر موجود من قبل (مثلاً بعد refresh) ما نكرّره
        if (wrapper.querySelector('.file-submit-btn')) return;

        wrapper.appendChild(btn);
        wrapper.appendChild(form);
    }

    async function loadAll() {
        for (const el of courseEls) {
            const id = el.id;                    // c_EEE43254
            const code = idToCode[id];           // EEE4 3254
            if (!code) continue;
            const filesBox = el.querySelector('.files');
            if (!filesBox) continue;
            try {
                const files = await PTCAuth.getCourseFiles(code);
                renderFiles(filesBox, files);
                addSubmitForm(filesBox, code);
            } catch (e) { /* نترك رسالة "قريباً" كما هي عند الخطأ */ }
        }
    }

    window.addEventListener('DOMContentLoaded', loadAll);
})();
// ============================================================
//  عرض الإعلانات في الصفحة الرئيسية (حسب سنة الطالب)
// ============================================================
(function announcementsLoader(){
  const section = document.getElementById('announcementsSection');
  const list = document.getElementById('announcementsList');
  if(!section || !list || typeof PTCAuth==='undefined') return;

  const yearName = y => ['','الأولى','الثانية','الثالثة','الرابعة'][y] || '';

  function render(items){
    if(!items || !items.length){ section.style.display='none'; return; }
    section.style.display='';
    list.innerHTML = items.map(function(n){
      var tag = n.year
        ? '<span style="font-family:var(--font-ar);font-size:11px;background:var(--olive);color:#fff;padding:2px 10px;border-radius:20px">سنة '+esc(yearName(n.year))+'</span>'
        : '<span style="font-family:var(--font-ar);font-size:11px;background:var(--copper);color:#fff;padding:2px 10px;border-radius:20px">عام</span>';
      var date = n.created_at ? new Date(n.created_at).toLocaleDateString('ar-EG-u-nu-latn') : '';
      return '<div class="reveal" style="border:1px solid var(--line);border-radius:16px;padding:18px 20px;background:var(--card)">'+
               '<div style="display:flex;justify-content:space-between;align-items:center;gap:10px;margin-bottom:8px">'+
                 '<b style="font-family:var(--font-ar);font-size:17px;color:var(--olive)">'+esc(n.title)+'</b>'+
                 tag+
               '</div>'+
               (n.body?'<div style="font-family:var(--font-ar);font-size:14px;color:var(--muted);line-height:1.7">'+esc(n.body)+'</div>':'')+
               (date?'<div style="font-family:var(--font-mono);font-size:11px;color:var(--faint);margin-top:8px">'+esc(date)+'</div>':'')+
             '</div>';
    }).join('');
    
    // الكروت انحقنت هلأ — لازم نعلّم عليها حتى تظهر
    if (window.revealScan) revealScan(list);
    if (window.renderIcons) window.renderIcons();
  }

  let loading = false;
  async function load(){
    if (loading) return;            // ما منسمح بنداءين متوازيين
    loading = true;
    try{
      const items = await PTCAuth.getAnnouncements();
      render(items);
    }
    catch(e){
      console.error('[Announcements] خطأ في تحميل الإعلانات:', e.message);
      section.style.display='none';
    }
    finally { loading = false; }
  }

  // نداء واحد بس، وبعد ما تجهز الجلسة — حتى نعرف سنة الطالب قبل الطلب.
  // لو حمّلنا قبل الجهوزية، السيرفر بيرجّع إعلانات كل السنوات وبعدين بتختفي.
  if (PTCAuth.isReady) {
    load();
  } else {
    window.addEventListener('ptc-auth-change', load, { once: true });
  }
})();
