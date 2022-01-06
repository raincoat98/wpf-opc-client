using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;

namespace WpfSamterOpcClient
{
    internal class OpcClient
    {
        public string endpointURL;
        public string nodeId = "ns=2;s=wookil.diecutter1.plc.";

        public Session session;
        private ApplicationConfiguration config;
        private MonitoredItem monitoredItem;
        private Subscription subscription;

        /* 출처 https://m.blog.naver.com/yeo2697/222083701071*/
        public async Task Opcua_start(string endPointUrl)
        {
            try
            {
                // Config
                Debug.WriteLine("Step 1 - Create a config.");
                config = CreateOpcUaAppConfiguration();

                // Create Session
                Debug.WriteLine("Step 2 - Create a session with your server.");
                session = await Session.Create(config, new ConfiguredEndpoint(null, new EndpointDescription(endPointUrl)), true, "", 60000, null, null);

                // Browse Session
                Debug.WriteLine("Step 3 - Browse the server namespace.");
                ReferenceDescriptionCollection refs;
                Byte[] cp;
                session.Browse(null, null, ObjectIds.ObjectsFolder, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, out cp, out refs);

                // Create Subscription
                Debug.WriteLine("Step 4 - Create a subscription. Set a faster publishing interval if you wish.");
                subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 1000, PublishingEnabled = true };

                Debug.WriteLine("Step 5 - Add a list of items you wish to monitor to the subscription.");
                int[] item = new int[] {123};

                for (int i = 0; i < item.Length; i++)
                {
                    Debug.WriteLine(nodeId + item[i]);
                    monitoredItem = new MonitoredItem(subscription.DefaultItem);
                    monitoredItem.StartNodeId = nodeId + item[i];
                    monitoredItem.AttributeId = Attributes.Value;
                    monitoredItem.MonitoringMode = MonitoringMode.Reporting;
                    monitoredItem.SamplingInterval = 1000;
                    monitoredItem.QueueSize = 0;
                    monitoredItem.DiscardOldest = true;
                    monitoredItem.Notification += new MonitoredItemNotificationEventHandler(monitoredItem_Notification);
                    subscription.AddItem(monitoredItem);
                }

                Debug.WriteLine("Step 6 - Add the subscription to the session.");
                // session에 subscription 추가
                session.AddSubscription(subscription);
                // subscription 생성
                subscription.Create();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return;
            }
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

            Debug.WriteLine("value: " + Utils.Format("{0}", notification.Value.WrappedValue.ToString()) +
              " // StatusCode: " + Utils.Format("{0}", notification.Value.StatusCode.ToString()));
        }

        // item 값 수정 시 사용
        public async Task WriteItemValue(string itemId, Boolean value)
        {
            if(session != null)
            {
                try
                {
                    WriteValue valueToWrite = new WriteValue();

                    valueToWrite.NodeId = nodeId + itemId;
                    valueToWrite.AttributeId = Attributes.Value;
                    valueToWrite.Value.Value = value;
                    valueToWrite.Value.StatusCode = StatusCodes.Good;
                    valueToWrite.Value.ServerTimestamp = DateTime.MinValue;
                    valueToWrite.Value.SourceTimestamp = DateTime.MinValue;

                    WriteValueCollection valuesToWrite = new WriteValueCollection();
                    valuesToWrite.Add(valueToWrite);

                    // write current value.
                    StatusCodeCollection results = null;
                    DiagnosticInfoCollection diagnosticInfos = null;

                    session.Write(
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
