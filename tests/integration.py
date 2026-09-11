"""Deterministic HTTP contract tests. No external provider requests are made."""
import http.cookiejar,json,os,socket,subprocess,tempfile,time,urllib.request,urllib.error
from pathlib import Path
root=Path(__file__).resolve().parents[1]
with socket.socket() as sock:
 sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
base=f'http://127.0.0.1:{port}'
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(path,body=None,code=200,client=opener):
 req=urllib.request.Request(base+path,data=None if body is None else json.dumps(body).encode(),headers={'Content-Type':'application/json'})
 try:
  with client.open(req,timeout=30) as res: status=res.status; data=res.read()
 except urllib.error.HTTPError as ex: status=ex.code;data=ex.read()
 assert status==code,(path,status,data.decode()[:500])
 return json.loads(data) if data else None
with tempfile.TemporaryDirectory() as directory:
 env={**os.environ,'ASPNETCORE_URLS':base,'ASPNETCORE_ENVIRONMENT':'Testing','TestFixtures':'true','DATA_DIR':directory}
 with open(Path(directory)/'server.log','w+') as log:
  process=subprocess.Popen([os.environ.get('DOTNET','dotnet'),str(root/'src/bin/Release/net10.0/App.dll'),'--contentRoot',str(root/'src')],env=env,stdout=log,stderr=log,cwd=root)
  try:
   for _ in range(100):
    try:
     assert request('/api/health')['mode']=='test fixtures';break
    except (OSError,AssertionError): time.sleep(.1)
   else: raise RuntimeError('Server did not start')
   body={'country':'DE','vat':'DE 123 456 789','iban':'DE89 3704 0044 0532 0130 00'}
   a=request('/api/validate',body);assert a['result']['vat']['status']=='valid' and a['result']['iban']['status']=='valid'
   b=request('/api/validate',body);assert b['result']['vat']['cached'] and b['result']['vat']['checkedAt']==a['result']['vat']['checkedAt']
   record=request('/api/audit/'+a['id']);assert '123456789' not in json.dumps(record) and '37040044' not in json.dumps(record)
   assert request('/api/validate',{'country':'DE','vat':'111111111'})['result']['vat']['status']=='invalid'
   assert request('/api/validate',{'country':'DE','vat':'999999999'})['result']['vat']['status']=='provider_unavailable'
   assert request('/api/validate',{'country':'ZZ','vat':'123'})['result']['vat']['status']=='invalid_input'
   assert request('/api/validate',{})['result']['vat']['status']=='not_checked'
   request('/api/batch',[{}]*11,400)
   assert len(request('/api/batch',[{'country':'DE','vat':'123456789'},{}]))==2
   print('HTTP VAT states, cache timestamps, IBAN, masking and batch limits passed.')
  finally:
   process.terminate()
   try:process.wait(timeout=10)
   except subprocess.TimeoutExpired:process.kill();process.wait()
