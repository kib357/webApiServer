using System.Collections.Generic;
using System.Net;
using System.Windows;
using BacNetApi;

namespace WPFBacNetApiSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MyViewModel _viewModel = new MyViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }
    }
}
