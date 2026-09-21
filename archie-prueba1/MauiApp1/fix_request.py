import os

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Models\ApiModels.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

old_req = """    public class ActualizarProyectoRequest
    {
        [JsonPropertyName("nombre")]"""

new_req = """    public class ActualizarProyectoRequest
    {
        [JsonPropertyName("idproyecto")]
        public int Idproyecto { get; set; }

        [JsonPropertyName("nombre")]"""

content = content.replace(old_req, new_req)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
