# UI/UX Redesign Strategy — AlNeda Pharmacy Platform

| Field | Value |
|-------|-------|
| **Project** | AlNeda (مخزن الندا) — Pharmaceutical Warehouse Management |
| **Platform** | WPF Desktop (.NET 9, MVVM, CommunityToolkit.Mvvm) |
| **Audit Date** | 2026-05-23 |
| **Target** | Enterprise-grade dark-theme RTL Arabic desktop experience |
| **Design Benchmark** | Linear, Notion, Stripe Dashboard, Microsoft Fluent Design |

---

## Current State Assessment

The existing UI is **functional but inconsistent**. It has strong bones (good MVVM separation, proper resource dictionary usage, working RTL) but suffers from:

1. **Inconsistent spacing system** — 7 different root margins across views
2. **No typography scale** — 22/24/28/30/36px all used as "page titles"
3. **Duplicate visual languages** — OffersDashboardWindow and dialogs use different colors than the main app
4. **Incomplete views** — PaymentsView, ReturnsView have hardcoded static data
5. **No responsive sidebar collapse** — CompactSidebar setting exists but is never wired
6. **Converter output is hardcoded hex** — converters return `#FF...` strings instead of brush resources

---

## Part 1: Design System Foundation

### 1.1 Color Palette

```xml
<!-- Current: Works well but needs refinement -->
<!-- Replace the flat tints with more premium, muted tones -->

<!-- PRIMARIES -->
<Color x:Key="PrimaryBase">#FF7C5CFC</Color>      <!-- Rich Violet — main accent -->
<Color x:Key="PrimaryHover">#FF6D4FE8</Color>
<Color x:Key="PrimaryPressed">#FF5E42D4</Color>
<Color x:Key="PrimarySubtle">#1A7C5CFC</Color>     <!-- 10% opacity for backgrounds -->

<!-- SECONDARY -->
<Color x:Key="SecondaryBase">#FF36C5F0</Color>      <!-- Cyan — secondary accent -->
<Color x:Key="SecondaryHover">#FF2DB4DC</Color>
<Color x:Key="SecondarySubtle">#1A36C5F0</Color>

<!-- SURFACE (Dark Theme) -->
<Color x:Key="SurfaceBase">#FF0D1117</Color>        <!-- Deepest background -->
<Color x:Key="SurfaceElevated">#FF161B22</Color>    <!-- Cards / elevated surfaces -->
<Color x:Key="SurfaceOverlay">#FF1C2128</Color>     <!-- Modals / popups -->
<Color x:Key="SurfaceBorder">#FF30363D</Color>      <!-- Subtle borders -->

<!-- TEXT -->
<Color x:Key="TextPrimary">#FFE6EDF3</Color>        <!-- High emphasis -->
<Color x:Key="TextSecondary">#FF8B949E</Color>      <!-- Medium emphasis -->
<Color x:Key="TextTertiary">#FF484F58</Color>        <!-- Low emphasis / placeholders -->

<!-- STATUS -->
<Color x:Key="StatusSuccess">#FF3FB950</Color>
<Color x:Key="StatusWarning">#FFD29922</Color>
<Color x:Key="StatusDanger">#FFF85149</Color>
<Color x:Key="StatusInfo">#FF58A6FF</Color>

<!-- CHARTS -->
<Color x:Key="ChartBlue">#FF58A6FF</Color>
<Color x:Key="ChartPurple">#FFBC8CFF</Color>
<Color x:Key="ChartCyan">#FF36C5F0</Color>
<Color x:Key="ChartOrange">#FFD29922</Color>
<Color x:Key="ChartRed">#FFF85149</Color>
<Color x:Key="ChartGreen">#FF3FB950</Color>
```

**Key changes from current:**
- Replace flat `PrimaryIndigo (#5C6BC0)` with a richer violet `#7C5CFC` — more modern, more premium
- Replace bright `PrimaryCyan (#00BCD4)` with muted secondary `#36C5F0`
- Simplify surface colors to 3 levels (base, elevated, overlay) instead of scattered background values
- Status colors: keep the current success/warning/danger but use the exact GitHub-dark palette for familiarity

### 1.2 Typography Scale

```xml
<!-- FontFamily: Segoe UI Variable (Windows 11) with Segoe UI fallback -->
<FontFamily x:Key="DefaultFontFamily">Segoe UI Variable Display, Segoe UI</FontFamily>
<FontFamily x:Key="MonospaceFontFamily">Cascadia Code, Consolas</FontFamily>

<!-- Complete Type Scale — use these everywhere, never hardcode font sizes -->
<sys:Double x:Key="FontSizeHero">40</sys:Double>       <!-- Dashboard hero numbers -->
<sys:Double x:Key="FontSizeH1">28</sys:Double>          <!-- Page titles -->
<sys:Double x:Key="FontSizeH2">20</sys:Double>          <!-- Section headers -->
<sys:Double x:Key="FontSizeH3">16</sys:Double>          <!-- Card titles -->
<sys:Double x:Key="FontSizeBody">14</sys:Double>         <!-- Body / DataGrid text -->
<sys:Double x:Key="FontSizeSmall">12</sys:Double>        <!-- Labels / captions -->
<sys:Double x:Key="FontSizeTiny">11</sys:Double>         <!-- Badges / timestamps -->
<sys:Double x:Key="FontSizeStat">32</sys:Double>         <!-- Metric values (KPI cards) -->
<sys:Double x:Key="FontSizeStatLabel">13</sys:Double>    <!-- Metric labels -->
```

