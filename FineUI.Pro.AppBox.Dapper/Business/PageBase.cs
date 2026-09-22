using Dapper;
using FineUI.Pro;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;


namespace FineUI.Pro.AppBox.Dapper
{
    public class PageBase : System.Web.UI.Page
    {
        #region 只读静态变量

        // Session key
        private static readonly string SK_ONLINE_UPDATE_TIME = "OnlineUpdateTime";
        //private static readonly string SK_USER_ROLE_ID = "UserRoleId";

        private static readonly string CHECK_POWER_FAIL_PAGE_MESSAGE = "您无权访问此页面！";
        private static readonly string CHECK_POWER_FAIL_ACTION_MESSAGE = "您无权进行此操作！";



        #endregion
		
		#region 数据库连接实例
		
		/// <summary>
        /// 每个请求共享一个数据库连接实例
        /// </summary>
        public static IDbConnection DB
        {
            get
            {
                // http://stackoverflow.com/questions/6334592/one-dbcontext-per-request-in-asp-net-mvc-without-ioc-container
                if (!HttpContext.Current.Items.Contains("__FineUI.Pro.AppBoxContext"))
                {
                    HttpContext.Current.Items["__FineUI.Pro.AppBoxContext"] = GetDbConnection();
                }
                return HttpContext.Current.Items["__FineUI.Pro.AppBoxContext"] as IDbConnection;
            }
        }


        /// <summary>
        /// 数据库连接实例
        /// </summary>
        /// <returns></returns>
        public static IDbConnection GetDbConnection()
        {
            //var database = ConfigurationManager.AppSettings["Database"];

            var connectionStringSection = ConfigurationManager.ConnectionStrings["MySQL"];
            var connectionString = connectionStringSection.ToString();

            IDbConnection connection = new MySqlConnection(connectionString);

            //if (connectionStringSection.ProviderName.StartsWith("MySql"))
            //{
            //    connection = new MySqlConnection(connectionString);
            //}
            //else
            //{
            //    connection = new SqlConnection(connectionString);
            //}

            // 打开数据库连接
            connection.Open();

            return connection;
        }
		
		#endregion

        #region 浏览权限

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public virtual string ViewPower
        {
            get
            {
                return String.Empty;
            }
        }

        #endregion

        #region 页面初始化

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // 此用户是否有访问此页面的权限
            if (!CheckPowerView())
            {
                CheckPowerFailWithPage();
                return;
            }

            // 设置主题
            var pm = PageManager.Instance;
            if (pm != null)
            {
                HttpCookie themeCookie = Request.Cookies["Theme"];
                if (themeCookie != null)
                {
                    string themeValue = themeCookie.Value;

                    // 是否为内置主题
                    if (IsSystemTheme(themeValue))
                    {
                        pm.CustomTheme = String.Empty;
                        pm.Theme = (Theme)Enum.Parse(typeof(Theme), themeValue, true);
                    }
                    else
                    {
                        pm.CustomTheme = themeValue;
                    }
                }

                // 为所有页面添加公共JS：<script type="text/css" href="res/js/common.js"></script>
                var commonJSPath = String.Format("<script type=\"text/javascript\" src=\"{0}\"></script>", PageContext.ResolveUrl("~/res/js/common.js?v" + GlobalConfig.ProductVersion));
                PageContext.RegisterPostStartupScript("FineUI.Pro.AppBox_Dapper_common_js", commonJSPath, false);
                // AppBox 专属脚本（下载源码 / 最大化 / 帮助菜单 / 关闭弹出窗口）。
                // 必须和 common.js 同层注册：这些回调在 iframe 子页里也会被调用。
                var appboxJSPath = String.Format("<script type=\"text/javascript\" src=\"{0}\"></script>", PageContext.ResolveUrl("~/res/js/appbox.js?v" + GlobalConfig.ProductVersion));
                PageContext.RegisterPostStartupScript("FineUI.Pro.AppBox_Dapper_appbox_js", appboxJSPath, false);


                // 页面水印
                ApplyPageWatermark(pm);
            }

            UpdateOnlineUser(GetIdentityID());

