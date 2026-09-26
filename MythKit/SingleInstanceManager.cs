using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MythKit
{
    public static class SingleInstanceManager
    {
        private const string AppId = "MythKit_CreeperMPG";
        private static Mutex _mutex;
        private static NamedPipeServerStream _pipeServer;

        /// <summary>
        /// 另一个实例启动并发送通知时触发。参数为对方的命令行参数。
        /// </summary>
        public static event Action<string[]> OtherInstanceStarted;

        /// <summary>
        /// 在 App.OnStartup 中调用
        /// </summary>
        /// <returns>是否为第一个实例</returns>
        public static bool Initialize()
        {
            _mutex = new Mutex(true, $"Global\\{AppId}", out bool createdNew);

            if (createdNew)
            {
                StartPipeServer();
                return true;
            }
            else
            {
                NotifyFirstInstance();
                _mutex.Dispose();
                return false;
            }
        }
        private static CancellationTokenSource _cts;

        private static void StartPipeServer()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    NamedPipeServerStream server = null;
                    try
                    {
                        server = new NamedPipeServerStream(
                            AppId,
                            PipeDirection.In,
                            1,
                            PipeTransmissionMode.Message,
                            PipeOptions.Asynchronous);

                        // 挂起等待客户端，不占CPU；支持取消
                        await server.WaitForConnectionAsync(token);

                        using (var reader = new StreamReader(server))
                        {
                            // 关键：加读超时，防止对方连上不发数据卡死
                            var readTask = reader.ReadToEndAsync();
                            var timeoutTask = Task.Delay(3000, token);
                            var completed = await Task.WhenAny(readTask, timeoutTask);

                            if (completed == readTask)
                            {
                                string content = await readTask;
                                string[] args = content.Split(
                                    new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                                Application.Current?.Dispatcher.Invoke(() =>
                                    OtherInstanceStarted?.Invoke(args));
                            }
                            // 超时则忽略本轮，继续下一轮
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常退出
                    }
                    catch (IOException)
                    {
                        // 客户端异常断开等，忽略，继续监听
                    }
                    finally
                    {
                        server?.Dispose();
                    }
                }
            }, token);
        }


        private static void NotifyFirstInstance()
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", AppId, PipeDirection.Out))
                {
                    // 等待连接，设置超时防止死等
                    client.Connect(3000);

                    using (var writer = new StreamWriter(client))
                    {
                        // 发送当前实例的命令行参数
                        string args = string.Join("\n", Environment.GetCommandLineArgs());
                        writer.Write(args);
                        writer.Flush();
                    }
                }
            }
            catch (TimeoutException)
            {
                // 连接超时，可能第一个实例未响应，静默失败
            }
            catch (IOException)
            {
                // 管道通信异常，静默失败
            }
        }
        public static void Cleanup()
        {
            try
            {
                try { _cts?.Cancel(); } catch { }
                _cts?.Dispose();
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch { }
        }
    }
}
