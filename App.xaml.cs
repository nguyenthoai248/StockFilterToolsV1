using SQLitePCL;
using System.Windows;

namespace StockFilterToolsV1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Batteries_V2.Init();
        }
    }

}
