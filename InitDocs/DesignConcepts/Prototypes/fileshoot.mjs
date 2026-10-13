import puppeteer from 'puppeteer-core';
const b=await puppeteer.launch({executablePath:'/usr/bin/google-chrome',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--no-sandbox']});
const base='file:///workspace/stitch-enhanced/';const out={};
for(const [f,w,h] of [['index',1280,900],['home',1600,1000],['home',390,844],['onboarding',1280,800],['graph',1280,800],['model-fail',1280,800],['landing',1280,800],['lab',1280,800]]){
 const p=await b.newPage();const e=[];p.on('pageerror',x=>e.push(x.message));p.on('console',m=>m.type()==='error'&&e.push(m.text()));
 await p.setViewport({width:w,height:h});await p.goto(base+f+'.html',{waitUntil:'networkidle0'});await new Promise(r=>setTimeout(r,1200));
 const n=`shots/file-${f}-${w}.png`;await p.screenshot({path:n});out[n]={errs:e,title:await p.title(),hasShell:await p.$('nav[aria-label=Main]')!==null,canvas:await p.evaluate(()=>!!document.querySelector('canvas'))};await p.close()}
// navigation test inside home
const p=await b.newPage();await p.setViewport({width:1280,height:800});await p.goto(base+'home.html',{waitUntil:'networkidle0'});
await p.click('nav[aria-label=Main] [data-go=lattice]');out.nav1=await p.evaluate(()=>!!document.querySelector('svg line'));
await p.click('nav[aria-label=Main] [data-go=welcome]');out.nav2=await p.evaluate(()=>document.body.innerText.includes('Choose folders'));
console.log(JSON.stringify(out,null,1));await b.close();
