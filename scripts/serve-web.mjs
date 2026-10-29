// Local-only preview of the same static files published to the game host.
import http from 'node:http';
import {createReadStream,statSync} from 'node:fs';
import path from 'node:path';
const root=path.resolve(process.argv[2]||'Builds/WebPublish');
const port=Number(process.argv[3]||8173);
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json','.txt':'text/plain; charset=utf-8','.css':'text/css','.png':'image/png','.unityweb':'application/octet-stream'};
http.createServer((request,response)=>{
  try{
    if(!['GET','HEAD'].includes(request.method)){response.writeHead(405);response.end();return;}
    const relative=decodeURIComponent(new URL(request.url,'http://localhost').pathname).replace(/^\/+/, '');
    let target=path.resolve(root,relative);
    if(target!==root&&!target.startsWith(root+path.sep)){response.writeHead(403);response.end();return;}
    if(statSync(target).isDirectory())target=path.join(target,'index.html');
    const stat=statSync(target);
    response.writeHead(200,{'Content-Type':types[path.extname(target)]||'application/octet-stream','Content-Length':stat.size,'Cache-Control':'no-cache','X-Content-Type-Options':'nosniff'});
    if(request.method==='HEAD')response.end();else createReadStream(target).pipe(response);
  }catch{response.writeHead(404);response.end('Not found');}
}).listen(port,'127.0.0.1',()=>console.log(`ProjectF preview: http://127.0.0.1:${port}/`));
