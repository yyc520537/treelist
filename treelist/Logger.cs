using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace treelist
{
    public class Logger
    {
        static Logger()
        {
            XmlConfigurator.Configure(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigFile//log4Net.config")));
            ILog log = LogManager.GetLogger(typeof(Logger));
            log.Info("系统初始化Logger模块");
        }
        private ILog logger = null;
        public Logger(Type type)
        {
            logger = LogManager.GetLogger(type);
        }
        //异常信息
        public void Error(string msg = "出现异常", Exception ex = null)
        {
            Console.WriteLine(msg);
            logger.Error(msg, ex);
        }
        //警告信息
        public void Warn(string msg)
        {
            Console.WriteLine(msg);
            logger.Warn(msg);
        }
        //正常信息
        public void Info(string msg)
        {
            Console.WriteLine(msg);
            logger.Info(msg);
        }
        //调试信息
        public void Debug(string msg)
        {
            Console.WriteLine(msg);
            logger.Debug(msg);
        }
    }
}
