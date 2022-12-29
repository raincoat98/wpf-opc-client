using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        internal static MainWindow main;

        #region Construction
        public MainWindow()
        {
            InitializeComponent();
            main = this;
            this.FontFamily = new FontFamily("Consolas");
        }
        #endregion

        #region Fields
        OpcClient opcClient = new OpcClient();
        string strAppName = "SamterOpcClient";
        public string KEPSERVER_PATH = "127.0.0.1";

        private static string DATA_PATH = AppDomain.CurrentDomain.BaseDirectory + @"\Data";
        private static string DATAFILE_PATH = DATA_PATH + "\\Data.txt";
        private static string LOGFILE_PATH = DATA_PATH + "\\Log.txt";
        #endregion

        #region 프로그램 실행 시 로드 함수
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("start");

            CreateInfoFile(); //설정파일 생성
            InitItemValue();
            SetNotification();

            Task.Run(async () => await opcClient.Opcua_start($"opc.tcp://{KEPSERVER_PATH}:49320"));
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
            bool isWindowStartUp = Properties.Settings.Default.windowStartUp;
            item1.Checked = isWindowStartUp;

            item1.Click += delegate (object click, EventArgs eventArgs)
            {
                if (item1.Checked == true)
                {
                    item1.Checked = false;
                    Properties.Settings.Default.windowStartUp = false;
                    Properties.Settings.Default.Save();
                    RemoveWindowsStartUpRegistry();
                }
                else if (item1.Checked == false)
                {
                    item1.Checked = true;
                    Properties.Settings.Default.windowStartUp = true;
                    Properties.Settings.Default.Save();
                    CreateWindowsStartUpRegistry();
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

        public bool isNullProductStartDttm()
        {
            return String.IsNullOrEmpty(GetErpJobValue("START_DTTM"));
        }

        public bool isProductEndDttm()
        {
            return String.IsNullOrEmpty(GetErpJobValue("END_DTTM"));
        }

        public bool isNullProductProcessingTime()
        {
            return String.IsNullOrEmpty(GetErpJobValue("PROCESSING_TIME"));
        }


        public void InitItemValue()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                BtReConnect.IsEnabled = true;
                LbConnectStatusValue.Content = "DisConnected";
                LbConnectStatusValue.Foreground = Brushes.Red;

                TbStatusValue.Text = "READY";
                TbStatusValue.Foreground = Brushes.Red;

                TbSpeedValue.Text = "0";
                TbOrderIdValue.Text = "";
                TbArticleCodeValue.Text = "";

                TbQuantityValue.Text = "0";
                TbOrderQuantityValue.Text = "0";

                startDtValue.Text = "";
                endDtValue.Text = "";
                processingTimeValue.Text = "";


                //db에 값들이 있을경우 호출
                if (!String.IsNullOrEmpty(GetErpJobValue("JOB_ORDER")))
                {
                    TbOrderIdValue.Text = GetErpJobValue("JOB_ORDER");
                }

                if (!String.IsNullOrEmpty(GetErpJobValue("ARTICLE_CODE")))
                {
                    TbArticleCodeValue.Text = GetErpJobValue("ARTICLE_CODE");
                }

                if (Int32.Parse(GetProdutionQuantity()) > 0)
                {
                    TbQuantityValue.Text = GetProdutionQuantity();
                }

                if (!String.IsNullOrEmpty(GetErpJobValue("ORDER_QUANTITY")))
                {
                    TbOrderQuantityValue.Text = GetErpJobValue("ORDER_QUANTITY");
                }


                if (!isNullProductStartDttm())
                {
                    startDtValue.Text = GetErpJobValue("START_DTTM");
                    endDtValue.Text = GetErpJobValue("END_DTTM");
                    processingTimeValue.Text = GetErpJobValue("PROCESSING_TIME");

                }


                bool autoStopStatus = Properties.Settings.Default.autoStop;
                BtAutoStop.IsChecked = autoStopStatus;
                if (autoStopStatus == true)
                {
                    TbAutoStopStatus.Text = "Enabled";
                    TbAutoStopStatus.Foreground = Brushes.Green;
                }
                else
                {
                    TbAutoStopStatus.Text = "Disabled";
                    TbAutoStopStatus.Foreground = Brushes.Red;
                }
            }));
        }
        #endregion

        #region 버튼 클릭 함수
        private void btnReConnect_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () => await opcClient.Opcua_start($"opc.tcp://{KEPSERVER_PATH}:49320"));

        }

        private void BtAutoStop_Checked(object sender, RoutedEventArgs e)
        {
            TbAutoStopStatus.Text = "Enabled";
            TbAutoStopStatus.Foreground = Brushes.Green;
            Properties.Settings.Default.autoStop = true;
            Properties.Settings.Default.Save();
        }

        private void BtAutoStop_UnChecked(object sender, RoutedEventArgs e)
        {
            TbAutoStopStatus.Text = "Disabled";
            TbAutoStopStatus.Foreground = Brushes.Red;
            Properties.Settings.Default.autoStop = false;
            Properties.Settings.Default.Save();
        }

        private void BtSuspendJob_Click(object sender, RoutedEventArgs e)
        {
            opcClient.WriteItemValue(opcClient.finalQuantity, Int32.Parse(GetProdutionQuantity()));
        }

        #endregion

        #region opc 관련 함수
        public void SetConnectItemValue()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                BtReConnect.IsEnabled = false;
                LbConnectStatusValue.Content = "Connected";
                LbConnectStatusValue.Foreground = Brushes.LightGreen;

            }));
        }

        private void SetProcessingTime()
        {
            string startDt = GetErpJobValue("START_DTTM");
            string endDt = GetErpJobValue("END_DTTM");

            TimeSpan timeDiff = DateTime.Parse(endDt) - DateTime.Parse(startDt);

            String processingTime = timeDiff.ToString();

            processingTimeValue.Text = processingTime;
            SetErpJobValue("PROCESSING_TIME", processingTime);
            opcClient.WriteItemValue(opcClient.processingTime, $"{processingTime}");
        }



        public void SetChangeItemValue(string itemId, string value)
        {
            try
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    //TODO: switch 문으로 변경

                    //kepware tag 문자열 default value 제외
                    if (value.StartsWith("String")) return;
                    /* if (value.StartsWith("0")) return;*/
                    //run

                    if (itemId == opcClient.run)
                    {
                        if (value.Equals("True"))
                        {
                            TbStatusValue.Text = "RUN";
                            TbStatusValue.Foreground = Brushes.LightGreen;
                        }
                        else
                        {
                            TbStatusValue.Text = "READY";
                            TbStatusValue.Foreground = Brushes.Red;
                        }
                        return;
                    }

                    if (itemId == opcClient.stop)
                    {
                        if (value.Equals("True"))
                        {
                            TbStatusValue.Text = "STOP";
                            TbStatusValue.Foreground = Brushes.Red;

                            if (BtAutoStop.IsChecked == false)
                            {
                                int prodQt = Int32.Parse(GetProdutionQuantity());
                                int orderQt = Int32.Parse(GetErpJobValue("ORDER_QUANTITY"));
                                string dateNowUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                                if (prodQt <= 1 && prodQt > orderQt)
                                {
                                    if (!isNullProductStartDttm())
                                    {
                                        string startDt = GetErpJobValue("START_DTTM");
                                        startDtValue.Text = startDt;

                                        opcClient.WriteItemValue(opcClient.endDTTM, Convert.ToDateTime(dateNowUTC));
                                        endDtValue.Text = dateNowUTC;
                                        SetProcessingTime();
                                    }
                                }
                            }
                        }
                        return;
                    }

                    if (itemId == opcClient.error)
                    {
                        if (value.Equals("True"))
                        {
                            TbStatusValue.Text = "ERROR";
                            TbStatusValue.Foreground = Brushes.Red;
                        }
                        return;
                    }

                    //speed
                    if (itemId == opcClient.speed)
                    {
                        TbSpeedValue.Text = value;
                        return;
                    }

                    //orderId
                    if (itemId == opcClient.jobOrder)
                    {
                        //Opc 기본 값 string으로 초기화 되는 문제로 db에서 데이터를 가져와야함 / 220706 해결 220723


                        // db에 잡 아이디와 비교 
                        if (!value.Equals(GetErpJobValue("JOB_ORDER")))
                        {
                            //order 값이 변경될 때 값을 초기화
                            /*                        
                             *initOrderValue
                             *initOrderDate
                            */
                            TbArticleCodeValue.Text = "";
                            TbOrderQuantityValue.Text = "0";
                            TbQuantityValue.Text = "0";
                            startDtValue.Text = "";
                            endDtValue.Text = "";
                            processingTimeValue.Text = "";
                            opcClient.WriteItemValue(opcClient.finalQuantity, 0);

                            SetErpJobValue("JOB_ORDER", value);
                            TbOrderIdValue.Text = value;
                        }
                        else
                        {
                            TbOrderIdValue.Text = value;
                        }



                        return;
                    }

                    //품목
                    if (itemId == opcClient.articleCode)
                    {
                        if (!value.Equals(GetErpJobValue("ARTICLE_CODE")))
                        {
                            SetErpJobValue("ARTICLE_CODE", value);
                            TbArticleCodeValue.Text = value;
                        }
                        return;
                    }
                    //장비코드
                    if (itemId == opcClient.equipCode)
                    {
                        if (!value.Equals(GetErpJobValue("EQUIP_CODE")))
                        {
                            SetErpJobValue("EQUIP_CODE", value);
                        }
                        return;
                    }

                    if (itemId == opcClient.prodQuantity)
                    {
                        /*string orderQt = opcClient.ReadItemValue(opcClient.orderQuantity).ToString();*/
                        int orderQt = Int32.Parse(GetErpJobValue("ORDER_QUANTITY"));

                        string dateNowUTC = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                        if (Int32.Parse(value) > 0)
                        {
                            TbQuantityValue.Text = value;
                        }

                        int prodQt = Int32.Parse(value);
                        IncreaseProdutionProcedure(prodQt);


                        //작업 시작시간
                        if (prodQt == 1)
                        {
                            startDtValue.Text = dateNowUTC;
                            SetErpJobValue("START_DTTM", dateNowUTC);
                            opcClient.WriteItemValue(opcClient.startDTTM, Convert.ToDateTime(dateNowUTC));
                        }

                        if (prodQt > 0)
                        {

                            Debug.WriteLine(orderQt);
                            Debug.WriteLine(prodQt);
                            if (prodQt == orderQt)
                            {
                                opcClient.WriteItemValue(opcClient.finalQuantity, prodQt);


                                //자동 종료 버튼 활성화 시 
                                if (BtAutoStop.IsChecked == true)
                                {

                                    // 목표 수량 달성시 종료 시간 업데이트
                                    // 한번 업데이트후 멈춤 
                                    if (isNullProductProcessingTime())
                                    {
                                        endDtValue.Text = dateNowUTC;
                                        SetErpJobValue("END_DTTM", dateNowUTC);
                                        opcClient.WriteItemValue(opcClient.endDTTM, Convert.ToDateTime(dateNowUTC));
                                        if (!isNullProductStartDttm())
                                        {
                                            SetProcessingTime();
                                        }
                                    }

                                    //장비 생산량과 주문 생산량이 같을 경우 장비 멈춤
                                    opcClient.WriteItemValue(opcClient.orderComplete, true);
                                    Thread.Sleep(1000);
                                    opcClient.WriteItemValue(opcClient.orderComplete, false);
                                }
                            }
                        }
                        return;
                    }

                    //주문 생산량
                    if (itemId == opcClient.orderQuantity)
                    {
                        if (value.StartsWith("0")) return;

                        if (!value.Equals(GetErpJobValue("ORDER_QUANTITY")))
                        {
                            SetErpJobValue("ORDER_QUANTITY", value);
                            TbOrderQuantityValue.Text = value;
                        }
                        return;
                    }

                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        #endregion

        #region 사용자 정의 함수
        private void CreateWindowsStartUpRegistry()
        {
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

        private void RemoveWindowsStartUpRegistry()
        {
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

        public void CreateInfoFile()
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
                }
                else
                {
                    string textValue = File.ReadAllText(DATAFILE_PATH);
                    JObject jObject = JObject.Parse(textValue.Trim());
                    KEPSERVER_PATH = (string)jObject["ip"];
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        public void WriteLog(string strMsg)
        {
            try
            {
                Debug.WriteLine(strMsg);
                DirectoryInfo di = new DirectoryInfo(DATA_PATH);
                if (!di.Exists) { di.Create(); }

                FileInfo file = new FileInfo(LOGFILE_PATH);

                StreamWriter sw = new StreamWriter(LOGFILE_PATH, true);
                sw.WriteLine(strMsg);
                sw.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());

            }
        }
        #endregion

        private static SqlConnection CreateConnection(
String ip = "127.0.0.1",
String id = "esamter",
String password = "esamter1!",
String dbName = "ActiveMCN")
        {
            SqlConnectionStringBuilder builder
                = new SqlConnectionStringBuilder();

            builder.DataSource = ip;
            builder.UserID = id;
            builder.Password = password;
            builder.InitialCatalog = dbName;

            return new SqlConnection(builder.ConnectionString);
        }

        private static void IncreaseProdutionProcedure(int value)
        {
            //커넥션 생성
            using (SqlConnection conn = CreateConnection())
            {
                try

                {
                    // sql 커넥션 연결
                    conn.Open();

                    SqlCommand cmd = new SqlCommand("Daemon_Write_Count", conn);

                    cmd.CommandType = CommandType.StoredProcedure;

                    Debug.WriteLine("pdc 프로시저 실행");

                    cmd.Parameters.AddWithValue("@LINE_CD", 1);
                    cmd.Parameters.AddWithValue("@MCN_NM", "ls");
                    Debug.WriteLine(value);
                    cmd.Parameters.AddWithValue("@FLAG", value);

                    int result = cmd.ExecuteNonQuery();
                    Debug.WriteLine("pdc 프로시저 실행 VALUE:{0}, Affected rows:{1}", value, result);
                }
                catch (SqlException se)
                {
                    Console.WriteLine("Sql Error: " + se.Message);

                }
                finally
                {
                    if (conn != null &&
                        conn.State == System.Data.ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
        }

        private static string GetProdutionQuantity()
        {
            //커넥션 생성
            using (SqlConnection conn = CreateConnection())
            {
                try
                {
                    // sql 커넥션 연결
                    conn.Open();
                    SqlDataReader dr;

                    // 데이터 베이스 셀럭트 쿼리
                    SqlCommand cmd = new SqlCommand("select count from pdc_real", conn);

                    // mssql에 쿼리 날림

                    dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        Console.WriteLine(String.Format("{0}", dr["count"]));
                        return String.Format("{0}", dr["count"]);
                    }
                }
                catch (SqlException se)
                {
                    Console.WriteLine("Sql Error: " + se.Message);
                }
                finally
                {
                    if (conn != null &&
                        conn.State == System.Data.ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
            return "0";
        }

        private static string GetErpJobValue(string name)
        {
            //커넥션 생성
            using (SqlConnection conn = CreateConnection())
            {
                try
                {
                    // sql 커넥션 연결
                    conn.Open();
                    SqlDataReader dr;


                    // 데이터 베이스 셀럭트 쿼리
                    SqlCommand cmd = new SqlCommand($"select {name} from ERP_WORK", conn);

                    // mssql에 쿼리 날림

                    dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        Console.WriteLine(String.Format("{0}", dr[$"{name}"]));
                        MainWindow.main.WriteLog(String.Format("ERP_WORK 조회 {0}", dr[$"{name}"]));
                        return String.Format("{0}", dr[$"{name}"]);
                    }
                }
                catch (SqlException se)
                {
                    Console.WriteLine("Sql Error: " + se.Message);
                }
                finally
                {
                    if (conn != null &&
                        conn.State == System.Data.ConnectionState.Open)
                    {
                        conn.Close();
                    }

                }
            }
            return "";
        }

        private static void SetErpJobValue(string name, string value)
        {
            //커넥션 생성
            using (SqlConnection conn = CreateConnection())
            {
                try

                {
                    /*                  if (value.StartsWith("String")) return;
                                        if (value.StartsWith("0")) return;*/

                    // sql 커넥션 연결
                    conn.Open();

                    SqlCommand cmd = new SqlCommand("UPDATE_ERP_WORK", conn);

                    cmd.CommandType = CommandType.StoredProcedure;

                    Debug.WriteLine($"UPDATE_ERP_WORK {name} 프로시저 실행");

                    cmd.Parameters.AddWithValue("@NAME", name);
                    cmd.Parameters.AddWithValue("@Value", value);

                    int result = cmd.ExecuteNonQuery();
                    Debug.WriteLine("UPDATE_ERP_WORK  프로시저 실행 VALUE:{0}, Affected rows:{1}", value, result);

                }
                catch (SqlException se)
                {
                    Console.WriteLine("Sql Error: " + se.Message);

                }
                finally
                {
                    if (conn != null &&
                        conn.State == System.Data.ConnectionState.Open)
                    {
                        conn.Close();
                    }
                }
            }
        }
    }
}
