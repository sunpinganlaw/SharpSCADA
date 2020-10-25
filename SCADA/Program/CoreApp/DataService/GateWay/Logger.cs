using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using log4net.Repository;

namespace GateWay
{
    class Logger
    {
        private static ILoggerRepository loggerRepository;

        public static ILoggerRepository LoggerRepository { get; private set; }
        public static ILog Log { get; private set; }

        /// <summary>
        /// 静态构造函数，程序启动时自动执行
        /// </summary>
        static Logger()
        {
            LoggerRepository = CreateLoggerRepository();
            LoadLog4NetConfig();
        }

        /// <summary>
        /// 初始化日志
        /// </summary>
        /// <returns></returns>
        public static void LoadLogger()
        {
            LoggerRepository = CreateLoggerRepository();
            LoadLog4NetConfig();
        }

        /// <summary>
        /// 创建日志仓储实例
        /// </summary>
        /// <returns></returns>
        private static ILoggerRepository CreateLoggerRepository()
        {
            loggerRepository = loggerRepository ?? LogManager.CreateRepository("GlobalExceptionHandler"); // 单例
            return loggerRepository;
        }

        /// <summary>
        /// 加载log4net配置
        /// </summary>
        private static void LoadLog4NetConfig()
        {
            // 配置log4net
            log4net.Config.XmlConfigurator.Configure(loggerRepository, new System.IO.FileInfo(System.IO.Directory.GetCurrentDirectory() + "/config/log4net.config"));

            // 创建log实例
            Log = LogManager.GetLogger(loggerRepository.Name, AppDomain.CurrentDomain.FriendlyName);

            Log.Info("已加载日志配置");
        }
    }
}
