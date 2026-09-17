using System;
using System.Web;
using System.Web.Security;

using FineUI.Pro;
using System.Text;
using System.Linq;
using Dapper;
using System.Collections.Generic;


namespace FineUI.Pro.AppBox.Dapper
{
    public partial class _default : PageBase
    {
        #region Page_Load

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            // 如果用户已经登录，则重定向到管理首页
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect(FormsAuthentication.DefaultUrl);
            }

            Window1.Title = String.Format("FineUI.Pro.AppBox.Dapper v{0}", GetProductVersion());

        }

        #endregion

        #region Events

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            string userName = tbxUserName.Text.Trim();
            string password = tbxPassword.Text.Trim();

            User user = DB.QueryFirstOrDefault<User>("SELECT * FROM Users WHERE Name = @Name", new { Name = userName });

            if (user != null)
            {
                if (PasswordUtil.ComparePasswords(user.Password, password))
                {
                    if (!user.Enabled)
                    {
                        Alert.Show("用户未启用，请联系管理员！");
                    }
                    else
                    {
                        // 登录成功
                        LoginSuccess(user);
                    }
                }
                else
                {
                    Alert.Show("用户名或密码错误！");
                }

            }
            else
            {
                Alert.Show("用户名或密码错误！");
            }

        }


        private void LoginSuccess(User user)
        {
            RegisterOnlineUser(user);

            // 用户所属的角色字符串，以逗号分隔
            string roleIDs = String.Empty;
            var roleIdList = DB.Query<int>("SELECT RoleID FROM RoleUsers WHERE UserID = @UserID", new { UserID = user.ID });
            if (roleIdList.Count() > 0)
            {
                roleIDs = String.Join(",", roleIdList);
            }



            bool isPersistent = false;
            DateTime expiration = DateTime.Now.AddMinutes(120);
            CreateFormsAuthenticationTicket(user.ID, user.Name, roleIDs, isPersistent, expiration);

            // 重定向到登陆后首页
            Response.Redirect(FormsAuthentication.DefaultUrl);
        }


        #endregion
    }
}
