   M S B A M L   `   `   `  ����M  IStockFilterToolsV1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:StockFilterToolsV1StockFilterToolsV1  5   	   M+clr-namespace:StockFilterToolsV1.ViewModelsStockFilterToolsV1.ViewModels  68   C&clr-namespace:StockFilterToolsV1.UtilsStockFilterToolsV1.Utils  6o   #    StockFilterToolsV1.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   2local clr-namespace:StockFilterToolsV1     5   	   :vm+clr-namespace:StockFilterToolsV1.ViewModels     68   9local1&clr-namespace:StockFilterToolsV1.Utils     6o     � Title$  
MainWindow��5   	   $	��450��6   $	��800��6)    � WindowState$ 	Maximized=�65    � DataContext 5	      1  +StockFilterToolsV1.ViewModels.MainViewModel  5
   
    � 	Resources 5      8   2StockFilterToolsV1.Utils.BoolToVisibilityConverter   BoolToVisibilityConverter��%   &	          5   
   6+   5      .��5      �  � ColumnDefinitions 5   
   �� 5      $	��150 �6   �� 5      $��* �6   .+�5   
   �� $��0��6   $��10q�6%   .+�5      �� $��Mã chứng khoán:��6   $��0,0,0,5q�6/   �� 5      -   5        $��StockCodeTextBox��5      $	��130��6n   $��0,0,0,10q�6z   ��6.   �� *	StockCode+ �� UpdateSourceTrigger$ PropertyChanged=�6z   �� 5      $��Tìm kiếm��6   $	��130��5   4   ��5   (   �� *FetchCommand+��5      �� *	StockCode+64   �� 5      $��Cập nhật CSDL��6   $	��130��6Q   $��0,10,0,0q�6]   ��6-   �� *SyncDataCommand+6]   �� 5      $��Lọc theo tiêu chí��6   $	��130��6W   $��0,10,0,0q�6c   ��61   �� *FilterDataCommand+6c   5      � 5"   
   $��1��6   $��10q�6   .+�5#      �  � RowDefinitions 5$      �� 5%      $
��Auto �6$   �� 5'      $��* �6$   .+�5,      �� $��0��6   ���#F9FAFB �� Padding$ 12q�5-   &    �� CornerRadius$ 6��63   $��0 0 0 10q�6D   ���#D1D5DB$��1q�5.   '   ._�5/      �  50      �� 51      $��* �6/   �� 52      $��* �6/   .+�56      �� 	 �� Orientation$	 
Horizontal=�6%   $��0��6>   .+�57      �� $��Mã cổ phiếu: ��6(   $��16(�6=   $
��Bold"�6L   ���#1118276^   �� 58      $��16(�6F   $
��Bold"�6U   ���#374151��6(   �� *ComStockCode+6g   59      �� 5<      $	 
Horizontal=�6%   $��1��6>   .+�5=      �� $��Tên công ty: ��6(   $��16(�6=   $
��Bold"�6L   ���#1118276^   �� 5>      $��16(�6E   $
��Bold"�6T   ���#374151��6(   �� *CompanyName+6f   5?      5@      5A      &   System.Windows.Controls.DataGrid  5D      -   5        $��StockDataGrid��5E      $��1��5D      
   AutoGenerateColumns$
 False��5G         HeadersVisibility$ Column=�5I         CanUserAddRows$ False��5J         CanUserResizeColumns$
 True��5K         CanUserResizeRows$ False��5L         CanUserSortColumns$ False��5M      $��0q�5N      $
��Auto=�5O      $
��Auto=�5P         
IsReadOnly$
 True��5Q      ��5F      �� *StockDataRows+y�5H      �� *IsGridVisible+ �� 	Converter# [     ColumnHeaderStyle 5S      �� 5T       �� 
TargetType=  7System.Windows.Controls.Primitives.DataGridColumnHeader"  6    .�5U      ��  �� Property � �� �� Value$ 16(�69   �� 5V       � ��$
 Bold"�6;   �� 5W       � ��
 �����6;   �� 5X       � ��
 �OO/�6;   �� 5Y       � ��$ Center=�6K   �� 5Z       � ��$ Center=�6I   �� 5[       � ��$ 8q�68   5\         RowStyle 5_      �� 5`      )  #System.Windows.Controls.DataGridRow"  6     �� Triggers 5a      �� 5b   "    �� Binding 6.   �� *IndicatorName+ �� Value  6P   .a�5c   &   ��  � ��
 �����6C   �� 5d   &    � ��$ Center=�6Q   �� 5e   &    � ��$ 0q�6H   5f   #   5h      5j      � 5m      $��2��6   ���	#80000000$��Stretch=�5p      $��Stretch=�5q       K� ZIndex$ 10��5r      y�5o      �� *	IsLoading+# [  .+�5s      �� $��Center=�6!   $��Center=�6<   .+�5t      �  � IsIndeterminate$
 True��6&   $	��200��6=   $��20��6I   �� 5u      $ ��Đang tải dữ liệu...��6$   
�������$��16(�6R   $��0,10,0,0q�6`   $��Center=�6r   5v      5w      5x      5y      5z      5{      