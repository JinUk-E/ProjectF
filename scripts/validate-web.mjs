import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import vm from 'node:vm';
const root=path.resolve(process.argv[2]||'Builds/WebPublish'),base=process.argv[3]||'http://127.0.0.1:8173/';
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8').replace(/^\uFEFF/,''));
const checked=[];
for(const file of release.files){
  const response=await fetch(new URL(file.path,base));
  if(response.status!==200)throw Error(`HTTP ${response.status}: ${file.path}`);
  const bytes=Buffer.from(await response.arrayBuffer());
  if(bytes.length!==file.bytes||crypto.createHash('sha256').update(bytes).digest('hex')!==file.sha256)throw Error(`Hash mismatch: ${file.path}`);
  if(file.path.endsWith('.js'))new vm.Script(bytes.toString('utf8'));
  if(file.path.endsWith('.unityweb')){
    if(response.headers.has('content-encoding'))throw Error('Fallback files must be served as stored');
    const raw=zlib.gunzipSync(bytes);
    if(file.path.includes('.wasm.')&&!raw.subarray(0,4).equals(Buffer.from([0,97,115,109])))throw Error('Invalid wasm header');
    if(file.path.includes('.framework.js.'))new vm.Script(raw.toString('utf8'));
  }
  checked.push({path:file.path,bytes:bytes.length,status:response.status,type:response.headers.get('content-type')});
}
const html=fs.readFileSync(path.join(root,'index.html'),'utf8');
if(html.includes('{{{'))throw Error('Unexpanded template');
new vm.Script(html.match(/<script>([\s\S]*?)<\/script>/)[1]);
for(const match of html.matchAll(/Build\/([^'"\s]+)/g))if(!fs.existsSync(path.join(root,'Build',match[1])))throw Error(`Missing ${match[1]}`);
for(const match of html.matchAll(/(?:src|href)=["']([^"']+\.(?:js|css))["']/g)){
  if(!match[1].includes('://')&&!fs.existsSync(path.join(root,match[1])))throw Error(`Missing page asset ${match[1]}`);
}
const evidence={actor:'editor-tooling-codex',verifiedAt:new Date().toISOString(),baseUrl:base,sourceCommit:release.sourceCommit,httpAndCompression:'passed',browserGameplay:'not verified by this file-level check',totalBytes:checked.reduce((sum,f)=>sum+f.bytes,0),files:checked};
fs.writeFileSync(process.argv[4]||'Docs/Validation/web-http-files.json',JSON.stringify(evidence,null,2)+'\n');
console.log(JSON.stringify({files:checked.length,totalBytes:evidence.totalBytes,result:'HTTP/hash/gzip/wasm-header/template syntax passed'}));
