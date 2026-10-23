// Exportaciones técnicas del original aprobado. Requiere Node y sharp 0.34+.
// No regenera el dibujo con IA. SHARP_MODULE permite usar un runtime externo sin añadir dependencias al juego.
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync, mkdirSync, copyFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
const require = createRequire(import.meta.url);
const sharp = require(process.env.SHARP_MODULE || 'sharp');
const root = dirname(fileURLToPath(import.meta.url));
const repo = resolve(root, '../../../..');
const master = join(root, 'source/tintalu-master-aprobado.png');
const cutout = join(root, 'source/tintalu-simbolo-transparente.png');
const mono = join(root, 'source/tintalu-monocromo.png');
const font = join(repo, 'Assets/_Project/Art/Fonts/Baloo2-ExtraBold.ttf');
if (readFileSync(font).subarray(0,40).toString().startsWith('version https://git-lfs')) throw Error('Descargar la fuente con git lfs pull antes de exportar; no usar un fallback tipográfico.');
const bg = '#142C36';
const put = (name, data) => { const p=join(root,name); mkdirSync(dirname(p),{recursive:true}); writeFileSync(p,data); };
const json = (name,data) => put(name,JSON.stringify(data,null,2)+'\n');
const hash = b => createHash('sha256').update(b).digest('hex');
const png = pipeline => pipeline.toColourspace('srgb').withIccProfile('srgb').png({compressionLevel:9,palette:false});
const rgba = async (input,size) => png(sharp(input).resize(size,size).ensureAlpha()).toBuffer();
const rgb = async (input,size) => png(sharp(input).resize(size,size).flatten({background:bg}).removeAlpha()).toBuffer();
const transparent = (w,h) => sharp({create:{width:w,height:h,channels:4,background:{r:0,g:0,b:0,alpha:0}}});

// Tienda y web: misma imagen y encuadre aprobado, sin máscaras incorporadas.
put('store/google-play-512.png',await rgba(master,512));
put('store/app-store-1024.png',await rgb(master,1024));
put('social/avatar-1024.png',await rgba(master,1024));
for (const size of [16,32,48,96,180,192,512]) put(`web/icon-${size}.png`,await rgba(master,size));
// ICO con PNG en 16/32/48, sin redibujar el símbolo.
const icoImages=await Promise.all([16,32,48].map(s=>rgba(master,s)));
const icoHeader=Buffer.alloc(6+16*icoImages.length); icoHeader.writeUInt16LE(1,2); icoHeader.writeUInt16LE(icoImages.length,4);
let offset=icoHeader.length;
icoImages.forEach((b,i)=>{const k=6+16*i; icoHeader[k]=[16,32,48][i]; icoHeader[k+1]=icoHeader[k]; icoHeader.writeUInt16LE(1,k+4); icoHeader.writeUInt16LE(32,k+6); icoHeader.writeUInt32LE(b.length,k+8); icoHeader.writeUInt32LE(offset,k+12); offset+=b.length;});
put('web/favicon.ico',Buffer.concat([icoHeader,...icoImages]));
json('web/site.webmanifest',{name:'Tintalú',short_name:'Tintalú',lang:'es',start_url:'/',display:'browser',background_color:bg,theme_color:bg,icons:[192,512].map(s=>({src:`icon-${s}.png`,sizes:`${s}x${s}`,type:'image/png',purpose:'any'}))});
put('web/head-snippet.html','<link rel="icon" href="/assets/brand/favicon.ico">\n<link rel="icon" type="image/png" sizes="32x32" href="/assets/brand/icon-32.png">\n<link rel="apple-touch-icon" sizes="180x180" href="/assets/brand/icon-180.png">\n<link rel="manifest" href="/assets/brand/site.webmanifest">\n<meta name="theme-color" content="#142C36">\n');

// iOS/iPadOS: catálogo Single Size, con apariencias any/dark/tinted.
const ios='ios/Assets.xcassets';
json(`${ios}/Contents.json`,{info:{author:'xcode',version:1}});
put(`${ios}/AppIcon.appiconset/AppIcon-1024.png`,await rgb(master,1024));
put(`${ios}/AppIcon.appiconset/AppIcon-dark-1024.png`,await rgba(cutout,1024));
put(`${ios}/AppIcon.appiconset/AppIcon-tinted-1024.png`,await png(sharp(master).resize(1024,1024).flatten({background:bg}).removeAlpha().greyscale()).toBuffer());
json(`${ios}/AppIcon.appiconset/Contents.json`,{images:[
 {filename:'AppIcon-1024.png',idiom:'universal',platform:'ios',size:'1024x1024'},
 {filename:'AppIcon-dark-1024.png',idiom:'universal',platform:'ios',size:'1024x1024',appearances:[{appearance:'luminosity',value:'dark'}]},
 {filename:'AppIcon-tinted-1024.png',idiom:'universal',platform:'ios',size:'1024x1024',appearances:[{appearance:'luminosity',value:'tinted'}]}
],info:{author:'xcode',version:1}});

