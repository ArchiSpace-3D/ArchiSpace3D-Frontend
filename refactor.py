import os
import re

base_dir = r'c:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1'
views_dir = os.path.join(base_dir, 'Views')

# Create Views directory
os.makedirs(views_dir, exist_ok=True)

pages = [
    'AdminUsersPage',
    'ARPage',
    'DashboardPage',
    'DesignPage',
    'LoginPage',
    'MainPage',
    'NotificationsPage',
    'ProfilePage',
    'RegisterPage'
]

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
        with open(cs_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Replace traditional namespace
        content = re.sub(r'namespace MauiApp1\s*\{', 'namespace MauiApp1.Views\n{', content)
        # Replace file-scoped namespace
        content = re.sub(r'namespace MauiApp1;', 'namespace MauiApp1.Views;', content)
        
        with open(cs_path, 'w', encoding='utf-8') as f:
            f.write(content)

# Update x:Class in .xaml
for page in pages:
    xaml_path = os.path.join(views_dir, page + '.xaml')
    if os.path.exists(xaml_path):
        with open(xaml_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        content = re.sub(r'x:Class="MauiApp1\.' + page + r'"', f'x:Class="MauiApp1.Views.{page}"', content)
        
        with open(xaml_path, 'w', encoding='utf-8') as f:
            f.write(content)

# Update AppShell.xaml
appshell_path = os.path.join(base_dir, 'AppShell.xaml')
with open(appshell_path, 'r', encoding='utf-8') as f:
    appshell_content = f.read()

if 'xmlns:views=' not in appshell_content:
    appshell_content = appshell_content.replace('xmlns:local="clr-namespace:MauiApp1"', 
                                                'xmlns:local="clr-namespace:MauiApp1"\n    xmlns:views="clr-namespace:MauiApp1.Views"')

appshell_content = re.sub(r'local:(DashboardPage|DesignPage|MainPage|ProfilePage)', r'views:\1', appshell_content)
with open(appshell_path, 'w', encoding='utf-8') as f:
    f.write(appshell_content)

# Update MauiProgram.cs to use global usings or update specific files
# Instead of modifying every single CS file, we can add a GlobalUsings.cs or just add using in App.xaml.cs and MauiProgram.cs
print("Files moved and namespaces updated successfully.")
