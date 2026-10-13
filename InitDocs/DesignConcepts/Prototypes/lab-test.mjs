import puppeteer from 'puppeteer-core';
const b=await puppeteer.launch({executablePath:'/usr/bin/google-chrome',args:['--no-sandbox','--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
const p=await b.newPage();await p.setViewport({width:1280,height:800});const errs=[];p.on('console',m=>m.type()=='error'&&errs.push(m.text()));p.on('pageerror',e=>errs.push(e.message));
await p.goto('http://localhost:8780/lab.html',{waitUntil:'networkidle0'});await new Promise(r=>setTimeout(r,1500));
// flash check: mean luminance of many frames (sampled via canvas readback from screenshot)
const lum=[];for(let i=0;i<16;i++){const buf=await p.screenshot({type:'png',clip:{x:300,y:200,width:400,height:300}});lum.push(buf.length);
 if(i==5)await p.evaluate(()=>document.getElementById('rs').click());if(i==9)await p.setViewport({width:1000,height:700});if(i==12)await p.evaluate(()=>document.getElementById('rs').click());await new Promise(r=>setTimeout(r,150))}
await p.setViewport({width:1280,height:800});await new Promise(r=>setTimeout(r,1200));await p.screenshot({path:'shots/lab-dark.png'});
const s1=await p.evaluate(()=>__lab.stats());
// hidden-tab pause: emulate by dispatching visibility via CDP focus emulation is not possible; use page lifecycle freeze
const cdp=await p.createCDPSession();const fA=await p.evaluate(()=>__lab.stats().frames);await cdp.send('Page.setWebLifecycleState',{state:'frozen'});await new Promise(r=>setTimeout(r,800));await cdp.send('Page.setWebLifecycleState',{state:'active'});const fB=await p.evaluate(()=>__lab.stats().frames);await p.bringToFront();const hid=await p.evaluate(()=>document.hidden);console.log('freeze frames delta',fB-fA,'hidden after',hid);
await p.evaluate(()=>document.getElementById('th').click());await new Promise(r=>setTimeout(r,1500));await p.screenshot({path:'shots/lab-light.png'});
await p.goto('http://localhost:8780/lab.html?reduced',{waitUntil:'networkidle0'});await new Promise(r=>setTimeout(r,1500));const red=await p.evaluate(()=>__lab.stats());await p.screenshot({path:'shots/lab-reduced.png'});
console.log(JSON.stringify({s1,red,errs,pngSizes:lum}));await b.close();
