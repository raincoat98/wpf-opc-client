using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace WpfSamterOpcClient
{
    internal class OpcClient
    {
        public string endpointURL;

        private UInt16 nameSpaceIndex = 2;

        public readonly string run = "wookil.diecutter1.run";
        public readonly string stop = "wookil.diecutter1.stop";
        public readonly string error = "wookil.diecutter1.error";
        public readonly string speed = "wookil.diecutter1.currentSpeed";
        public readonly string jobOrder = "wookil-mes.diecutter1.jobOrder";
        public readonly string articleCode = "wookil-mes.diecutter1.articleCode";
        public readonly string equipCode = "wookil-mes.diecutter1.equipCode";
        public readonly string orderComplete = "wookil.diecutter1.orderCompleted";
        public readonly string quantity = "wookil.diecutter1.prodQuantity";
        public readonly string orderQuantity = "wookil.diecutter1.orderQuantity";
        public readonly string startDTTM = "wookil-mes.diecutter1.startDTTM";
        public readonly string endDTTM = "wookil-mes.diecutter1.endDTTM";
        public readonly string processingTime = "wookil-mes.diecutter1.processingTime";

        private Session m_session = null;
        private ApplicationConfiguration config;
        private MonitoredItem monitoredItem;
        private Subscription subscription;

        private SessionReconnectHandler m_reconnectHandler;
        private EventHandler m_ReconnectStarting;
        private EventHandler m_KeepAliveComplete;
        private EventHandler m_ReconnectComplete;
        private int m_reconnectPeriod = 60;

        /* 출처 https://m.blog.naver.com/yeo2697/222083701071*/
        public async Task Opcua_start(string endPointUrl)
        {
            try
            {
                // Config
                MainWindow.main.writeLog("Step 1 - Create a config.");
                config = CreateOpcUaAppConfiguration();

                // Create Session
                MainWindow.main.writeLog("Step 2 - Create a session with your server.");
                m_session = await Session.Create(config, new ConfiguredEndpoint(null, new EndpointDescription(endPointUrl)), true, "", 60000, null, null);
                m_session.KeepAlive += new KeepAliveEventHandler(Session_KeepAlive);


                // Browse Session
                MainWindow.main.writeLog("Step 3 - Browse the server namespace.");
                ReferenceDescriptionCollection refs;
                Byte[] cp;
                m_session.Browse(null, null, ObjectIds.ObjectsFolder, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, out cp, out refs);

                MainWindow.main.SetConnectItemValue();

                // Create Subscription
                MainWindow.main.writeLog("Step 4 - Create a subscription. Set a faster publishing interval if you wish.");
                subscription = new Subscription(m_session.DefaultSubscription) { PublishingInterval = 1000, PublishingEnabled = true };

                MainWindow.main.writeLog("Step 5 - Add a list of items you wish to monitor to the subscription.");
                string[] item = { run, stop, error, speed, jobOrder, articleCode, orderComplete, quantity, orderQuantity, startDTTM, endDTTM, processingTime };

                for (int i = 0; i < item.Length; i++)
                {
                    monitoredItem = new MonitoredItem(subscription.DefaultItem);
                    monitoredItem.StartNodeId = new NodeId(item[i], nameSpaceIndex);
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    subscription.AddItem(monitoredItem);
                }

                MainWindow.main.writeLog("Step 6 - Add the subscription to the session.");
                // session에 subscription 추가
                m_session.AddSubscription(subscription);
                // subscription 생성
                subscription.Create();
            }
            catch (Exception e)
            {
                MessageBox.Show("Connection failed: Please check the server status and reconnect.");
                Debug.WriteLine(e.ToString());
                return;
            }
        }

        private void Server_ReconnectComplete(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine("reconnect Complete");

                // ignore callbacks from discarded objects.
                if (!Object.ReferenceEquals(sender, m_reconnectHandler))
                {
                    return;
                }

                m_session = m_reconnectHandler.Session;
                m_reconnectHandler.Dispose();
                m_reconnectHandler = null;

                // raise any additional notifications.
                if (m_ReconnectComplete != null)
                {
                    m_ReconnectComplete(this, e);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(String.Format("[OPC UA Reconnect ERROR][Function] {0}", exception));
            }
        }

        private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            try
            {
                // check for events from discarded sessions.
                if (!Object.ReferenceEquals(session, m_session))
                {
                    return;
                }

                // start reconnect sequence on communication error.
                if (ServiceResult.IsBad(e.Status))
                {
                    if (m_reconnectPeriod <= 0)
                    {
                        Debug.WriteLine(String.Format("[Reconncet ERROR] {0}", e.Status));
                        return;
                    }

                    Debug.WriteLine(String.Format("[Reconnceting] {0}s", m_reconnectPeriod));

                    if (m_reconnectHandler == null)
                    {
                        if (m_ReconnectStarting != null)
                        {
                            m_ReconnectStarting(this, e);
                        }

                        m_reconnectHandler = new SessionReconnectHandler();
                        m_reconnectHandler.BeginReconnect(m_session, m_reconnectPeriod * 1000, Server_ReconnectComplete);
                    }

                    return;
                }

                // raise any additional notifications.
                if (m_KeepAliveComplete != null)
                {
                    m_KeepAliveComplete(this, e);
                }
            }
            catch (Exception exception)
            {
            }
        }

        public void Disconnect()
        {
            m_session.Dispose();
            MainWindow.main.InitItemValue();
        }

        // 인증서
        private ApplicationConfiguration CreateOpcUaAppConfiguration()
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "OPC UA Client",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier(),
                    AutoAcceptUntrustedCertificates = true //신뢰할 수 없는 인증서 허용
                },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            config.Validate(ApplicationType.Client);

            //신뢰할 수 없는 인증서 허용
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) =>
                {
                    e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
                };
            }

            return config;
        }

        // Node 모니터링 시 사용
        public void monitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;
            if (notification == null)
            {
                return;
            }

            string NodeId = monitoredItem.StartNodeId.ToString();
            string Value = notification.Value.WrappedValue.ToString();
            string StatusCode = notification.Value.StatusCode.ToString();

            MainWindow.main.writeLog($"NodeId: {NodeId} // value: {Value} // StatusCode: {StatusCode} // TimeStemp: {DateTime.Now}");

            //TODO: 안티패턴 해결 필요
            if (StatusCode != "Bad")
            {
                string itemId = NodeId.Replace("ns=2;s=", "");
                MainWindow.main.SetChangeItemValue(itemId, Value);
            }

        }
        // item 값 가져올 때 사용
        public DataValue ReadItemValue(string itemId)
        {
            try
            {
                ReadValueId itemToRead = new ReadValueId();

                // Read할 NodeId 설정
                itemToRead.NodeId = new NodeId(itemId, nameSpaceIndex);

                itemToRead.AttributeId = Attributes.Value;

                ReadValueIdCollection itemsToRead = new ReadValueIdCollection();
                itemsToRead.Add(itemToRead);

                // read from server.
                DataValueCollection values = null;
                DiagnosticInfoCollection diagnosticInfos = null;
                itemToRead.AttributeId = Attributes.Value;

                ResponseHeader responseHeader = m_session.Read(
                    null,
                    0,
                    TimestampsToReturn.Both,
                    itemsToRead,
                    out values,
                    out diagnosticInfos);

                ClientBase.ValidateResponse(values, itemsToRead);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, itemsToRead);

                if (StatusCode.IsBad(values[0].StatusCode))
                {
                    ServiceResult result = ClientBase.GetResult(values[0].StatusCode, 0, diagnosticInfos, responseHeader);
                    Debug.WriteLine(result);
                    throw new ServiceResultException(result);

                }
                // Read한 Item의 Value 값 Return
                return values[0];
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                return null;
            }
        }

        // item 값 수정 시 사용
        public void WriteItemValue(string itemId, dynamic value)
        {
            if (m_session != null)
            {
                try
                {
                    WriteValue valueToWrite = new WriteValue();

                    valueToWrite.NodeId = new NodeId(itemId, nameSpaceIndex);
                    valueToWrite.AttributeId = Attributes.Value;

                    if (value is Int32)
                    {
                        value = Convert.ToInt32(value);
                    }

                    object objvalue = value;
                    DataValue m_value = new DataValue()
                    {
                        WrappedValue = value,
                        SourceTimestamp = DateTime.Now,
                        Value = objvalue
                    };

                    valueToWrite.Value = m_value;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    m_session.Write(
                        null,
                        valuesToWrite,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, valuesToWrite);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, valuesToWrite);

                    if (StatusCode.IsBad(results[0]))
                    {
                        throw new ServiceResultException(results[0]);
                    }

                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                }
            }
        }
    }
}
