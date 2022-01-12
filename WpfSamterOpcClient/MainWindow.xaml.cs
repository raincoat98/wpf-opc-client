﻿using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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
        string strAppName = "SamterOpcClient";
        public string KEPSERVER_PATH = "127.0.0.1";

        private static string DATA_PATH = AppDomain.CurrentDomain.BaseDirectory + @"\Data";
        private static string DATAFILE_PATH = DATA_PATH + "\\Data.txt";

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {


            Debug.WriteLine("start");

            //설정파일 생성
            createInfoFile();
            InitItemValue();

            SetNotification();
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

        private void ChkAutoConnect_Checked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(ChkAutoConnect.IsEnabled);

            if (IsAutoConnect() == false)
            {
                System.Windows.MessageBox.Show("프로그램을 자동으로 연결합니다.");

                using (RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\SamterOpcClient", true))
                {
                    try
                    {
                        //레지스트리 등록...
                        rk.SetValue("autoConnect", true);

                        //레지스트리 닫기...
                        rk.Close();


                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("오류: " + ex.Message.ToString());
                    }
                }
            }
            Task.Run(() => opcClient.Opcua_start($"opc.tcp://{KEPSERVER_PATH}:49320"));
        }

        private void ChkAutoConnect_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsAutoConnect() == true)
            {
                Registry.CurrentUser.DeleteSubKey(@"SOFTWARE\SamterOpcClient");
            }
            opcClient.Disconnect();
        }

        public void InitItemValue()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                TbServerURLValue.Text = KEPSERVER_PATH;

                BtConnectValue.IsEnabled = true;
                BtDisConnectValue.IsEnabled = false;
                LbConnectStatusValue.Content = "Not Connect";

                TbStatusValue.Text = "STOP";
                TbStatusValue.Foreground = Brushes.Red;

                TbSpeedValue.Text = "0";
                TbOrderIdValue.Text = "none....";

                TbQuantityValue.Text = "0";
                TbOrderQuantityValue.Text = "0";


                //자동 연결 확인
                if (IsAutoConnect())
                {
                    ChkAutoConnect.IsChecked = true;
                }
                else
                {
                    ChkAutoConnect.IsChecked = false;
                }
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
                if (itemId == opcClient.jobOrder)
                {
                    TbOrderIdValue.Text = value;
                }

                //articleCode
                if (itemId == opcClient.articleCode)
                {
                    TbArticleCodeValue.Text = value;
                }

                //count
                if (itemId == opcClient.quantity)
                {
                    TbQuantityValue.Text = value;

                    string orderQt = opcClient.ReadItemValue(opcClient.orderQuantity).ToString();

                    if ((Int32.Parse(value) > 0) && Int32.Parse(value) >= Int32.Parse(orderQt))
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

                //orderCount
                if (itemId == opcClient.orderQuantity)
                {
                    TbOrderQuantityValue.Text = value;
                }

            }));
        }
        public void createInfoFile()
        {
            try
            {
                //프로그램 실행시 Data 폴더 확인 및 없을경우 Data 폴더 생성
                DirectoryInfo di = new DirectoryInfo(DATA_PATH);
                if (!di.Exists) { di.Create(); }

                FileInfo file = new FileInfo(DATAFILE_PATH);
                if (!file.Exists)  //해당 파일이 없으면 생성하고 파일 닫기
                {

                    // 단순히 해당 파일에 내용을 저장하고자 할 때 (기존 내용 초기화 O)
                    FileStream fs = file.OpenWrite();
                    TextWriter tw = new StreamWriter(fs);
                    JObject kepwareSpec = new JObject(new JProperty("ip", $"{KEPSERVER_PATH}"));
                    tw.Write(kepwareSpec);
                    tw.Close();
                    fs.Close();
                    setServerURL(KEPSERVER_PATH);
                }
                else
                {
                    string textValue = File.ReadAllText(DATAFILE_PATH);
                    JObject jObject = JObject.Parse(textValue.Trim());
                    setServerURL((string)jObject["ip"]);

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());

            }
        }

        public void setServerURL(string value)
        {
            KEPSERVER_PATH = value;
        }
        
        //시작 프로그램에 등록되어 있는지 확인.
        public bool IsWindowStartUp()
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                try
                {
                    if (rk.GetValue(strAppName) != null)
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("오류: " + ex.Message.ToString());
                }
            }
            return false;
        }

        // autoConnect 버튼이 클릭 되어 있는지 확인.
        public bool IsAutoConnect()
        {
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\SamterOpcClient", true))
            {
                try
                {
                    if (rk != null)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("오류: " + ex.Message.ToString());
                }
            }
            return false;
        }

        private void SetNotification()
        {
            // 트레이 아이콘 생성
            NotifyIcon ni = new NotifyIcon();
            ni.Icon = Properties.Resources.myicon;
            ni.Visible = true;
            ni.Text = "SamterOpcClient";

            ni.DoubleClick += delegate (object sender, EventArgs eventArgs)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            ni.ContextMenu = SetContextMenu(ni);
        }

        private ContextMenu SetContextMenu(NotifyIcon ni)
        {
            // ContextMenu 생성합니다.
            ContextMenu menu = new ContextMenu();
            MenuItem item1 = new MenuItem();
            item1.Text = "windows startUp";

            //시작 프로그램 등록 확인
            if (IsWindowStartUp())
            {
                item1.Checked = true;
            } else
            {
                item1.Checked = false;
            }

            item1.Click += delegate (object click, EventArgs eventArgs)
            {
                if (item1.Checked == false)
                {
                    item1.Checked = true;
                    using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        try
                        {
                            Debug.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().Location);
                            //레지스트리 등록
                            if (rk.GetValue(strAppName) == null)
                            {
                                rk.SetValue(strAppName, System.Reflection.Assembly.GetExecutingAssembly().Location.ToString());
                            }
                            rk.Close();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show("오류: " + ex.Message.ToString());
                        }
                    }
                }
                else
                {
                    item1.Checked = false;

                    using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        try
                        {
                            //레지스트리 삭제
                            if (rk.GetValue(strAppName) != null)
                            {
                                rk.DeleteValue(strAppName, false);
                            }
                            rk.Close();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show("오류: " + ex.Message.ToString());
                        }
                    }
                }
            };
            menu.MenuItems.Add(item1);

           
            MenuItem item2 = new MenuItem();
            item2.Text = "Exit";

            item2.Click += delegate (object click, EventArgs eventArgs)
            {
                // 프로그램을 강제로 종료하는 부분입니다.
                System.Windows.Application.Current.Shutdown();

                // 프로그램 종료 후 NotifyIcoy 리소스를 해제합니다.
                // 해제하지 않을 경우 프로그램이 완전히 종료되지 않는 경우도 발생합니다.
                ni.Dispose();
            };
            menu.MenuItems.Add(item2);

            return menu;
        }
    }
}
