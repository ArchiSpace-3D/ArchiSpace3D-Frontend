import urllib.request
import json

url = "https://archispace3d-backend-production.up.railway.app/api/Medicion"
data = {
    "idproyecto": 11, # Assuming 11 is a valid project, or it might fail with 404/403. Let's see what it says.
    "puntoinicial": "{\"x\":0, \"y\":0, \"z\":0}",
    "puntofinal": "{\"x\":0, \"y\":0, \"z\":0}",
    "distancia": 10.5,
    "fechamedicion": "2026-09-22T00:00:00Z",
    "etapa": "Test",
    "partida": "Test",
    "descripcion": "Test",
    "veces": 1,
    "largo": 10.5,
    "ancho": 1.0,
    "alto": 1.0,
    "unidad": "m",
    "totalparcial": 10.5
}

req = urllib.request.Request(url, data=json.dumps(data).encode('utf-8'), method='POST')
req.add_header('Content-Type', 'application/json')
# We don't have a valid token, so it will probably return 401 Unauthorized.
try:
    with urllib.request.urlopen(req) as response:
        print(response.read().decode())
except urllib.error.HTTPError as e:
    print(f"HTTP Error: {e.code}")
    print(e.read().decode())
