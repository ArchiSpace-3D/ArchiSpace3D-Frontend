import os
import re

base_dir = r'c:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1'
views_dir = os.path.join(base_dir, 'Views')

os.makedirs(views_dir, exist_ok=True)

pages = ['AdminUsersPage', 'ARPage', 'DashboardPage', 'DesignPage', 'LoginPage', 'MainPage', 'NotificationsPage', 'ProfilePage', 'RegisterPage']

def read_file(path):
    encodings = ['utf-8', 'utf-8-sig', 'latin-1', 'cp1252']
    for e in encodings:
        try:
            with open(path, 'r', encoding=e) as f:
                return f.read(), e
        except UnicodeDecodeError:
            pass
    raise Exception(f"Could not read {path}")

def write_file(path, content, enc):
    with open(path, 'w', encoding=enc) as f:
        f.write(content)

# Move files
for page in pages:
    for ext in ['.xaml', '.xaml.cs']:
        src = os.path.join(base_dir, page + ext)
        dst = os.path.join(views_dir, page + ext)
        if os.path.exists(src):
            os.rename(src, dst)

# Update namespaces in .xaml.cs
for page in pages:
    cs_path = os.path.join(views_dir, page + '.xaml.cs')
    if os.path.exists(cs_path):
        content, enc = read_file(cs_path)
        content = re.sub(r'namespace MauiApp1\s*\{', 'namespace MauiApp1.Views\n{', content)
        content = re.sub(r'namespace MauiApp1;', 'namespace MauiApp1.Views;', content)
        # Also fix any "using MauiApp1.Views;" if needed, we'll do it globally later
        write_file(cs_path, content, enc)

# Update x:Class in .xaml
for page in pages:
    xaml_path = os.path.join(views_dir, page + '.xaml')
    if os.path.exists(xaml_path):
        content, enc = read_file(xaml_path)
        content = re.sub(r'x:Class="MauiApp1\.' + page + r'"', f'x:Class="MauiApp1.Views.{page}"', content)
        write_file(xaml_path, content, enc)

# Update AppShell.xaml
appshell_path = os.path.join(base_dir, 'AppShell.xaml')
appshell_content, enc = read_file(appshell_path)
if 'xmlns:views=' not in appshell_content:
    appshell_content = appshell_content.replace('xmlns:local="clr-namespace:MauiApp1"', 'xmlns:local="clr-namespace:MauiApp1"\n    xmlns:views="clr-namespace:MauiApp1.Views"')
appshell_content = re.sub(r'local:(DashboardPage|DesignPage|MainPage|ProfilePage)', r'views:\1', appshell_content)
write_file(appshell_path, appshell_content, enc)

print("Files moved and namespaces updated successfully.")
