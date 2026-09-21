import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Controls\FloatingTabBar.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Hook into the Loaded event to ensure visuals are updated after XAML is fully parsed
constructor_block = """    public FloatingTabBar()
    {
        InitializeComponent();
        UpdateVisualStates(SelectedIndex);
        this.Loaded += (s, e) => UpdateVisualStates(SelectedIndex);
    }"""
content = re.sub(r'public FloatingTabBar\(\)\s*\{.*?UpdateVisualStates\(SelectedIndex\);\s*\}', constructor_block, content, flags=re.DOTALL)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
