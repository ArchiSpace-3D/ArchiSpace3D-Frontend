import os

base_dir = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Resources\Images"

def process_svg(filename, new_filename):
    with open(os.path.join(base_dir, filename), "r", encoding="utf-8") as f:
        content = f.read()
    
    # Replace white stroke with dark stroke
    content = content.replace('stroke="#FFFFFF"', 'stroke="#0F172A"')
    
    with open(os.path.join(base_dir, new_filename), "w", encoding="utf-8") as f:
        f.write(content)

process_svg("ic_moon.svg", "ic_moon_dark.svg")
process_svg("ic_sun.svg", "ic_sun_dark.svg")

print("Created dark SVGs successfully.")
