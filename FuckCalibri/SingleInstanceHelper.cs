
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace FuckCalibri {

    using static WinApi;

    public class SingleInstanceHelper {
        
        private static Mutex _mutex;

        private static uint _mutexMessage;

        public static bool IsSingleInstance { get; private set; }

        /// <summary>
        /// 响应通知
        /// * 前启动的进程是否响应后启动进程的通知消息
        /// </summary>
        public static bool ReactToNotification { get; private set; }


        /// <summary>
        /// 检查是否已有实例在运行
        /// </summary>
        /// <typeparam name="T">锚点类型</typeparam>
        /// <param name="silent">是否不响应重复启动的进程发送的消息</param>
        public static void Check<T>(bool silent = false) => Check(typeof(T), silent);

        /// <summary>
        /// 检查是否已有实例在运行
        /// </summary>
        /// <param name="type">锚点类型</param>
        /// <param name="silent">是否不响应重复启动的进程发送的消息</param>
        public static void Check(Type type, bool silent = false) {
            ReactToNotification = !silent;

            var assemblyGuid = type.Assembly.GetCustomAttribute<GuidAttribute>()?.Value;
            if (string.IsNullOrWhiteSpace(assemblyGuid))
                assemblyGuid = type.FullName;
            else
                assemblyGuid = type.Namespace + "/" + assemblyGuid;

            _mutexMessage = RegisterWindowMessage(assemblyGuid);

            _mutex = new Mutex(true, $"FuckCalibri/{assemblyGuid}", out bool createNew);
            IsSingleInstance = createNew;
        }

        public static bool IsMutextMessage(int msg) => msg == _mutexMessage;

        public static void Notify() {
            if (_mutexMessage > 0)
                PostMessage((IntPtr)HNDL_BROADCAST, _mutexMessage, 0, 0);
        }

        public static void Release() {
            _mutex?.ReleaseMutex();
            _mutex = null;
            _mutexMessage = 0;
        }
    }
}
