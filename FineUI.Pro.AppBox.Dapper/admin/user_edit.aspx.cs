using System;

using System.Linq;
using FineUI.Pro;
using Dapper;
using System.Transactions;
using System.Collections.Generic;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class user_edit : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreUserEdit";
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

        public User CurrentUser { get; set; }

        private void LoadData()
        {

            int id = GetQueryIntValue("id");
            User current = GetUserByID(id);
            if (current == null)
            {
                // 参数错误，首先弹出Alert对话框然后关闭弹出窗口
                Alert.Show("参数错误！", String.Empty, ActiveWindow.GetHideReference());
                return;
            }

            if (current.Name == "admin" && GetIdentityName() != "admin")
            {
                Alert.Show("你无权编辑超级管理员！", String.Empty, ActiveWindow.GetHideReference());
                return;
            }

            // 将当前用户保存到属性中，供后续代码使用
            CurrentUser = current;


            labName.Text = current.Name;
            tbxRealName.Text = current.ChineseName;
            tbxCompanyEmail.Text = current.CompanyEmail;
            tbxEmail.Text = current.Email;
            tbxCellPhone.Text = current.CellPhone;
            tbxOfficePhone.Text = current.OfficePhone;
            tbxOfficePhoneExt.Text = current.OfficePhoneExt;
            tbxHomePhone.Text = current.HomePhone;
            tbxRemark.Text = current.Remark;
            cbxEnabled.Checked = current.Enabled;
            ddlGender.SelectedValue = current.Gender;

            // 初始化用户所属角色
            InitUserRole();

            // 初始化用户所属部门
            InitUserDept();

            // 初始化用户所属职称
            InitUserTitle();
        }

        #endregion

        #region InitUserDept

        private void InitUserDept()
        {
            // 用户所属部门
            if (CurrentUser.DeptID != null)
            {
                ddbDept.Value = CurrentUser.DeptID.ToString();
                ddbDept.Text = CurrentUser.DeptName;
            }

            gridDept.DataSource = DB.Query<Dept>("SELECT * FROM Depts ORDER BY SortIndex ASC");
            gridDept.DataBind();
        }

        #endregion

        #region InitUserRole

        private void InitUserRole()
        {
            // 用户所属角色
            var roles = DB.Query<Role>("SELECT * FROM Roles INNER JOIN RoleUsers ON Roles.ID = RoleUsers.RoleID WHERE RoleUsers.UserID = @UserID", new { UserID = CurrentUser.ID });
            if (roles.Count() > 0)
            {
                ddbRoles.Values = roles.Select(u => u.ID.ToString()).ToArray();
                ddbRoles.Texts = roles.Select(u => u.Name).ToArray();
            }

            cblRoles.DataSource = DB.Query<Role>("SELECT * FROM Roles");
            cblRoles.DataBind();
        }
        #endregion

        #region InitUserTitle

        private void InitUserTitle()
        {
            // 用户拥有职称
            var titles = DB.Query<Title>("SELECT * FROM Titles INNER JOIN TitleUsers ON Titles.ID = TitleUsers.TitleID WHERE TitleUsers.UserID = @UserID", new { UserID = CurrentUser.ID });
            if (titles.Count() > 0)
            {
                ddbTitles.Values = titles.Select(u => u.ID.ToString()).ToArray();
                ddbTitles.Texts = titles.Select(u => u.Name).ToArray();
            }

            cblTitles.DataSource = DB.Query<Title>("SELECT * FROM Titles");
            cblTitles.DataBind();
        }

        #endregion

        #region Events

        protected void btnSaveClose_Click(object sender, EventArgs e)
        {
            int userID = GetQueryIntValue("id");

            User item = GetUserByID(userID);
            item.ChineseName = tbxRealName.Text.Trim();
            item.Gender = ddlGender.SelectedValue;
            item.CompanyEmail = tbxCompanyEmail.Text.Trim();
            item.Email = tbxEmail.Text.Trim();
            item.CellPhone = tbxCellPhone.Text.Trim();
            item.OfficePhone = tbxOfficePhone.Text.Trim();
            item.OfficePhoneExt = tbxOfficePhoneExt.Text.Trim();
            item.HomePhone = tbxHomePhone.Text.Trim();
            item.Remark = tbxRemark.Text.Trim();
            item.Enabled = cbxEnabled.Checked;



            // 如果选择了部门，则更新部门ID，否则设置为null
            if (!String.IsNullOrEmpty(ddbDept.Value))
            {
                item.DeptID = Convert.ToInt32(ddbDept.Value);
            }
            else
            {
                item.DeptID = null;
            }


            using (var transactionScope = new TransactionScope())
            {
                // 更新用户
                ExecuteUpdate<User>(DB, item);

                // 更新用户所属的角色
                int[] roleIDs = ddbRoles.Values.Select(r => Convert.ToInt32(r)).ToArray();
                DB.Execute("DELETE FROM RoleUsers WHERE UserID = @UserID", new { UserID = userID });
                DB.Execute("INSERT RoleUsers (UserID, RoleID) VALUES (@UserID, @RoleID)", roleIDs.Select(u => new { UserID = userID, RoleID = u }).ToList());

                // 更新用户拥有的职称
                int[] titleIDs = ddbTitles.Values.Select(r => Convert.ToInt32(r)).ToArray();
                DB.Execute("DELETE FROM TitleUsers WHERE UserID = @UserID", new { UserID = userID });
                DB.Execute("INSERT TitleUsers (UserID, TitleID) VALUES (@UserID, @TitleID)", titleIDs.Select(u => new { UserID = userID, TitleID = u }).ToList());


                transactionScope.Complete();
            }

            ActiveWindow.HidePostBack();
        }

        #endregion

    }
}
