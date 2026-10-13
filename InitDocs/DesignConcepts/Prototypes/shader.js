// Recall · Synaptic lattice background (from Stitch shader.html, ANIMATION_12), hardened:
// DPR cap, resize synced inside the frame (no clear-flash), pause when hidden/offscreen,
// prefers-reduced-motion => one static composed frame, light-theme variant, eased pointer.
export function mountLattice(canvas,{theme='dark',maxDpr=1.5,fps=60,staticT=14.0}={}){
 const gl=canvas.getContext('webgl',{antialias:false,alpha:false,premultipliedAlpha:false,preserveDrawingBuffer:false,powerPreference:'low-power'});
 if(!gl){canvas.style.background='var(--c-bg)';return{setTheme(){},destroy(){},stats:()=>({gl:false})}}
 const vs=`attribute vec2 a_position;void main(){gl_Position=vec4(a_position,0.,1.);}`;
 const fs=`precision highp float;uniform float u_time;uniform vec2 u_resolution;uniform vec2 u_mouse;uniform float u_light;
 vec2 hash2(vec2 p){float n=sin(dot(p,vec2(127.1,311.7)));float m=sin(dot(p,vec2(269.5,183.3)));return fract(vec2(n,m)*43758.5453);}
 void main(){
  vec2 uv=(gl_FragCoord.xy-.5*u_resolution.xy)/min(u_resolution.x,u_resolution.y);
  uv+= (u_mouse-.5)*.04; // gentle eased parallax
  vec3 base=mix(vec3(.047,.055,.075),vec3(.953,.957,.965),u_light);
  vec2 g=uv*6.5;vec2 id=floor(g);vec2 f=fract(g)-.5;float md=1.;
  for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){vec2 nb=vec2(float(x),float(y));vec2 p=hash2(id+nb);p=.5+.35*sin(u_time*.8+6.2831*p);md=min(md,length(nb+p-f-.5));}
  float core=smoothstep(.08,.01,md);float halo=smoothstep(.35,0.,md)*.4;
  float w1=sin(uv.x*4.+uv.y*2.-u_time*1.2)*.5+.5;float w2=cos(uv.x*-3.+uv.y*5.+u_time*.9)*.5+.5;float syn=pow(w1*w2,2.5);
  float vig=smoothstep(1.3,.3,length(uv));
  vec3 cyan=vec3(0.,.949,.996),vio=vec3(.54,.36,1.);
  float grid=max(smoothstep(.48,.5,abs(f.x)),smoothstep(.48,.5,abs(f.y)))*.06;
  float cg=1.-smoothstep(0.,1.2,length(uv-vec2(.35,.1)));
  vec3 dark=(vec3(.047,.055,.075)+(mix(cyan,vio,w2)*(core*1.4+halo+syn*.25)+cyan*grid+vio*cg*.15)*.65)*vig;
  vec3 ink=mix(vec3(0.,.42,.5),vec3(.38,.18,.7),w2);
  float amt=clamp((core*1.1+halo*.8+syn*.18)*.55+grid*.7+cg*.04,0.,.85);
  vec3 lite=mix(vec3(.955,.96,.968),ink,amt)*mix(.97,1.,vig);
  vec3 col=mix(dark,lite,u_light);
  gl_FragColor=vec4(col,1.);}`;
 const sh=(t,s)=>{const o=gl.createShader(t);gl.shaderSource(o,s);gl.compileShader(o);if(!gl.getShaderParameter(o,gl.COMPILE_STATUS))console.error(gl.getShaderInfoLog(o));return o};
 const pr=gl.createProgram();gl.attachShader(pr,sh(gl.VERTEX_SHADER,vs));gl.attachShader(pr,sh(gl.FRAGMENT_SHADER,fs));gl.linkProgram(pr);gl.useProgram(pr);
 const b=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,b);gl.bufferData(gl.ARRAY_BUFFER,new Float32Array([-1,-1,1,-1,-1,1,1,1]),gl.STATIC_DRAW);
 const ap=gl.getAttribLocation(pr,'a_position');gl.enableVertexAttribArray(ap);gl.vertexAttribPointer(ap,2,gl.FLOAT,false,0,0);
 const U={t:gl.getUniformLocation(pr,'u_time'),r:gl.getUniformLocation(pr,'u_resolution'),m:gl.getUniformLocation(pr,'u_mouse'),l:gl.getUniformLocation(pr,'u_light')};
 const mq=matchMedia('(prefers-reduced-motion: reduce)');let reduced=mq.matches||/[?&]reduced/.test(location.search);
 let lastTs=0,light=theme==='light'?1:0,lightT=light,mx=.5,my=.5,tx=.5,ty=.5,raf=0,t0=performance.now(),acc=staticT,last=0,visible=true,frames=0,deltas=[];
 const dpr=()=>Math.min(window.devicePixelRatio||1,maxDpr);let want=[0,0];
 const measure=()=>{want=[Math.max(1,Math.round(canvas.clientWidth*dpr())),Math.max(1,Math.round(canvas.clientHeight*dpr()))]};
 function draw(){if(canvas.width!==want[0]||canvas.height!==want[1]){canvas.width=want[0];canvas.height=want[1]}
  gl.viewport(0,0,canvas.width,canvas.height);gl.uniform1f(U.t,acc);gl.uniform2f(U.r,canvas.width,canvas.height);gl.uniform2f(U.m,mx,my);gl.uniform1f(U.l,light);gl.drawArrays(gl.TRIANGLE_STRIP,0,4);frames++}
 function loop(now){raf=0;lastTs=performance.now();const dt=last?Math.min(.05,(now-last)/1000):0;if(last&&now-last<1000/fps-1){raf=requestAnimationFrame(loop);return}
  if(last)deltas.push(now-last);if(deltas.length>240)deltas.shift();last=now;acc+=dt*.6; // slower than original: calmer
  const k=1-Math.exp(-dt*2.5);mx+=(tx-mx)*k;my+=(ty-my)*k;light+=(lightT-light)*(1-Math.exp(-dt*6));
  draw();if(!reduced&&visible&&!document.hidden)raf=requestAnimationFrame(loop)}
 function wake(){if(reduced){light=lightT;draw();return}if(raf&&performance.now()-lastTs>400){cancelAnimationFrame(raf);raf=0}if(!raf&&visible&&!document.hidden){last=0;raf=requestAnimationFrame(loop)}}
 const ro=new ResizeObserver(()=>{measure();if(reduced||!raf)draw()});ro.observe(canvas);measure();
 const io=new IntersectionObserver(e=>{visible=e[0].isIntersecting;visible?wake():(cancelAnimationFrame(raf),raf=0)});io.observe(canvas);
 const vis=()=>{if(document.hidden){cancelAnimationFrame(raf);raf=0}else wake()};document.addEventListener('visibilitychange',vis);document.addEventListener('resume',vis);addEventListener('pageshow',vis);addEventListener('focus',vis);
 const pm=e=>{tx=e.clientX/innerWidth;ty=1-e.clientY/innerHeight};addEventListener('pointermove',pm,{passive:true});
 mq.addEventListener?.('change',e=>{reduced=e.matches;wake()});
 draw();wake();
 return{setTheme(t){lightT=t==='light'?1:0;wake()},advance(dt){acc+=dt*.6;light+=(lightT-light)*(1-Math.exp(-dt*6));draw()},set reduced(v){reduced=v;wake()},
  stats:()=>{const d=deltas.slice(-120);return{frames,avg:d.reduce((a,b)=>a+b,0)/(d.length||1),max:Math.max(0,...d),running:!!raf,dpr:dpr(),size:[canvas.width,canvas.height]}},
  destroy(){cancelAnimationFrame(raf);ro.disconnect();io.disconnect();document.removeEventListener('visibilitychange',vis);removeEventListener('pointermove',pm)}};
}
