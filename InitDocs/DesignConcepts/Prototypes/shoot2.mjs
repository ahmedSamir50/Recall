import puppeteer from 'puppeteer-core';
const b=await puppeteer.launch({executablePath:'/usr/bin/google-chrome',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--no-sandbox']});
for(const [n,q,w,h,full] of [['landing-dark-en-big','',1440,900,1],['landing-light-ar-phone','?lang=ar&theme=light',390,844,1]]){
const p=await b.newPage();await p.setViewport({width:w,height:h});await p.goto('http://localhost:8780/landing.html'+q,{waitUntil:'networkidle0'});await new Promise(r=>setTimeout(r,900));
await p.screenshot({path:`shots/${n}.png`,fullPage:!!full});console.log(n)}await b.close();
