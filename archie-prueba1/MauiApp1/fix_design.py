import os

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Using python's find to precisely slice it
start_tag = '<Border x:Name="EditProjectSheetModal"'
end_tag = '<BoxView x:Name="JoinCodeBackdrop"'

start_idx = content.find(start_tag)
end_idx = content.find(end_tag, start_idx)

if start_idx != -1 and end_idx != -1:
    old_modal = content[start_idx:end_idx]
    
    new_modal = """<Border x:Name="EditProjectSheetModal" BackgroundColor="{AppThemeBinding Light=#FFFFFF, Dark=#1E1E1E}" StrokeThickness="0" VerticalOptions="Center" HorizontalOptions="Fill" Margin="25" IsVisible="False" Opacity="0" Scale="0.9">
            <Border.StrokeShape><RoundRectangle CornerRadius="28"/></Border.StrokeShape>
            <Border.Shadow><Shadow Brush="Black" Offset="0,10" Radius="30" Opacity="0.2"/></Border.Shadow>

            <Grid RowDefinitions="Auto,Auto,Auto" Padding="24,20,24,24">
                
                <Grid ColumnDefinitions="*,Auto" Margin="0,0,0,15">
                    <VerticalStackLayout>
                        <Label Text="Editar Proyecto" FontSize="20" FontAttributes="Bold" TextColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}"/>
                        <Label Text="Modifica los detalles" FontSize="12" TextColor="{AppThemeBinding Light=#64748B, Dark=#94A3B8}"/>
                    </VerticalStackLayout>
                    <Border Grid.Column="1" WidthRequest="32" HeightRequest="32" CornerRadius="16" BackgroundColor="{AppThemeBinding Light=#F1F5F9, Dark=#334155}" StrokeThickness="0" VerticalOptions="Center">
                        <Image Source="ic_close.svg" WidthRequest="16" HeightRequest="16" VerticalOptions="Center" HorizontalOptions="Center"/>
                        <Border.GestureRecognizers><TapGestureRecognizer Tapped="OnCloseEditProjectSheetClicked"/></Border.GestureRecognizers>
                    </Border>
                </Grid>

                <ScrollView Grid.Row="1" VerticalScrollBarVisibility="Never" Margin="0,0,0,15" MaximumHeightRequest="400">
                    <VerticalStackLayout Spacing="15" x:Name="EditProjectSheetCard">
                        
                        <VerticalStackLayout Spacing="5">
                            <Label Text="NOMBRE DEL PROYECTO" FontSize="11" FontAttributes="Bold" TextColor="{AppThemeBinding Light=#64748B, Dark=#94A3B8}"/>
                            <Border BackgroundColor="{AppThemeBinding Light=#F8FAFC, Dark=#0F172A}" StrokeThickness="0" HeightRequest="50">
                                <Border.StrokeShape><RoundRectangle CornerRadius="12"/></Border.StrokeShape>
                                <Entry x:Name="EditNombreProyecto" Placeholder="Ej. Casa en el lago" PlaceholderColor="{AppThemeBinding Light=#94A3B8, Dark=#475569}" TextColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}" Margin="15,0"/>
                            </Border>
                        </VerticalStackLayout>

                        <VerticalStackLayout Spacing="5">
                            <Label Text="DESCRIPCIÓN" FontSize="11" FontAttributes="Bold" TextColor="{AppThemeBinding Light=#64748B, Dark=#94A3B8}"/>
                            <Border BackgroundColor="{AppThemeBinding Light=#F8FAFC, Dark=#0F172A}" StrokeThickness="0" HeightRequest="50">
                                <Border.StrokeShape><RoundRectangle CornerRadius="12"/></Border.StrokeShape>
                                <Entry x:Name="EditDescripcionProyecto" Placeholder="Detalles..." PlaceholderColor="{AppThemeBinding Light=#94A3B8, Dark=#475569}" TextColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}" Margin="15,0"/>
                            </Border>
                        </VerticalStackLayout>

                        <VerticalStackLayout Spacing="5">
                            <Label Text="UBICACIÓN" FontSize="11" FontAttributes="Bold" TextColor="{AppThemeBinding Light=#64748B, Dark=#94A3B8}"/>
                            <Border BackgroundColor="{AppThemeBinding Light=#F8FAFC, Dark=#0F172A}" StrokeThickness="0" HeightRequest="50">
                                <Border.StrokeShape><RoundRectangle CornerRadius="12"/></Border.StrokeShape>
                                <Entry x:Name="EditUbicacionProyecto" Placeholder="Ciudad, País" PlaceholderColor="{AppThemeBinding Light=#94A3B8, Dark=#475569}" TextColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}" Margin="15,0"/>
                            </Border>
                        </VerticalStackLayout>

                        <VerticalStackLayout Spacing="5">
                            <Label Text="PRESUPUESTO ($)" FontSize="11" FontAttributes="Bold" TextColor="{AppThemeBinding Light=#64748B, Dark=#94A3B8}"/>
                            <Border BackgroundColor="{AppThemeBinding Light=#F8FAFC, Dark=#0F172A}" StrokeThickness="0" HeightRequest="50">
                                <Border.StrokeShape><RoundRectangle CornerRadius="12"/></Border.StrokeShape>
                                <Entry x:Name="EditPresupuestoProyecto" Placeholder="0.00" Keyboard="Numeric" PlaceholderColor="{AppThemeBinding Light=#94A3B8, Dark=#475569}" TextColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}" Margin="15,0"/>
                            </Border>
                        </VerticalStackLayout>

                        <VerticalStackLayout Spacing="5">
                            <Label Text="ESTADO DEL PROYECTO" FontSize="11" FontAttributes="Bold" TextColor="{AppThemeBinding Light=#64748B, Dark=#94A3B8}"/>
                            <Border BackgroundColor="{AppThemeBinding Light=#F8FAFC, Dark=#0F172A}" StrokeThickness="0" HeightRequest="50">
                                <Border.StrokeShape><RoundRectangle CornerRadius="12"/></Border.StrokeShape>
                                <Picker x:Name="EditEstadoProyecto" Title="Selecciona el estado" TitleColor="{AppThemeBinding Light=#94A3B8, Dark=#475569}" TextColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}" Margin="15,0">
                                    <Picker.Items>
                                        <x:String>Borrador</x:String>
                                        <x:String>En Progreso</x:String>
                                        <x:String>Finalizado</x:String>
                                    </Picker.Items>
                                </Picker>
                            </Border>
                        </VerticalStackLayout>

                    </VerticalStackLayout>
                </ScrollView>

                <Button Grid.Row="2" Text="Guardar Cambios" BackgroundColor="{AppThemeBinding Light=#0F172A, Dark=#F8FAFC}" TextColor="{AppThemeBinding Light=#F8FAFC, Dark=#0F172A}" HeightRequest="50" CornerRadius="25" FontSize="15" FontAttributes="Bold" Clicked="OnSubmitEditarProyectoClicked"/>
                
            </Grid>
        </Border>
        
        """
    
    content = content[:start_idx] + new_modal + content[end_idx:]

    with open(file_path, "w", encoding="utf-8") as f:
        f.write(content)
    print("UI Replaced!")
else:
    print("Could not find blocks.")