**Why this fixes the current issue:**
- Currently page titles vary from 22 to 30px — this enforces ONE value (`28px`)
- Stat values vary from 24 to 36px — this enforces ONE value (`32px`)
- Body text varies between 12 and 13px — this enforces `14px` (more readable)
- Adds missing sizes: hero (40px), H2 (20px), H3 (16px), tiny (11px)

### 1.3 Spacing System

```xml
<!-- 8px grid — every spacing value is a multiple of 4 or 8 -->
<Thickness x:Key="SpaceNone">0</Thickness>
<Thickness x:Key="SpaceXxs">4</Thickness>        <!-- Rare, for dense layouts -->
<sys:Double x:Key="SpaceXs">8</sys:Double>        <!-- Tight spacing -->
<sys:Double x:Key="SpaceSm">12</sys:Double>       <!-- Between related items -->
<sys:Double x:Key="SpaceMd">16</sys:Double>       <!-- Standard gap -->
<sys:Double x:Key="SpaceLg">24</sys:Double>       <!-- Between sections -->
<sys:Double x:Key="SpaceXl">32</sys:Double>       <!-- Page margins -->
<sys:Double x:Key="Space2xl">48</sys:Double>      <!-- Large page sections -->

<!-- Convenience Thickness resources for common margins -->
<Thickness x:Key="MarginPage">24</Thickness>             <!-- Root content margin -->
<Thickness x:Key="MarginCard">20</Thickness>             <!-- Card inner padding -->
<Thickness x:Key="MarginSection">0,0,0,24</Thickness>   <!-- Bottom margin for sections -->
<Thickness x:Key="MarginFormGroup">0,0,0,16</Thickness> <!-- Form field groups -->
<Thickness x:Key="PaddingDataGridCell">12,0,12,0</Thickness>  <!-- Cell content padding -->
```

**Why this fixes the current issue:**
- Currently root margins vary from `6px` to `32px` across views — this enforces `24px` everywhere
- Currently spacer columns use 12/16/24/32px — this enforces `SpaceMd (16px)` as the standard gap
- Currently card padding varies from 18 to 24px — this enforces `MarginCard (20px)`
- All views reference `{StaticResource MarginPage}` instead of hardcoding

### 1.4 Shadows & Elevation

```xml
<!-- DropShadow definitions — 3 elevation levels -->
<!-- Level 1: Cards, sidebar (subtle) -->
<DropShadowEffect x:Key="Elevation1" 
    BlurRadius="12" ShadowDepth="0" Opacity="0.12" Color="#FF000000" Direction="270" />

<!-- Level 2: Dropdowns, modals (medium) -->
<DropShadowEffect x:Key="Elevation2" 
    BlurRadius="24" ShadowDepth="0" Opacity="0.16" Color="#FF000000" Direction="270" />

<!-- Level 3: Dialogs, notifications (pronounced) -->
<DropShadowEffect x:Key="Elevation3" 
    BlurRadius="48" ShadowDepth="0" Opacity="0.20" Color="#FF000000" Direction="270" />
```

---

## Part 2: Structural Redesign

### 2.1 MainWindow Shell

**Current problems:**
- Sidebar is fixed at `282px` with no collapse
- No responsive behavior
- Border padding `18,18,0,18` + content margin `18` creates uneven spacing
- Main content area has no top header bar with user/profile info

**Redesigned layout:**

