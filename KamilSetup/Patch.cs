using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PandaAudioSetup
{
    class Patch
    {
        /// <summary>
        /// 是否必须管理员运行
        /// </summary>
        /// <returns></returns>
        public static bool MustRunInAdmin()
        {
            string value = SysEnvironment.GetSysEnvironmentByName("KMP_WARNINGS");
            if (value != "0")
            {
                return true;
            }
            return false;
        }
        public static void Fix()
        {
            try
            {
                string value = SysEnvironment.GetSysEnvironmentByName("KMP_WARNINGS");
                if (value != "0")
                {
                    //设置系统变量 KMP_WARNINGS，否则恐龙会报错
                    SysEnvironment.SetSysEnvironment("KMP_WARNINGS", "0");
                }
            }
            catch (Exception ex)
            {
                
            }
            
        }
    }

    public static class SysEnvironment
    {


        #region --获取注册表中的环境变量

        /// <summary>
        /// 获取系统环境变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetSysEnvironmentByName(string name)
        {
            string result = string.Empty;
            try
            {
                result = OpenSysEnvironment().GetValue(name).ToString();//读取
            }
            catch (Exception)
            {

                return string.Empty;
            }
            return result;

        }

        /// <summary>
        /// 打开系统环境变量注册表
        /// </summary>
        /// <returns>RegistryKey</returns>
        private static RegistryKey OpenSysEnvironment()
        {
            RegistryKey regLocalMachine = Registry.LocalMachine;
            RegistryKey regSYSTEM = regLocalMachine.OpenSubKey("SYSTEM", true);//打开HKEY_LOCAL_MACHINE下的SYSTEM 
            RegistryKey regControlSet001 = regSYSTEM.OpenSubKey("ControlSet001", true);//打开ControlSet001 
            RegistryKey regControl = regControlSet001.OpenSubKey("Control", true);//打开Control 
            RegistryKey regManager = regControl.OpenSubKey("Session Manager", true);//打开Control 

            RegistryKey regEnvironment = regManager.OpenSubKey("Environment", true);
            return regEnvironment;
        }

        /// <summary>
        /// 设置系统环境变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="strValue">值</param>
        public static void SetSysEnvironment(string name, string strValue)
        {
            OpenSysEnvironment().SetValue(name, strValue);
        }
        #endregion


        /// <summary>
        /// 检测系统环境变量是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool CheckSysEnvironmentExist(string name)
        {
            if (!string.IsNullOrEmpty(GetSysEnvironmentByName(name)))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 添加到PATH环境变量（会检测路径是否存在，存在就不重复）
        /// </summary>
        /// <param name="strPath"></param>
        public static void SetPathAfter(string strHome)
        {
            string pathlist;
            pathlist = GetSysEnvironmentByName("PATH");

            bool isPathExist = false;
            if (pathlist.Length > 1)
            {
                //检测是否以;结尾
                if (pathlist.Substring(pathlist.Length - 1, 1) != ";") //截取最后一个字符,判断这个字符不等于;号,
                {
                    SetSysEnvironment("PATH", pathlist + ";");
                    pathlist = GetSysEnvironmentByName("PATH");
                }
                string[] list = pathlist.Split(';');//以;切割


                foreach (string item in list)
                {
                    if (item == strHome)
                        isPathExist = true;
                }
            }
            if (!isPathExist)
            {
                SetSysEnvironment("PATH", pathlist + strHome + ";");
            }
        }

        public static void SetPathBefore(string strHome)
        {

            string pathlist;
            pathlist = GetSysEnvironmentByName("PATH");
            string[] list = pathlist.Split(';');
            bool isPathExist = false;

            foreach (string item in list)
            {
                if (item == strHome)
                    isPathExist = true;
            }
            if (!isPathExist)
            {
                SetSysEnvironment("PATH", strHome + ";" + pathlist);
            }

        }

        public static void SetPath(string strHome)
        {
            string pathlist;
            pathlist = GetSysEnvironmentByName("PATH");
            string[] list = pathlist.Split(';');
            bool isPathExist = false;

            foreach (string item in list)
            {
                if (item == strHome)
                    isPathExist = true;
            }
            if (!isPathExist)
            {
                SetSysEnvironment("PATH", pathlist + strHome + ";");
            }
        }
    }

    //Kernel32.DLL内有SetEnvironmentVariable函数用于设置系统环境变量
    //C#调用要用DllImport，代码封装如下：
    class SetSysEnvironmentVariable
    {
        [DllImport("Kernel32.DLL ", SetLastError = true)]
        public static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        public static void SetPath(string pathValue)
        {
            string pathlist;
            pathlist = SysEnvironment.GetSysEnvironmentByName("PATH");
            string[] list = pathlist.Split(';');
            bool isPathExist = false;

            foreach (string item in list)
            {
                if (item == pathValue)
                    isPathExist = true;
            }
            if (!isPathExist)
            {
                SetEnvironmentVariable("PATH", pathlist + pathValue + ";");

            }

        }
    }

}
