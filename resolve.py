# -*- coding: utf-8 -*-
import re

with open('archie-prueba1/MauiApp1/LoginPage.xaml', 'r', encoding='utf-8') as f:
    content = f.read()

def replacer(match):
    return match.group(1)

content = re.sub(r'<<<<<<< HEAD\n(.*?)\n=======\n.*?\n>>>>>>> origin/main\n?', replacer, content, flags=re.DOTALL)

with open('archie-prueba1/MauiApp1/LoginPage.xaml', 'w', encoding='utf-8') as f:
    f.write(content)
