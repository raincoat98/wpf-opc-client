using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfSamterOpcClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Mutex mutex;

        /// <summary>
        /// 중복 실행 방지 코드
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string mutexName = "program";
            bool createNew;

            mutex = new Mutex(true, mutexName, out createNew);

            if (!createNew)
            {
                Shutdown();
            }
        }
    }
}