// Android: las imágenes adaptativas usan lienzo de 108 dp (432 px a xxxhdpi).
// Limitar el radio real de la silueta a 32 dp, dentro del círculo seguro de 66 dp.
async function safeForeground(input){
 const {data,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let radius=0;
 for(let y=0;y<info.height;y++) for(let x=0;x<info.width;x++) if(data[(y*info.width+x)*4+3]>=32) radius=Math.max(radius,Math.hypot(x-info.width/2,y-info.height/2));
 const scale=Math.min(1,128/radius), w=Math.round(info.width*scale),h=Math.round(info.height*scale);
 const resized=await sharp(input).resize(w,h).png().toBuffer();
 return {buffer:await transparent(432,432).composite([{input:resized,left:Math.floor((432-w)/2),top:Math.floor((432-h)/2)}]).png().toBuffer(),radiusPx:radius,scale};
}
const fg=await safeForeground(cutout), mg=await safeForeground(mono);
put('android/layers/foreground-432.png',fg.buffer);
put('android/layers/monochrome-432.png',mg.buffer);
const background=await sharp({create:{width:432,height:432,channels:3,background:bg}}).png().toBuffer();
put('android/layers/background-432.png',background);
const adaptive=await sharp(background).composite([{input:fg.buffer}]).png().toBuffer();
put('android/layers/composite-432-preview.png',adaptive);
const densities=[['mdpi',1],['hdpi',1.5],['xhdpi',2],['xxhdpi',3],['xxxhdpi',4]];
for(const [density,mult] of densities){
 put(`android/res/mipmap-${density}/ic_launcher.png`,await rgba(master,Math.round(48*mult)));
 put(`android/res/drawable-${density}/ic_launcher_foreground.png`,await rgba(fg.buffer,Math.round(108*mult)));
 put(`android/res/drawable-${density}/ic_launcher_monochrome.png`,await rgba(mg.buffer,Math.round(108*mult)));
}
put('android/res/values/colors.xml','<?xml version="1.0" encoding="utf-8"?>\n<resources><color name="ic_launcher_background">#142C36</color></resources>\n');
const xml=(themed=false)=>`<?xml version="1.0" encoding="utf-8"?>\n<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">\n  <background android:drawable="@color/ic_launcher_background" />\n  <foreground android:drawable="@drawable/ic_launcher_foreground" />\n${themed?'  <monochrome android:drawable="@drawable/ic_launcher_monochrome" />\n':''}</adaptive-icon>\n`;
put('android/res/mipmap-anydpi-v26/ic_launcher.xml',xml());
put('android/res/mipmap-anydpi-v33/ic_launcher.xml',xml(true));
put('android/manifest-snippet.xml','<!-- Dentro de application; no sustituye el manifest completo. -->\n<application android:icon="@mipmap/ic_launcher" />\n');

// Composiciones tipográficas: símbolo recortado + Baloo 2 ExtraBold del juego.
// PNG, sin afirmar que el dibujo sea vectorial.
for(const [theme,color] of [['claro','#142C36'],['oscuro','#FFF3D6']]){
 const text=await sharp({text:{text:`<span foreground="${color}">Tintalú</span>`,font:'Baloo 2 ExtraBold 210',fontfile:font,rgba:true}}).png().toBuffer({resolveWithObject:true});
 const symbol=await sharp(cutout).resize(432,432).png().toBuffer();
 const horizontal=await transparent(1536,480).composite([{input:symbol,left:24,top:24},{input:text.data,left:500,top:Math.round((480-text.info.height)/2)}]).png().toBuffer();
 put(`logo/tintalu-horizontal-fondo-${theme}.png`,horizontal);
 const stackedText=await sharp({text:{text:`<span foreground="${color}">Tintalú</span>`,font:'Baloo 2 ExtraBold 160',fontfile:font,rgba:true}}).png().toBuffer({resolveWithObject:true});
 const stacked=await transparent(1024,1200).composite([{input:await sharp(cutout).resize(880,880).png().toBuffer(),left:72,top:40},{input:stackedText.data,left:Math.round((1024-stackedText.info.width)/2),top:990}]).png().toBuffer();
 put(`logo/tintalu-vertical-fondo-${theme}.png`,stacked);
}
mkdirSync(join(root,'licenses'),{recursive:true});
put('licenses/OFL-Baloo2.txt',readFileSync(join(repo,'Assets/_Project/Art/Fonts/OFL-Baloo2.txt'),'utf8').split(/\r?\n/).map(line=>line.trimEnd()).join('\n').trimEnd()+'\n');

// Planchas de QA con los PNG reales exportados: ninguna maqueta generativa.
const svg=(w,h,body)=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}">${body}</svg>`);
const label=(s,w=600)=>svg(w,36,`<text x="0" y="26" font-family="sans-serif" font-size="21" fill="#243940">${s}</text>`);
const mask=async(input,size,shape)=>{
 const img=await sharp(input).resize(size,size).png().toBuffer();
 const form=shape==='circle'?`<circle cx="${size/2}" cy="${size/2}" r="${size/2}" fill="white"/>`:`<rect width="${size}" height="${size}" rx="${size*.24}" fill="white"/>`;
 return sharp(img).composite([{input:svg(size,size,form),blend:'dest-in'}]).png().toBuffer();
};
const iosIcon=await mask(master,240,'rounded');
// El viewport visible aproximado de adaptive es el centro de 72 dp de los 108 dp.
const viewport=await sharp(adaptive).extract({left:72,top:72,width:288,height:288}).png().toBuffer();
const themedInk=await sharp({create:{width:432,height:432,channels:4,background:'#173D36'}}).composite([{input:mg.buffer,blend:'dest-in'}]).png().toBuffer();
const monoComposite=await sharp({create:{width:432,height:432,channels:3,background:'#CDE9DB'}}).composite([{input:themedInk}]).png().toBuffer();
const monoViewport=await sharp(monoComposite).extract({left:72,top:72,width:288,height:288}).png().toBuffer();
const items=[{input:label('Tintalú · paquete oficial v1',1000),left:50,top:35}];
for(const [i,title,input,shape] of [[0,'iOS / Play',master,'rounded'],[1,'Android circular',viewport,'circle'],[2,'Android redondeado',viewport,'rounded'],[3,'Android temático',monoViewport,'circle']]){
 items.push({input:await mask(input,240,shape),left:50+i*280,top:105});
 items.push({input:label(title,270),left:50+i*280,top:365});
}
items.push({input:label('Lectura a tamaño real: 16 / 32 / 48 / 96 px',1000),left:50,top:435});
let x=50; for(const s of [16,32,48,96]) {items.push({input:await mask(master,s,'rounded'),left:x,top:500});x+=s+55;}
const lh=await sharp(join(root,'logo/tintalu-horizontal-fondo-claro.png')).resize(1000).png().toBuffer();
items.push({input:lh,left:50,top:660});
put('preview/identidad-y-recortes.png',await sharp({create:{width:1220,height:1030,channels:3,background:'#EEF3EF'}}).composite(items).png().toBuffer());
put('preview/ios-240.png',iosIcon);
// Verificaciones de formato y referencias; no sustituyen actool/Xcode ni build Unity.
const play=await sharp(join(root,'store/google-play-512.png')).metadata();
const apple=await sharp(join(root,'store/app-store-1024.png')).metadata();
if(play.width!==512||play.height!==512||play.channels!==4||statSync(join(root,'store/google-play-512.png')).size>1024*1024) throw Error('Play no cumple dimensiones/canales/peso');
if(apple.width!==1024||apple.height!==1024||apple.hasAlpha) throw Error('App Store debe ser RGB 1024 sin alpha');
const iosManifest=JSON.parse(readFileSync(join(root,`${ios}/AppIcon.appiconset/Contents.json`),'utf8'));
for(const e of iosManifest.images) await sharp(join(root,`${ios}/AppIcon.appiconset`,e.filename)).metadata();
json('validation.json',{masterSha256:hash(readFileSync(master)),play:{width:play.width,height:play.height,channels:play.channels,bytes:statSync(join(root,'store/google-play-512.png')).size,space:play.space},ios:{width:apple.width,height:apple.height,channels:apple.channels,hasAlpha:apple.hasAlpha,appearances:iosManifest.images.length},android:{layerPx:432,layerDp:108,safeContentRadiusPx:128,foregroundScale:fg.scale,monochromeScale:mg.scale},validated:'Formatos, alpha, tamaño Play, referencias de catálogo y generación determinista. Pendiente build de Xcode/Unity y prueba física.'});
function files(dir){return readdirSync(dir).sort().flatMap(n=>{const p=join(dir,n);return statSync(p).isDirectory()?files(p):[p];});}
const inventory=files(root).filter(p=>!p.endsWith('manifest-sha256.json')).map(p=>({path:relative(root,p).split('\\').join('/'),bytes:statSync(p).size,sha256:hash(readFileSync(p))}));
json('manifest-sha256.json',inventory);
console.log(`Paquete generado: ${inventory.length+1} archivos. Play ${statSync(join(root,'store/google-play-512.png')).size} bytes; iOS 1024 RGB.`);
