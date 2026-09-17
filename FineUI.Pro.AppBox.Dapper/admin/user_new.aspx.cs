using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;

using FineUI.Pro;
using System.Transactions;
using Dapper;


namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class user_new : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreUserNew";
            }
        }

        #endregion

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

            // 初始化用户所属角色
            InitUserRole();

            // 初始化用户所属部门
            InitUserDept();

            // 初始化用户所属职称
            InitUserTitle();
        }

        #region InitUserDept

        private void InitUserDept()
        {
            gridDept.DataSource = DB.Query<Dept>("SELECT * FROM Depts ORDER BY SortIndex ASC");
            gridDept.DataBind();

        }

        #endregion

        #region InitUserRole

        private void InitUserRole()
        {
            cblRoles.DataSource = DB.Query<Role>("SELECT * FROM Roles");
            cblRoles.DataBind();

        }
        #endregion

        #region InitUserTitle

        private void InitUserTitle()
        {
            cblTitles.DataSource = DB.Query<Title>("SELECT * FROM Titles");
            cblTitles.DataBind();
        }

        #endregion

        #endregion

        #region Events


        protected void btnSaveClose_Click(object sender, EventArgs e)
        {
            string inputUserName = tbxName.Text.Trim();

            User user = DB.QuerySingleOrDefault("SELECT * FROM Users WHERE Name = @UserName", new { UserName = inputUserName });

            if (user != null)
            {
                Alert.Show("用户 " + inputUserName + " 已经存在！");
                return;
            }

            User item = new User();
            item.Name = tbxName.Text.Trim();
            item.Password = PasswordUtil.CreateDbPassword(tbxPassword.Text.Trim());
            item.ChineseName = tbxRealName.Text.Trim();
            item.Gender = ddlGender.SelectedValue;
            item.CompanyEmail = tbxCompanyEmail.Text.Trim();
            item.Email = tbxEmail.Text.Trim();
            item.OfficePhone = tbxOfficePhone.Text.Trim();
            item.OfficePhoneExt = tbxOfficePhoneExt.Text.Trim();
            item.HomePhone = tbxHomePhone.Text.Trim();
            item.CellPhone = tbxCellPhone.Text.Trim();
            item.Remark = tbxRemark.Text.Trim();
            item.Enabled = cbxEnabled.Checked;
            item.CreateTime = DateTime.Now;


            // 添加部门
            if (!String.IsNullOrEmpty(ddbDept.Value))
            {
                item.DeptID = Convert.ToInt32(ddbDept.Value);
            }


            using (var transactionScope = new TransactionScope())
            {
                // 插入用户
                var userID = ExecuteInsert<User>(DB, item);

                // 更新用户所属角色
                int[] roleIDs = ddbRoles.Values.Select(u => Convert.ToInt32(u)).ToArray();
                DB.Execute("DELETE FROM RoleUsers WHERE UserID = @UserID", new { UserID = userID });
                DB.Execute("INSERT RoleUsers (UserID, RoleID) VALUES (@UserID, @RoleID)", roleIDs.Select(u => new { UserID = userID, RoleID = u }).ToList());

                // 更新用户所属职称
                int[] titleIDs = ddbTitles.Values.Select(u => Convert.ToInt32(u)).ToArray();
                DB.Execute("DELETE FROM TitleUsers WHERE UserID = @UserID", new { UserID = userID });
                DB.Execute("INSERT TitleUsers (UserID, TitleID) VALUES (@UserID, @TitleID)", titleIDs.Select(u => new { UserID = userID, TitleID = u }).ToList());


                transactionScope.Complete();
            }

            // 关闭本窗体（触发窗体的关闭事件）
            ActiveWindow.HidePostBack();
        }
        #endregion

    }
}