```
┌─────────────────────────────────────────────────────┐
│ ┌──────────┐ ┌────────────────────────────────────┐ │
│ │          │ │ ┌───TOP BAR─────────────────────┐  │ │
│ │ Sidebar  │ │ │ 🔍 Search    🔔  👤 admin   │  │ │
│ │ Collapsed │ │ └────────────────────────────────┘  │ │
│ │ to 64px   │ │                                      │ │
│ │ on toggle │ │ ┌───PAGE CONTENT──────────────────┐ │ │
│ │           │ │ │ (ScrollViewer with MarginPage)   │ │ │
│ │ Icons     │ │ │                                   │ │ │
│ │ + labels  │ │ │                                   │ │ │
│ │           │ │ └────────────────────────────────────┘ │ │
│ │           │ │                                      │ │
│ │           │ │ ┌───FOOTER STATUS BAR─────────────┐ │ │
│ │           │ │ │ 📡 Connected  |  🕐 14:22:30  │ │ │
│ │           │ │ └──────────────────────────────────┘  │ │
│ └──────────┘ └───────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Implementation:**

```xml
<!-- MainWindow.xaml — redesigned root -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{Binding SidebarWidth, Mode=TwoWay}" MinWidth="64" MaxWidth="282" />
        <ColumnDefinition Width="Auto" />  <!-- Resize grip -->
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- Sidebar -->
    <Border Grid.Column="0" Background="{StaticResource SurfaceElevated}"
            Effect="{StaticResource Elevation1}">
        <Grid Margin="0,0,0,0">
            <Grid.RowDefinitions>
                <RowDefinition Height="64" />  <!-- Logo area -->
                <RowDefinition Height="*" />   <!-- Navigation -->
                <RowDefinition Height="48" />  <!-- Collapse button -->
            </Grid.RowDefinitions>
            
            <!-- Logo -->
            <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="20,0,20,0">
                <Border Width="32" Height="32" CornerRadius="8"
                        Background="{StaticResource PrimaryBase}" />
                <TextBlock Text="AlNeda" FontSize="18" FontWeight="SemiBold"
                           Margin="12,0,0,0" VerticalAlignment="Center"
                           Visibility="{Binding ShowLabels}" />
            </StackPanel>
            
            <!-- Navigation (scrollable) -->
            <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Hidden">
                <ItemsControl ItemsSource="{Binding NavItems}" />
            </ScrollViewer>
            
            <!-- Collapse toggle -->
            <Button Grid.Row="2" Style="{StaticResource TransparentButtonStyle}"
                    Command="{Binding ToggleSidebarCommand}">
                <TextBlock Text="&#xE76C;" FontFamily="Segoe MDL2 Assets" />  
                <!-- &#xE76C = GlobalNavButton -->
            </Button>
        </Grid>
    </Border>

    <!-- Resize grip (interactive column splitter) -->
    <GridSplitter Grid.Column="1" Width="4" 
                  ResizeBehavior="PreviousAndNext" 
                  Background="Transparent" />
    
    <!-- Main content -->
    <Grid Grid.Column="2">
        <Grid.RowDefinitions>
            <RowDefinition Height="56" />  <!-- Top bar -->
            <RowDefinition Height="*" />   <!-- Page content -->
            <RowDefinition Height="28" />  <!-- Footer status bar -->
        </Grid.RowDefinitions>
        
        <!-- Top Bar -->
        <Border Grid.Row="0" Background="{StaticResource SurfaceElevated}">
            <Grid Margin="{StaticResource MarginPage}">
                <!-- Breadcrumb / page title -->
                <TextBlock Text="{Binding CurrentPageTitle}" 
                           FontSize="{StaticResource FontSizeH2}" 
                           FontWeight="SemiBold"
                           VerticalAlignment="Center" />
                <!-- Right side: search, notifications, user -->
                <StackPanel HorizontalAlignment="Left" Orientation="Horizontal">
                    <Button Style="{StaticResource IconButtonStyle}" Content="..." />
                </StackPanel>
            </Grid>
        </Border>
        
        <!-- Page Content (injected via ContentControl) -->
        <ContentControl Grid.Row="1" Content="{Binding CurrentView}" />
        
        <!-- Footer Status Bar -->
        <Border Grid.Row="2" Background="{StaticResource SurfaceElevated}">
            <Grid Margin="{StaticResource MarginPage}">
                <TextBlock Text="{Binding StatusText}" 
                           FontSize="{StaticResource FontSizeSmall}"
                           Foreground="{StaticResource TextSecondary}" />
            </Grid>
        </Border>
    </Grid>
</Grid>
```

### 2.2 Sidebar Redesign

**Current issues:**
- No collapse animation (just visibility)
- No icon-only mode (despite `CompactSidebar` setting existing)
- Navigation buttons use string-typed `Tag` for routing
- No keyboard shortcut hints

**Redesigned Navigation Model:**

```csharp
public class NavItem : ObservableObject
{
    public string Id { get; set; }                     // "dashboard", "products", etc.
    public string Label { get; set; }                  // "لوحة التحكم"
    public string LabelEn { get; set; }                 // "Dashboard"
    public string IconGlyph { get; set; }               // "&#xE80F;"
    public string IconFilled { get; set; }              // Filled variant for active state
    public bool IsActive { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? BadgeText { get; set; }              // "3" for alert counts
    public string? Route { get; set; }                  // View type name
    public ICommand NavigateCommand { get; set; }
    public ObservableCollection<NavItem> Children { get; set; }  // Sub-items (collapsible)
}
```

**NavButtonStyle — Updated:**

```xml
<Style x:Key="NavButtonStyle" TargetType="RadioButton">
    <Setter Property="Height" Value="44" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="RadioButton">
                <Border Name="Root" CornerRadius="8" 
                        Margin="8,0,8,2"
                        Background="Transparent">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="40" />  <!-- Icon area -->
                            <ColumnDefinition Width="*" />   <!-- Label area -->
                            <ColumnDefinition Width="Auto" /> <!-- Badge -->
                        </Grid.ColumnDefinitions>
                        
                        <!-- Active indicator bar (left side for RTL = right visually) -->
                        <Rectangle Name="ActiveBar" 
                                   Grid.Column="1"
                                   HorizontalAlignment="Left"
                                   Width="3" Height="20" 
                                   Fill="{StaticResource PrimaryBase}"
                                   CornerRadius="2"
                                   Opacity="0" />
                        
                        <!-- Icon -->
                        <TextBlock Grid.Column="0" 
                                   Text="{TemplateBinding Tag}"
                                   FontFamily="Segoe MDL2 Assets"
                                   FontSize="18"
                                   Foreground="{StaticResource TextSecondary}"
                                   HorizontalAlignment="Center" />
                        
                        <!-- Label -->
                        <TextBlock Grid.Column="1" 
                                   Text="{TemplateBinding Content}"
                                   FontSize="14"
                                   Foreground="{StaticResource TextSecondary}"
                                   VerticalAlignment="Center"
                                   Visibility="{Binding DataContext.ShowLabels, 
                                        RelativeSource={RelativeSource AncestorType=Window}}" />
                        
                        <!-- Badge -->
                        <Border Grid.Column="2" 
                                Background="{StaticResource StatusDanger}"
                                CornerRadius="10"
                                Padding="6,2"
                                Margin="0,0,8,0"
                                MinWidth="20" Height="20">
                            <TextBlock Text="3" FontSize="11" 
                                       Foreground="White" 
                                       HorizontalAlignment="Center" />
                        </Border>
                    </Grid>
                </Border>
                
                <!-- Triggers for IsChecked, IsMouseOver -->
                <ControlTemplate.Triggers>
                    <Trigger Property="IsChecked" Value="True">
                        <Setter TargetName="ActiveBar" Property="Opacity" Value="1" />
                        <Setter TargetName="Root" Property="Background" Value="{StaticResource PrimarySubtle}" />
                        <Setter TargetName="..." Value="{StaticResource TextPrimary}" />
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Root" Property="Background" Value="{StaticResource SurfaceBorder}" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

