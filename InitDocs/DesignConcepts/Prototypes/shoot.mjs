import puppeteer from 'puppeteer-core';
const b=await puppeteer.launch({executablePath:'/usr/bin/google-chrome',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--no-sandbox']});
const V={big:[1600,1000],mid:[1024,768],phone:[390,844]};
const only=process.argv[2];
const L=[['home','dark','en','big'],['home','light','ar','big'],['home','dark','en','phone'],['home','light','ar','phone'],['home','dark','ar','mid'],['welcome','dark','en','big'],['welcome','light','ar','phone'],['folders','dark','en','mid'],['folders','light','ar','big'],['model','dark','en','big','&fail'],['model','light','ar','phone'],['ready','dark','en','mid'],['lattice','dark','en','big'],['lattice','light','en','phone']];
const errs=[];
for(const [s,th,lg,v,x=''] of L){const n=`${s}-${th}-${lg}-${v}`;if(only&&!n.includes(only))continue;
 const p=await b.newPage();p.on('console',m=>m.type()==='error'&&errs.push(n+': '+m.text()));p.on('pageerror',e=>errs.push(n+': '+e.message));
 await p.setViewport({width:V[v][0],height:V[v][1],deviceScaleFactor:1});
 await p.evaluateOnNewDocument(()=>localStorage.clear());
 await p.goto(`http://localhost:8780/index.html?s=${s}&theme=${th}&lang=${lg}${x}`,{waitUntil:'networkidle0'});
 await p.evaluate(()=>document.fonts.ready);await new Promise(r=>setTimeout(r,900));
 await p.screenshot({path:`shots/${n}.png`});await p.close();console.log(n)}
console.log(JSON.stringify(errs.filter(e=>!e.includes('cdn.tailwindcss'))));await b.close();
