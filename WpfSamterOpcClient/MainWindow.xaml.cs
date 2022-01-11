using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;


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
            this.FontFamily = new FontFamily("Consolas");
        }
        OpcClient opcClient = new OpcClient();
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("start");
            InitItemValue();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string serverUrl = TbServerURLValue.Text;
            Task.Run(() => opcClient.Opcua_start($"opc.tcp://{serverUrl}:49320"));
        }
        private void btnDisConnect_Click(object sender, RoutedEventArgs e)
        {
            opcClient.Disconnect();
        }


        private void BtOorderComplate_Click(object sender, RoutedEventArgs e)
        {
            opcClient.WriteItemValue(opcClient.quantity, 0);
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

        public void SetConnectItemValue()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                BtConnectValue.IsEnabled = false;
                BtDisConnectValue.IsEnabled = true;
                LbConnectStatusValue.Content = "Connect";

            }));
        }

        public void SetChangeItemValue(string itemId, string value)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                //run
                if (itemId == opcClient.run)
                {
                    if (value.Equals("True"))
                    {
                        TbStatusValue.Text = "START";
                        TbStatusValue.Foreground = Brushes.Green;
                    }
                    else
                    {
                        TbStatusValue.Text = "STOP";
                        TbStatusValue.Foreground = Brushes.Red;
                    }
                }

                //speed
                if (itemId == opcClient.speed)
                {
                    TbSpeedValue.Text = value;
                }

                //orderId
                if (itemId == opcClient.orderId)
                {
                    TbOrderIdValue.Text = value;
                }

                //count
                if (itemId == opcClient.quantity)
                {
                    TbQuantityValue.Text = value;

                    string orderQt = opcClient.ReadItemValue(opcClient.orderQuantity).ToString();

                    if (Int32.Parse(value) >= 0)
                    {
                        if (Int32.Parse(value) >= Int32.Parse(orderQt))
                        {
                            opcClient.WriteItemValue(opcClient.orderComplate, true);
                            // 버튼 활성화
                            BtOorderComplate.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            BtOorderComplate.Visibility = Visibility.Hidden;
                        }
                    }
                }

                //orderCount
                if (itemId == opcClient.orderQuantity)
                {
                    TbOrderQuantityValue.Text = value;
                }

            }));
        }
    }
}