---

## Part 3: Page-Level Redesigns

### 3.1 DashboardView — Hero Metrics + Charts

**Current issues:**
- KPI cards are flat with gradient backgrounds — need depth
- Chart occupies too much width without being visually distinct
- Alert section is a flat list — should be priority-sorted cards
- No "quick action" buttons for common tasks

**Redesigned layout:**

```
┌─────────────────────────────────────────────────────┐
│  مرحباً بعودتك، admin                                │
│  📅 الأحد، ٢٣ مايو ٢٠٢٦                             │
│                                                     │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐     │
│ │إجمالي│ │المبيعات│ │الطلبات│ │الديون│ │المنتجات│   │
│ │المبيعات│ │اليوم │ │اليوم  │ │  │  │منخفضة│   │
│ │1.2M  │ │12.5K │ │45    │ │ 28  │ │ 8   │     │
│ │+12%  │ │+5%   │ │+18%  │ │-2%  │ │⚠️   │     │
│ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘     │
│                                                     │
│ ┌────────────────────────┐ ┌────────────────────┐  │
│ │  📊 المبيعات (30 يوم)   │ │  🏆 أفضل المنتجات   │  │
│ │  [LINE CHART AREA]     │ │  1. براسيتامول 500   │  │
│ │                        │ │  2. فيتامين C      │  │
│ │  With gradient fill    │ │  3. مضاد حيوي X    │  │
│ │  under the line        │ │  4. شراب سعال      │  │
│ │                        │ │  5. مرهم Y         │  │
│ └────────────────────────┘ └────────────────────┘  │
│                                                     │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │
│ │ 🔴 منتهية × 3│ │ 🟡 قريبة × 8│ │ 🟢 منخفض × 5│ │
│ │  الباراسيتامول  │  فيتامين سي   │  مضاد حيوي   │ │
│ │  ينتهي غداً    │  ينتهي خلال  │  الكمية: 3    │ │
│ │                │  ١٥ يوماً    │               │ │
│ └──────────────┘ └──────────────┘ └──────────────┘ │
└─────────────────────────────────────────────────────┘
```

**Key improvements:**
- **KPI cards** get `Elevation1` shadow, subtle border (1px `SurfaceBorder`), and an icon + change indicator (↑↓ arrows with percentage)
- **Chart** uses glass-effect background with gradient under the line
- **Alert cards** use color-coded left borders (red/yellow/green) with priority icon
- **Greeting header** with date adds warmth

```xml
<!-- Dashboard KPI Card — redesigned -->
<Border Style="{StaticResource DashboardCardStyle}">
    <Border Background="{StaticResource SurfaceElevated}"
            CornerRadius="12"
            BorderBrush="{StaticResource SurfaceBorder}"
            BorderThickness="1"
            Effect="{StaticResource Elevation1}">
        <Grid Margin="20">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            
            <!-- Label -->
            <TextBlock Grid.Row="0" Grid.Column="0"
                       Text="إجمالي المبيعات"
                       FontSize="{StaticResource FontSizeStatLabel}"
                       Foreground="{StaticResource TextSecondary}" />
            
            <!-- Value -->
            <TextBlock Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
                       Text="1,234,567"
                       FontSize="{StaticResource FontSizeHero}"
                       FontWeight="Bold"
                       Foreground="{StaticResource TextPrimary}" />
            
            <!-- Change indicator -->
            <Border Grid.Row="2" Grid.Column="0"
                    Background="{StaticResource SuccessSubtle}"
                    CornerRadius="4" Padding="6,2">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="▲" FontSize="10" 
                               Foreground="{StaticResource StatusSuccess}" />
                    <TextBlock Text="12.5%" FontSize="12"
                               Foreground="{StaticResource StatusSuccess}"
                               Margin="4,0,0,0" />
                </StackPanel>
            </Border>
            
            <!-- Icon -->
            <Border Grid.Row="0" Grid.Column="1" Grid.RowSpan="3"
                    Width="40" Height="40" CornerRadius="10"
                    Background="{StaticResource PrimarySubtle}"
                    VerticalAlignment="Top">
                <TextBlock Text="&#xE9F9;" FontFamily="Segoe MDL2 Assets"
                           FontSize="18" Foreground="{StaticResource PrimaryBase}"
                           HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>
        </Grid>
    </Border>
</Border>
```

### 3.2 DataGrid — Modern Table Design

**Current issues:**
- Row height is cramped — no breathing room
- Action buttons use emoji characters instead of icons
- No row hover state with action reveal (on-hover pattern)
- No sticky header with shadow when scrolling

**Redesigned DataGrid column definition pattern:**

