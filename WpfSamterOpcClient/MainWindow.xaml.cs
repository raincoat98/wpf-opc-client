﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;


namespace WpfSamterOpcClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static WpfSamterOpcClient.MainWindow main;

        public MainWindow()
        {
            InitializeComponent();
            main = this;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            OpcClient opcClient = new OpcClient();
            Debug.WriteLine("start");
            InitItemValue();
            Task.Run(() => opcClient.Opcua_start("opc.tcp://192.168.0.211:49320"));
        }

       public void InitItemValue()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                TbServerURLValue.Text = "127.0.0.1";

                BtConnectValue.IsEnabled = true;
                BtDisConnectValue.IsEnabled = false;
                LbConnectStatusValue.Content = "Not Connect";

                TbStatusValue.Text = "STOP";
                TbStatusValue.Foreground = Brushes.Red;

                TbSpeedValue.Text = "0";
                TbOrderIdValue.Text = "none....";

                TbQuantityValue.Text = "0";
                TbOrderQuantityValue.Text = "0";

            }));
        }
    }
}