            // 设置页面标题
            Page.Title = String.Format("{0} - 通用权限管理系统", ConfigHelper.Title);

            // 禁用表单的自动完成功能
            Form.Attributes["autocomplete"] = "off";
        }

        private void ApplyPageWatermark(PageManager pm)
        {
            // 未登录时不需要页面水印
            if (!User.Identity.IsAuthenticated)
            {
                return;
            }
            // 登录后的框架页不需要页面水印
            if (this is FineUI.Pro.AppBox.Dapper.main)
            {
                return;
            }

            // 当前登录用户名称和角色信息

            var watermarkText = GetIdentityName();
            var roleNames = GetIdentityRoleNames();
            if (roleNames.Count > 0)
            {
                watermarkText = $"{roleNames[0]}（{watermarkText}）";
            }
            // 将当前登录用户名称显式为页面水印
            pm.EnableWatermark = true;
            pm.WatermarkFontSize = 16;
            pm.WatermarkText = watermarkText;

        }

        private bool IsSystemTheme(string themeName)
        {
            themeName = themeName.ToLower();
            string[] themes = Enum.GetNames(typeof(Theme));
            foreach (string theme in themes)
            {
                if (theme.ToLower() == themeName)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 请求参数

        /// <summary>
        /// 获取查询字符串中的参数值
        /// </summary>
        protected string GetQueryValue(string queryKey)
        {
            return Request.QueryString[queryKey];
        }


        /// <summary>
        /// 获取查询字符串中的参数值
        /// </summary>
        protected int GetQueryIntValue(string queryKey)
        {
            int queryIntValue = -1;
            try
            {
                queryIntValue = Convert.ToInt32(Request.QueryString[queryKey]);
            }
            catch (Exception)
            {
                // ...
            }

            return queryIntValue;
        }

        #endregion

        #region 在线用户相关

        protected void UpdateOnlineUser(int? userID)
        {
            if (userID == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            object lastUpdateTime = Session[SK_ONLINE_UPDATE_TIME];
            if (lastUpdateTime == null || (now.Subtract(Convert.ToDateTime(lastUpdateTime)).TotalMinutes > 5))
            {
                // 记录本次更新时间
                Session[SK_ONLINE_UPDATE_TIME] = now;

                Online online = DB.QueryFirstOrDefault<Online>("SELECT * FROM Onlines WHERE UserID = @UserID", new { UserID = userID });

                if (online != null)
                {
                    DB.Execute("UPDATE Onlines SET UpdateTime = @UpdateTime WHERE UserID = @UserID", new { UpdateTime = now, UserID = userID });
                }

            }
        }

        protected void RegisterOnlineUser(User user)
        {
            DateTime now = DateTime.Now;

            Online online = DB.QueryFirstOrDefault<Online>("SELECT * FROM Onlines WHERE UserID = @UserID", new { UserID = user.ID });

            // 如果不存在，就创建一条新的记录
            var isNew = false;
            if (online == null)
            {
                isNew = true;
                online = new Online();
            }
            online.UserID = user.ID;
            online.IPAdddress = Request.UserHostAddress;
            online.LoginTime = now;
            online.UpdateTime = now;


            if (isNew)
            {
                ExecuteInsert<Online>(online, "UserID", "IPAdddress", "LoginTime", "UpdateTime");
                //DB.Execute("INSERT Onlines (UserID, IPAdddress, LoginTime, UpdateTime) VALUES (@UserID, @IPAdddress, @LoginTime, @UpdateTime)", online);
            }
            else
            {
                ExecuteUpdate<Online>(online, "UserID", "IPAdddress", "LoginTime", "UpdateTime");
                //DB.Execute("UPDATE Onlines SET IPAdddress = @IPAdddress, LoginTime = @LoginTime, UpdateTime = @UpdateTime WHERE UserID = @UserID", online);
            }


            // 记录本次更新时间
            Session[SK_ONLINE_UPDATE_TIME] = now;

        }

        /// <summary>
        /// 在线人数
        /// </summary>
        /// <returns></returns>
        protected int GetOnlineCount()
        {
            DateTime lastM = DateTime.Now.AddMinutes(-15);

            return DB.Execute("SELECT COUNT(*) FROM Onlines WHERE UpdateTime > @LastUpdateTime", new { LastUpdateTime = lastM });

        }

        #endregion

        #region 当前登录用户信息

        
        /// <summary>
        /// 当前登录用户名
        /// </summary>
        /// <returns></returns>
        protected string GetIdentityName()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return null;
            }

            var identityName = User.Identity.Name;
            var firstUnderlineIndex = identityName.IndexOf('_');
            return identityName.Substring(firstUnderlineIndex + 1);
        }

        /// <summary>
        /// 当前登录用户标识符
        /// </summary>
        /// <returns></returns>
        protected int? GetIdentityID()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return null;
            }