```xml
<DataGrid ItemsSource="{Binding Products}" 
          Style="{StaticResource ModernDataGrid}"
          RowHeight="52">  <!-- Taller rows for readability -->
    
    <DataGrid.Resources>
        <!-- On-hover action button visibility -->
        <Style TargetType="Border" x:Key="RowActions">
            <Setter Property="Opacity" Value="0" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsMouseOver, 
                    RelativeSource={RelativeSource AncestorType=DataGridRow}}" Value="True">
                    <Setter Property="Opacity" Value="1" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </DataGrid.Resources>
    
    <DataGrid.Columns>
        <!-- ID column — narrow, subtle -->
        <DataGridTextColumn Header="#" Width="48">
            <DataGridTextColumn.ElementStyle>
                <Style>
                    <Setter Property="TextBlock.Foreground" Value="{StaticResource TextTertiary}" />
                    <Setter Property="TextBlock.FontSize" Value="{StaticResource FontSizeSmall}" />
                </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
        
        <!-- Product Name — primary column, takes remaining width -->
        <DataGridTextColumn Header="اسم المنتج" Width="*" 
                            Binding="{Binding Name}">
            <DataGridTextColumn.ElementStyle>
                <Style>
                    <Setter Property="TextBlock.FontWeight" Value="Medium" />
                    <Setter Property="TextBlock.FontSize" Value="{StaticResource FontSizeBody}" />
                </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
        
        <!-- Quantity — right-aligned, monospace -->
        <DataGridTextColumn Header="الكمية" Width="80">
            <DataGridTextColumn.ElementStyle>
                <Style>
                    <Setter Property="TextBlock.FontFamily" Value="{StaticResource MonospaceFontFamily}" />
                    <Setter Property="TextBlock.HorizontalAlignment" Value="Right" />
                    <Setter Property="TextBlock.Foreground" 
                            Value="{Binding Quantity, Converter={StaticResource StockLevelToBrushConverter}}" />
                </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
        
        <!-- Price — right-aligned, currency format -->
        <DataGridTextColumn Header="السعر" Width="100"
                            Binding="{Binding UnitPrice, StringFormat={}{0:N2} ر.س}">
            <DataGridTextColumn.ElementStyle>
                <Style>
                    <Setter Property="TextBlock.HorizontalAlignment" Value="Right" />
                    <Setter Property="TextBlock.FontFamily" Value="{StaticResource MonospaceFontFamily}" />
                </Style>
            </DataGridTextColumn.ElementStyle>
        </DataGridTextColumn>
        
        <!-- Status — using colored badge -->
        <DataGridTemplateColumn Header="الحالة" Width="100">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <Border CornerRadius="4" Padding="8,3"
                            Background="{Binding Status, Converter={StaticResource StatusToBackgroundConverter}}">
                        <TextBlock Text="{Binding Status, Converter={StaticResource StatusToTextConverter}}"
                                   FontSize="{StaticResource FontSizeSmall}"
                                   Foreground="{Binding Status, Converter={StaticResource StatusToForegroundConverter}}"
                                   HorizontalAlignment="Center" />
                    </Border>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
        
        <!-- Actions column — revealed on row hover -->
        <DataGridTemplateColumn Width="120">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Opacity="0">
                        <StackPanel.Style>
                            <Style TargetType="StackPanel">
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding IsMouseOver, 
                                        RelativeSource={RelativeSource AncestorType=DataGridRow}}" Value="True">
                                        <Setter Property="Opacity" Value="1" />
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </StackPanel.Style>
                        <Button Style="{StaticResource RowActionButton}" 
                                Command="{Binding DataContext.EditCommand, 
                                    RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding Id}"
                                Content="&#xE70F;" ToolTip="تعديل" />
                        <Button Style="{StaticResource RowActionButton}" 
                                Command="{Binding DataContext.DeleteCommand, 
                                    RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding Id}"
                                Content="&#xE74D;" ToolTip="حذف" />
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

**RowActionButton style:**
```xml
<Style x:Key="RowActionButton" TargetType="Button">
    <Setter Property="Width" Value="32" />
    <Setter Property="Height" Value="32" />
    <Setter Property="Margin" Value="2" />
    <Setter Property="FontFamily" Value="Segoe MDL2 Assets" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{StaticResource TextSecondary}" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Name="Root" Background="{TemplateBinding Background}"
                        CornerRadius="{TemplateBinding CornerRadius}">
                    <TextBlock Text="{TemplateBinding Content}"
                               FontFamily="{TemplateBinding FontFamily}"
                               FontSize="{TemplateBinding FontSize}"
                               Foreground="{TemplateBinding Foreground}"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Root" Property="Background" 
                                Value="{StaticResource SurfaceBorder}" />
                        <Setter Property="Foreground" Value="{StaticResource TextPrimary}" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

### 3.3 Form Design — Clean, Modern Inputs

**Current issues:**
- Validation messages use inconsistent margins/radii
- Field labels sometimes use the `FieldLabel` style, sometimes hardcoded
- Save/Cancel buttons lack clear visual priority separation
- No inline validation (error icon + message next to field)

**Redesigned form pattern:**

