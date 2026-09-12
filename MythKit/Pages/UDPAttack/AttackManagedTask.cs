using iNKORE.UI.WPF.Modern.Controls;
using MythKit.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit.Pages.UDPAttack
{
    public enum WaitingState
    {
        None,
        CycleInterval,
        GroupInterval
    }
    public class AttackManagedTask : IManagedTask, INotifyPropertyChanged
    {
        public string TaskName => "UDP 重放攻击";
        public TaskState State => TaskState.Running;
        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
                ProgressUpdated?.Invoke((int)_progress);
            }
        }
        public void ProgressBackgroundUpdate(double progress)
        {
            _progress = progress;
            OnPropertyChanged(nameof(Progress));
            ProgressUpdatedBackground?.Invoke((int)progress);
        }
        private AttackStateControl _attackStateControl;
        public FrameworkElement DetailContentView { get => _attackStateControl; set => throw new NotImplementedException(); }
        public bool IsDetailViewReady => DetailContentView != null;
        public bool IsIndeterminate => Config.TotalCycles == null;
        public AttackConfig Config { get; set; }
        public event Action<int> ProgressUpdated;
        public event Action<string> StateMessageUpdated;
        public event Action<TaskState> StateUpdated;
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int> ProgressUpdatedBackground;
        public event Action<string> StateMessageUpdatedBackground;

        public AttackManagedTask(AttackConfig config)
        {
            Config = config;
            _attackStateControl = new AttackStateControl(this);
        }

        private int _currentCycle = 1;
        public int CurrentCycle
        {
            get => _currentCycle;
            private set
            {
                _currentCycle = value;
                OnPropertyChanged();
            }
        }

        private int _currentGroup = 1;
        public int CurrentGroup
        {
            get => _currentGroup;
            private set
            {
                _currentGroup = value;
                OnPropertyChanged();
            }
        }

        private int _currentTargetIndex = 1;
        public int CurrentTargetIndex
        {
            get => _currentTargetIndex;
            private set
            {
                _currentTargetIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentAttackCount));
                OnPropertyChanged(nameof(CurrentTargetIndexUI));
            }
        }
        public int CurrentTargetIndexUI => CurrentTargetIndex - 1; // 用于 UI 显示的索引，从 0 开始
        private WaitingState _currentWaitingState = WaitingState.None;
        public WaitingState CurrentWaitingState
        {
            get => _currentWaitingState;
            private set
            {
                _currentWaitingState = value;
                OnPropertyChanged();
            }
        }
        private int _remainingWaitingSeconds = 0;
        public int RemainingWaitingSeconds
        {
            get => _remainingWaitingSeconds;
            private set
            {
                _remainingWaitingSeconds = value;
                OnPropertyChanged();
            }
        }
        public int CurrentAttackCount => (CurrentCycle - 1) * Config.TargetIPs.Count + CurrentTargetIndex;
        public int? TotalAttackCount => Config.TotalCycles.HasValue ? (int?)(Config.TotalCycles.Value * Config.TargetIPs.Count) : null;
        private bool _paused = false;
        public bool Paused
        {
            get => _paused;
            set
            {
                _paused = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            StateUpdated?.Invoke(TaskState.Running);
            try
            {
                string totalCycleIndicator = Config.TotalCycles.HasValue ? $" / {Config.TotalCycles.Value}" : "";
                for (; Config.TotalCycles == null || CurrentCycle <= Config.TotalCycles.Value; CurrentCycle++)
                {
                    CurrentTargetIndex = 1;
                    if (CurrentCycle != 1)
                    {
                        CurrentWaitingState = WaitingState.CycleInterval;
                        await WaitWithStatusAsync(Config.CycleIntervalMilliseconds, cancellationToken, $"第 {CurrentCycle - 1}{totalCycleIndicator} 轮攻击结束，等待间隔");
                        CurrentWaitingState = WaitingState.None;
                    }
                    CurrentGroup = 1;
                    for (; CurrentTargetIndex <= Config.TargetIPs.Count; CurrentTargetIndex++)
                    {
                        if (Config.GroupConfig != null && CurrentTargetIndex != 1 && (CurrentTargetIndex - 1) % Config.GroupConfig.Value.SingleGroupSize == 0)
                        {
                            CurrentWaitingState = WaitingState.GroupInterval;
                            await WaitWithStatusAsync(Config.GroupConfig.Value.GroupIntevalMilliseconds, cancellationToken, $"第 {CurrentCycle}{totalCycleIndicator} 轮，第 {CurrentGroup} 组攻击结束，等待组间间隔");
                            CurrentGroup++;
                            CurrentWaitingState = WaitingState.None;
                        }
                        var targetIP = Config.TargetIPs[CurrentTargetIndex - 1];
                        StateMessageUpdatedBackground?.Invoke($"第 {CurrentCycle} 轮，第 {CurrentTargetIndex}/{Config.TargetIPs.Count} 个目标: {targetIP}");

                        // 构造数据包并发送
                        AttackPacket packets = null; string errorMessage = "";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            packets = Config.AttackPattern.ConstructPacket(ref errorMessage, targetIP, CurrentCycle, CurrentGroup);
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
                        if (errorMessage != "") AddErrorMessage(errorMessage);
                        if (Config.TotalCycles.HasValue)
                        {
                            ProgressBackgroundUpdate(100.0 * CurrentAttackCount / TotalAttackCount.Value);
                        }
                        if (Paused)
                        {
                            StateMessageUpdated?.Invoke("攻击已暂停。点击详情按钮操控攻击启停");
                            while (Paused)
                            {
                                await Task.Delay(500, cancellationToken);
                            }
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            StateUpdated?.Invoke(TaskState.Cancelled);
                            return;
                        }
                    }
                }
                Progress = 100;
                StateUpdated?.Invoke(TaskState.Completed);
            }
            catch (OperationCanceledException)
            {
                StateUpdated?.Invoke(TaskState.Cancelled);
                return;
            }
        }
        private async Task WaitWithStatusAsync(int totalMilliseconds, CancellationToken cancellationToken, string baseMessage)
        {
            const int updateIntervalMs = 1000; // 每秒更新一次
            int elapsed = 0;

            while (elapsed < totalMilliseconds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 计算剩余等待时间
                int remaining = totalMilliseconds - elapsed;
                int delay = Math.Min(updateIntervalMs, remaining);

                RemainingWaitingSeconds = (remaining + 999) / 1000; // 向上取整
                StateMessageUpdated?.Invoke($"{baseMessage}，剩余 {RemainingWaitingSeconds} 秒...");

                await Task.Delay(delay, cancellationToken);
                elapsed += delay;
            }
        }
        private void AddErrorMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _attackStateControl.LogTextBox.Text += message + "\n";
            });
        }
    }
}