            var identityName = User.Identity.Name;
            var firstUnderlineIndex = identityName.IndexOf('_');
            if (firstUnderlineIndex > 0)
            {
                return Convert.ToInt32(identityName.Substring(0, firstUnderlineIndex));
            }

            return null;
        }

        // http://blog.163.com/zjlovety@126/blog/static/224186242010070024282/
        // http://www.cnblogs.com/gaoshuai/articles/1863231.html
        /// <summary>
        /// 当前登录用户的角色列表
        /// </summary>
        /// <returns></returns>
        protected List<int> GetIdentityRoleIDs()
        {
            List<int> roleIDs = new List<int>();

            if (User.Identity.IsAuthenticated)
            {
                FormsAuthenticationTicket ticket = ((FormsIdentity)User.Identity).Ticket;
                string userData = ticket.UserData;

                foreach (string roleID in userData.Split(','))
                {
                    if (!String.IsNullOrEmpty(roleID))
                    {
                        roleIDs.Add(Convert.ToInt32(roleID));
                    }
                }
            }

            return roleIDs;
        }


        /// <summary>
        /// 当前登录用户的角色列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetIdentityRoleNames()
        {
            List<string> roleNames = new List<string>();

            if (User.Identity.IsAuthenticated)
            {
                // 超级管理员拥有所有权限
                if (GetIdentityName() == "admin")
                {
                    return new List<string> { "超级管理员" };
                }
                else
                {
                    List<int> roleIDs = GetIdentityRoleIDs();

                    roleNames = DB.Query<string>("SELECT Roles.Name FROM Roles WHERE ID IN @RoleIDs", new { RoleIDs = roleIDs }).ToList();
                }
            }

            return roleNames;
        }


        /// <summary>
        /// 创建表单验证的票证并存储在客户端Cookie中
        /// </summary>
        /// <param name="userID">当前登录用户标识符</param>
        /// <param name="userName">当前登录用户名</param>
        /// <param name="roleIDs">当前登录用户的角色标识符列表</param>
        /// <param name="isPersistent">是否跨浏览器会话保存票证</param>
        /// <param name="expiration">过期时间</param>
        protected void CreateFormsAuthenticationTicket(int userID, string userName, string roleIDs, bool isPersistent, DateTime expiration)
        {
            // 创建Forms身份验证票据
            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1,
                userID + "_" + userName,        // 与票证关联的用户
                DateTime.Now,                   // 票证发出时间
                expiration,                     // 票证过期时间
                isPersistent,                   // 如果票证将存储在持久性 Cookie 中（跨浏览器会话保存），则为 true；否则为 false。
                roleIDs                         // 存储在票证中的用户特定的数据
             );

            // 对Forms身份验证票据进行加密，然后保存到客户端Cookie中
            string hashTicket = FormsAuthentication.Encrypt(ticket);
            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, hashTicket);
            cookie.HttpOnly = true;
            // 1. 关闭浏览器即删除（Session Cookie）：DateTime.MinValue
            // 2. 指定时间后删除：大于 DateTime.Now 的某个值
            // 3. 删除Cookie：小于 DateTime.Now 的某个值
            if (isPersistent)
            {
                cookie.Expires = expiration;
            }
            else
            {
                cookie.Expires = DateTime.MinValue;
            }
            Response.Cookies.Add(cookie);
        }

        #endregion

        #region 权限检查

        /// <summary>
        /// 检查当前用户是否拥有当前页面的浏览权限
        /// 页面需要先定义ViewPower属性，以确定页面与某个浏览权限的对应关系
        /// </summary>
        /// <returns></returns>
        protected bool CheckPowerView()
        {
            return CheckPower(ViewPower);
        }

        /// <summary>
        /// 检查当前用户是否拥有某个权限
        /// </summary>
        /// <param name="powerType"></param>
        /// <returns></returns>
        protected bool CheckPower(string powerName)
        {
            // 如果权限名为空，则放行
            if (String.IsNullOrEmpty(powerName))
            {
                return true;
            }

            // 当前登陆用户的权限列表
            List<string> rolePowerNames = GetRolePowerNames();
            if (rolePowerNames.Contains(powerName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前登录用户拥有的全部权限列表
        /// </summary>
        /// <param name="roleIDs"></param>
        /// <returns></returns>
        protected List<string> GetRolePowerNames()
        {
            // 将用户拥有的权限列表保存在Session中，这样就避免每个请求多次查询数据库
            if (Session["UserPowerList"] == null)
            {
                List<string> rolePowerNames = new List<string>();

                // 超级管理员拥有所有权限
                if (GetIdentityName() == "admin")
                {
                    rolePowerNames = DB.Query<string>("SELECT Name FROM Powers").ToList();

                }
                else
                {
                    List<int> roleIDs = GetIdentityRoleIDs();

                    rolePowerNames = DB.Query<string>("SELECT DISTINCT Powers.Name AS PowerName FROM Powers INNER JOIN RolePowers WHERE Powers.ID = RolePowers.PowerID and RolePowers.RoleID in @RoleIDs", new { RoleIDs = roleIDs }).ToList();

                }

                Session["UserPowerList"] = rolePowerNames;
            }
            return (List<string>)Session["UserPowerList"];
        }

        #endregion

        #region 权限相关

        protected void CheckPowerFailWithPage()
        {
            Response.Write(CHECK_POWER_FAIL_PAGE_MESSAGE);
            Response.End();
        }

        //protected void CheckPowerFailWithButton(FineUI.Pro.Button btn)
        //{
        //    btn.Enabled = false;
        //    btn.ToolTip = CHECK_POWER_FAIL_ACTION_MESSAGE;
        //}

        //protected void CheckPowerFailWithLinkButtonField(FineUI.Pro.Grid grid, string columnID)
        //{
        //    FineUI.Pro.LinkButtonField btn = grid.FindColumn(columnID) as FineUI.Pro.LinkButtonField;
        //    btn.Enabled = false;
        //    btn.ToolTip = CHECK_POWER_FAIL_ACTION_MESSAGE;
        //}

        //protected void CheckPowerFailWithWindowField(FineUI.Pro.Grid grid, string columnID)
        //{
        //    FineUI.Pro.WindowField btn = grid.FindColumn(columnID) as FineUI.Pro.WindowField;
        //    btn.Enabled = false;
        //    btn.ToolTip = CHECK_POWER_FAIL_ACTION_MESSAGE;
        //}

        protected void CheckPowerFailWithAlert()
        {
            PageContext.RegisterStartupScript(Alert.GetShowInTopReference(CHECK_POWER_FAIL_ACTION_MESSAGE));
        }

        //protected void CheckPowerWithButton(string powerName, FineUI.Pro.Button btn)
        //{
        //    if (!CheckPower(powerName))
        //    {
        //        CheckPowerFailWithButton(btn);
        //    }
        //}

        //protected void CheckPowerWithLinkButtonField(string powerName, FineUI.Pro.Grid grid, string columnID)
        //{
        //    if (!CheckPower(powerName))
        //    {
        //        CheckPowerFailWithLinkButtonField(grid, columnID);
        //    }
        //}

        //protected void CheckPowerWithWindowField(string powerName, FineUI.Pro.Grid grid, string columnID)
        //{
        //    if (!CheckPower(powerName))
        //    {
        //        CheckPowerFailWithWindowField(grid, columnID);
        //    }
        //}

        #endregion

        #region 产品版本

        public string GetProductVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return String.Format("{0}.{1}.{2}", v.Major, v.Minor, v.Build);
        }

        #endregion

        #region 日志记录

        protected void LogInfo(string message)
        {
            var log = new Log
            {
                Level = "Info",
                Message = message,
                LogTime = DateTime.Now
            };

            ExecuteInsert<Log>(log);
        }

        #endregion

        #region Dapper

        
        /// <summary>
        /// 获取实例的属性名称列表
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private string[] GetReflectionProperties(object instance)
        {
            var result = new List<string>();
            foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var propertyName = property.Name;
                // NotMapped特性
                var notMappedAttr = property.GetCustomAttribute<NotMappedAttribute>(false);
                if (notMappedAttr == null && propertyName != "ID")
                {
                    result.Add(propertyName);
                }
            }
            return result.ToArray();
        }


        /// <summary>
        /// 执行数据库更新操作
        /// </summary>
        /// <param name="instance">模型实例</param>
        /// <param name="fields">更新的表字段</param>
        /// <returns></returns>
        protected int ExecuteUpdate<T>(T instance, params string[] fields)
        {
            return ExecuteUpdate<T>(DB, instance, fields);
        }

        /// <summary>
        /// 执行数据库更新操作
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="instance"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        protected int ExecuteUpdate<T>(IDbConnection conn, T instance, params string[] fields)
        {
            // 约定：类型 User 对应的数据库表名 users
            string tableName = typeof(T).Name.ToLower() + "s";

            if (fields.Length == 0)
            {
                fields = GetReflectionProperties(instance);
            }

            var fieldsSql = String.Join(",", fields.Select(field => field + " = @" + field));

            var sql = String.Format("UPDATE {0} SET {1} WHERE ID = @ID", tableName, fieldsSql);

            return conn.Execute(sql, instance);
        }


        protected int ExecuteInsert<T>(T instance, params string[] fields)
        {
            return ExecuteInsert<T>(DB, instance, fields);
        }

        /// <summary>
        /// 执行数据库插入操作
        /// </summary>
        /// <param name="instance">模型实例</param>
        /// <param name="tableName">模型对应的表名</param>
        /// <param name="fields">插入的表字段</param>
        /// <returns>新插入的行ID</returns>
        protected int ExecuteInsert<T>(IDbConnection conn, T instance, params string[] fields)
        {
            // 约定：类型 User 对应的数据库表名 users
            string tableName = typeof(T).Name.ToLower() + "s";

            if (fields.Length == 0)
            {
                fields = GetReflectionProperties(instance);
            }

            var fieldsSql1 = String.Join(",", fields);
            var fieldsSql2 = String.Join(",", fields.Select(field => "@" + field));

            var sql = String.Format("INSERT {0} ({1}) VALUES ({2});", tableName, fieldsSql1, fieldsSql2);

            if (conn is MySqlConnection)
            {
                sql += "SELECT last_insert_id();";
            }
            else
            {
                sql += "SELECT @@IDENTITY;";
            }

            return conn.QuerySingle<int>(sql, instance);
        }


        /// <summary>
        /// 检索对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="paramID"></param>
        /// <returns></returns>
        protected T FindByID<T>(int paramID)
        {
            return FindByID<T>(DB, paramID);
        }

        /// <summary>
        /// 检索对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn"></param>
        /// <param name="paramID"></param>
        /// <returns></returns>
        protected T FindByID<T>(IDbConnection conn, int paramID)
        {
            // 约定：类型 User 对应的数据库表名 users
            var tableName = typeof(T).Name.ToLower() + "s";

            return conn.QuerySingleOrDefault<T>("SELECT * FROM "+ tableName +" WHERE ID = @ParamID", new { ParamID = paramID });
        }


        /// <summary>
        /// 获取用户信息（返回的数据中包含用户所属的部门信息）
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        protected User GetUserByID(int userID)
        {
            return DB.QuerySingleOrDefault<User>("SELECT Users.*, Depts.Name DeptName FROM Users LEFT JOIN Depts ON Users.DeptID = Depts.ID WHERE Users.ID = @UserID", new { UserID = userID });
        }


        #endregion

    }

}