```xml
<!-- Form field template — consistent, clean -->
<Grid Margin="0,0,0,16">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    
    <!-- Label -->
    <TextBlock Grid.Row="0" Text="اسم المنتج"
               FontSize="{StaticResource FontSizeSmall}"
               Foreground="{StaticResource TextSecondary}"
               Margin="0,0,0,6" />
    
    <!-- Input + icon container -->
    <Border Grid.Row="1" CornerRadius="8"
            Background="{StaticResource SurfaceOverlay}"
            BorderBrush="{Binding Path=(validation:Validation.HasErrors), 
                Converter={StaticResource HasErrorsToBorderConverter}}"
            BorderThickness="1"
            Height="44">
        <Grid>
            <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged, 
                       ValidatesOnDataErrors=True}"
                     Style="{StaticResource ModernTextBox}"
                     Background="Transparent"
                     BorderThickness="0"
                     FontSize="{StaticResource FontSizeBody}" />
            <!-- Error icon (shown when validation fails) -->
            <TextBlock Name="ErrorIcon"
                       Text="&#xE783;" FontFamily="Segoe MDL2 Assets"
                       FontSize="14" Foreground="{StaticResource StatusDanger}"
                       HorizontalAlignment="Right" VerticalAlignment="Center"
                       Margin="0,0,12,0" Opacity="0" />
        </Grid>
    </Border>
    
    <!-- Validation message -->
    <TextBlock Grid.Row="2" 
               Text="{Binding (validation:Validation.Errors).CurrentItem.ErrorContent}"
               Foreground="{StaticResource StatusDanger}"
               FontSize="{StaticResource FontSizeSmall}"
               Margin="0,4,0,0" Visibility="Collapsed" />
</Grid>
```

### 3.4 Dialog/Modal Redesign

**Current issues:**
- `AddEditUserDialog.xaml` uses hardcoded GitHub-dark colors (#161B22, #30363D)
- No animation on open/close
- Overlay background doesn't fade smoothly
- Window chrome differs from main app

**Redesigned dialog pattern:**

```xml
<!-- Modal overlay — always translucent, animated -->
<Border Name="Overlay" Background="#80000000" Opacity="0">
    <Border.Triggers>
        <EventTrigger RoutedEvent="Loaded">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                     From="0" To="1" Duration="0:0:0.15" />
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Border.Triggers>
    
    <!-- Modal card — centered, elevated, animated scale-in -->
    <Border Background="{StaticResource SurfaceOverlay}"
            CornerRadius="16"
            Width="480" MaxHeight="80vh"
            Effect="{StaticResource Elevation3}"
            RenderTransformOrigin="0.5,0.5">
        <Border.RenderTransform>
            <ScaleTransform ScaleX="0.95" ScaleY="0.95" />
        </Border.RenderTransform>
        <Border.Triggers>
            <EventTrigger RoutedEvent="Loaded">
                <BeginStoryboard>
                    <Storyboard>
                        <DoubleAnimation Storyboard.TargetProperty="RenderTransform.ScaleX"
                                         From="0.95" To="1" Duration="0:0:0.2" />
                        <DoubleAnimation Storyboard.TargetProperty="RenderTransform.ScaleY"
                                         From="0.95" To="1" Duration="0:0:0.2" />
                    </Storyboard>
                </BeginStoryboard>
            </EventTrigger>
        </Border.Triggers>
        
        <Grid Margin="24">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />  <!-- Title + close -->
                <RowDefinition Height="*" />     <!-- Content -->
                <RowDefinition Height="Auto" />  <!-- Actions -->
            </Grid.RowDefinitions>
            
            <!-- Title row -->
            <Grid Grid.Row="0" Margin="0,0,0,20">
                <TextBlock Text="إضافة مستخدم جديد"
                           FontSize="{StaticResource FontSizeH2}"
                           FontWeight="SemiBold" />
                <Button Style="{StaticResource CloseButtonStyle}"
                        HorizontalAlignment="Right" />
            </Grid>
            
            <!-- Scrollable content -->
            <ScrollViewer Grid.Row="1" MaxHeight="400">
                <StackPanel>
                    <!-- Form fields here -->
                </StackPanel>
            </ScrollViewer>
            
            <!-- Action buttons -->
            <StackPanel Grid.Row="2" Orientation="Horizontal"
                        HorizontalAlignment="Left" Margin="0,20,0,0">
                <Button Content="حفظ" Style="{StaticResource PrimaryButtonStyle}"
                        Width="120" />
                <Button Content="إلغاء" Style="{StaticResource SecondaryButtonStyle}"
                        Width="100" Margin="12,0,0,0" />
            </StackPanel>
        </Grid>
    </Border>
</Border>
```

---

## Part 4: Animation & Interaction Design

### 4.1 Page Transitions

Add a `ContentControl` with animated transition between views:

```csharp
public class AnimatedTransitionControl : ContentControl
{
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        if (oldContent != null)
        {
            // Fade out old, fade in new
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100));
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            fadeIn.BeginTime = TimeSpan.FromMilliseconds(100);
            
            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeOut, this);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);
            
            // Apply new content
            storyboard.Completed += (s, e) => {
                base.OnContentChanged(oldContent, newContent);
                BeginAnimation(OpacityProperty, fadeIn);
            };
            
            BeginAnimation(OpacityProperty, fadeOut);
        }
        else
        {
            base.OnContentChanged(oldContent, newContent);
        }
    }
}
```

### 4.2 Toast Notifications

Replace `MessageBox` calls with a non-blocking toast system:

```xml
<!-- Toast notification popup — slides in from top-right -->
<Popup Name="ToastPopup" Placement="Center" 
       AllowsTransparency="True" PopupAnimation="Slide">
    <Border CornerRadius="8" Padding="16,12"
            Background="{StaticResource SurfaceOverlay}"
            BorderBrush="{StaticResource SurfaceBorder}"
            BorderThickness="1"
            Effect="{StaticResource Elevation2}"
            MinWidth="320">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            
            <Border Grid.Column="0" Width="20" Height="20" CornerRadius="10"
                    Background="{Binding Type, Converter={StaticResource ToastTypeToColor}}">
                <TextBlock Text="✓" FontSize="12" Foreground="White"
                           HorizontalAlignment="Center" VerticalAlignment="Center" />
            </Border>
            
            <StackPanel Grid.Column="1" Margin="12,0,12,0">
                <TextBlock Text="{Binding Title}" FontWeight="SemiBold"
                           FontSize="14" />
                <TextBlock Text="{Binding Message}" FontSize="12"
                           Foreground="{StaticResource TextSecondary}"
                           Margin="0,2,0,0" />
            </StackPanel>
            
            <Button Grid.Column="2" Content="✕" 
                    Style="{StaticResource TransparentButtonStyle}"
                    Width="24" Height="24" />
        </Grid>
    </Border>
</Popup>
```

---

## Part 5: OffersDashboardWindow — Complete Redesign

**Current issues:**
- Uses different color palette than main app (`#141E293B`, `#CC0D1728`)
- Incomplete bindings (many controls have no data binding)
- Cluttered layout with overlapping concerns
- Sidebar collapse is the only responsive feature — but main window lacks it

**Redesigned approach:**
The Offers module should feel like a **standalone marketing platform** within the app:

```
┌──────────────────────────────────────────────────────────────────┐
│  [Marketing Offers]                                              │
│                                                                   │
│  ┌─────────────┐  ┌──────────────────────────────────────────┐  │
│  │ Campaigns    │  │  New Campaign                            │  │
│  │─────────────│  │                                          │  │
│  │ ● Active    │  │  ┌────────────────────┐ ┌──────────────┐│  │
│  │ ○ Drafts    │  │  │ 🏷️ Campaign Name   │ │ 📅 Schedule  ││  │
│  │ ○ Scheduled │  │  │ ┌──────────────┐  │ │ ┌──────────┐  ││  │
│  │ ○ Ended     │  │  │ │ Flash Sale   │  │ │ │15 May -  │  ││  │
│  │ ○ Archived  │  │  │ └──────────────┘  │ │ │30 May     │  ││  │
│  │             │  │  │                    │ │ └──────────┘  ││  │
│  │ ● All (12)  │  │  │ 🎯 Targeting       │ └──────────────┘│  │
│  │             │  │  │ ┌──────────────┐  │                  │  │
│  │             │  │  │ │ ● All        │  │  ┌──────────┐   │  │
│  │             │  │  │ │ ○ Specific   │  │  │ Products │   │  │
│  │             │  │  │ └──────────────┘  │  │ ┌────────┐│   │  │
│  │             │  │  │                    │  │ │Item 1  ││   │  │
│  │             │  │  │ 📊 Budget          │  │ │Item 2  ││   │  │
│  │             │  │  │ ┌──────────────┐  │  │ │+ Add   ││   │  │
│  │             │  │  │ │ $5,000       │  │  │ └────────┘│   │  │
│  │             │  │  │ └──────────────┘  │  └──────────┘   │  │
│  │             │  │  └────────────────────┘                 │  │
│  │             │  │                                          │  │
│  │             │  │  [🚀 Publish]  [💾 Save Draft]  [🗑️ Delete]│  │
│  └─────────────┘  └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Part 6: WPF UI Library Recommendations

### Recommended Libraries

| Library | Purpose | Why |
|---------|---------|-----|
| **ModernWpf** (MahApps.Metro successor) | Fluent Design theme, Window chrome, controls | Polished Windows 11-style chrome, native RTL |
| **MaterialDesignInXamlToolkit** | Material Design 3 theming | Pre-built cards, chips, toggles, sliders — high polish |
| **WpfHexEditor** | Not needed | — |
| **LiveChartsCore** ✅ Already used | Charts | Keep — already in project, works well |
| **Wpf.AnimatedGif** | Not needed | — |
| **System.Drawing.Common** | Not needed | — |

**Recommendation:** **Do NOT add a heavy third-party theme library.** The existing custom theme is well-structured and specific to the brand. Instead:

1. **Extract** the current DarkTheme.xaml into a modular system (split into `Colors.xaml`, `Typography.xaml`, `Styles.xaml`, `Controls.xaml`)
2. **Refine** the existing styles with the new design system above
3. **Add** only targeted NuGet packages for specific needs:
   - `LoadingIndicators.WPF` — polished loading spinners
   - `ToastNotifications.Messages` — non-blocking notification system
   - `FluentWPF` — acrylic/transparency effects (Windows 10/11 only)

### Icon Pack Strategy

| Source | Usage | Glyph Range |
|--------|-------|-------------|
| **Segoe MDL2 Assets** | System icons, navigation | ✅ Already used — keep |
| **Segoe Fluent Icons** (Windows 11) | Modern filled/regular variants | Better than MDL2 for UI |
| **Lucide** or **Phosphor** (icon font) | Premium open-source icons | Consistent weight, modern look |

---

## Part 7: Step-by-Step Implementation Roadmap

### Phase 1: Foundation (Week 1)

| Day | Task | Files |
|-----|------|-------|
| 1 | Extract DarkTheme.xaml into modular files | `Colors.xaml`, `Typography.xaml`, `Spacing.xaml`, `Brushes.xaml`, `Styles.xaml`, `Controls.xaml` |
| 2 | Define new color palette, typography scale, spacing system | All new resource files |
| 3 | Update all converters to return brush resources instead of hex strings | `Converters.cs` |
| 4 | Standardize root margins across all 20 views to `{StaticResource MarginPage}` (24px) | All views |
| 5 | Fix `PageBackgroundBrush` → `PageBgBrush` typo in dialogs | `AddEditCategoryDialog.xaml`, `CategoryProductsDialog.xaml` |

### Phase 2: Structural (Week 2)

| Day | Task | Files |
|-----|------|-------|
| 6-7 | Redesign MainWindow shell with top bar, footer, collapsible sidebar | `MainWindow.xaml`, `App.xaml.cs` |
| 8 | Wire compact sidebar toggle + animation | `MainViewModel.cs`, `NavButtonStyle` |
| 9 | Add page transition animations | `AnimatedTransitionControl.cs` |
| 10 | Remove hardcoded stat values in PaymentsView, ReturnsView — add proper bindings | `PaymentsView.xaml`, `ReturnsView.xaml` |

### Phase 3: Component Polish (Week 3)

| Day | Task | Files |
|-----|------|-------|
| 11 | Redesign DataGrid templates with taller rows, hover-reveal actions, sticky headers | `DarkTheme.xaml` ModernDataGrid section |
| 12 | Redesign form inputs with inline validation, error icons, consistent heights | `DarkTheme.xaml` ModernTextBox, all dialog views |
| 13 | Replace emoji action buttons with Segoe MDL2 Assets icons | All DataGrid action columns |
| 14 | Redesign DashboardView with new KPI card template, alert cards, greeting header | `DashboardView.xaml`, `DashboardViewModel.cs` |

### Phase 4: Dialogs & Offers (Week 4)

| Day | Task | Files |
|-----|------|-------|
| 15 | Redesign modal dialogs with fade-in + scale animation, consistent colors | `AddEditUserDialog.xaml`, all dialog views |
| 16 | Fix OffersDashboardWindow to use app theme colors (remove hardcoded #141E293B etc.) | `OffersDashboardWindow.xaml` |
| 17 | Add toast notification service, replace MessageBox calls | `ToastService.cs`, `Views/Notifications/` |
| 18 | Add skeleton loading states to all views (not just Dashboard) | All views |
| 19 | QA pass: verify no overlapping controls, consistent spacing, responsive resize | All views |
| 20 | Final polish: keyboard shortcuts, tab order, focus visuals, high-contrast support | All views |

---

## Part 8: Summary of Changes

### What to Remove

| File/Element | Why |
|-------------|-----|
| Hardcoded hex colors in `OffersDashboardWindow.xaml` | Should use app resources |
| Hardcoded hex colors in `AddEditUserDialog.xaml` | Should use app resources |
| Hardcoded stat values in `PaymentsView.xaml`, `ReturnsView.xaml` | Placeholder data — bind to ViewModel |
| Emoji action buttons (`📝`, `⏳`, `👁️`, `🖨️`, `🔍`) | Inconsistent across OS versions — use Segoe MDL2 Assets |
| Inconsistent root margins (6-32px range) | Standardize to 24px |
| `PageBackgroundBrush` typo references | Fix to `PageBgBrush` |
| Unused `CompactSidebar` setting in SettingsViewModel | Wire it to actual sidebar collapse |

### What to Add

| Element | Priority | Effort |
|---------|----------|--------|
| Top bar with breadcrumb + user menu | HIGH | 1 day |
| Collapsible sidebar with animation | HIGH | 1 day |
| Toast notification system | MEDIUM | 2 days |
| Page transition animations | MEDIUM | 1 day |
| Skeleton loading for all views | MEDIUM | 2 days |
| Inline form validation with error icons | HIGH | 1 day |
| Row hover-reveal action buttons in DataGrids | MEDIUM | 1 day |
| Responsive layout with min-width breakpoints | MEDIUM | 2 days |
| Keyboard shortcut hints | LOW | 1 day |
| High-contrast theme support | LOW | 1 day |

### What to Standardize

| Element | Standard Value |
|---------|---------------|
| Root content margin | `24px` (all views) |
| Card padding | `20px` |
| Standard grid gap | `16px` |
| Page title font size | `28px` |
| Stat value font size | `32px` |
| Body font size | `14px` |
| DataGrid row height | `52px` |
| Input height | `44px` |
| Button heights (primary) | `44px` |
| Button heights (small/icon) | `32px` |
| Border radius (cards) | `12px` |
| Border radius (buttons) | `8px` |
| Border radius (inputs) | `8px` |
| Border radius (badges) | `6px` |

---

## Final Verdict

The current AlNeda UI is **functional but visually inconsistent** — like a product built by engineers who care about architecture but had limited design bandwidth. It has the right structural patterns (MVVM, resource dictionaries, style templates) but lacks design system rigor.

The redesign strategy above is **evolutionary, not revolutionary** — it preserves all existing XAML structure, MVVM bindings, and converter logic while introducing:

1. A **predictable spacing system** (8px grid)
2. A **consistent typography scale** (5 sizes, never hardcoded)
3. A **refined color palette** (richer primaries, muted accents)
4. **Interaction patterns** (hover-reveal actions, page transitions, toast notifications)
5. **Responsive behavior** (collapsible sidebar, min-width handling)

**Estimated implementation effort: 20 developer-days** (4 weeks for one developer, or 2 weeks with a designer). The result will be a desktop application that feels premium, consistent, and enterprise-grade — comparable to modern SaaS dashboards like Linear or Stripe.
