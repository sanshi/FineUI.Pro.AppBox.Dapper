using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using FineUI.Pro;
using System.Text;
using System.Linq;

using Dapper;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class user_view : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreUserView";
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

            int id = GetQueryIntValue("id");

            User current = GetUserByID(id);

            if (current == null)
            {
                // 参数错误，首先弹出Alert对话框然后关闭弹出窗口
                Alert.Show("参数错误！", String.Empty, ActiveWindow.GetHideReference());
                return;
            }

            labName.Text = current.Name;
            labRealName.Text = current.ChineseName;
            labCompanyEmail.Text = current.CompanyEmail;
            labEmail.Text = current.Email;
            labCellPhone.Text = current.CellPhone;
            labOfficePhone.Text = current.OfficePhone;
            labOfficePhoneExt.Text = current.OfficePhoneExt;
            labHomePhone.Text = current.HomePhone;
            labRemark.Text = current.Remark;
            labEnabled.Text = current.Enabled ? "启用" : "禁用";
            labGender.Text = current.Gender;

            // 用户所属角色
            var roleNames = DB.Query<string>("SELECT roles.Name FROM RoleUsers LEFT JOIN roles ON RoleUsers.RoleID = roles.ID WHERE RoleUsers.UserID = @UserID", new { UserID = id });
            labRole.Text = String.Join(",", roleNames.ToArray());


            // 用户的职称列表
            var titleNames = DB.Query<string>("SELECT Titles.Name FROM TitleUsers LEFT JOIN Titles ON TitleUsers.TitleID = Titles.ID WHERE TitleUsers.UserID = @UserID", new { UserID = id });
            labTitle.Text = String.Join(",", titleNames.ToArray());


            // 用户所属的部门
            if (current.DeptID != null)
            {
                labDept.Text = current.DeptName;
            }

        }

        #endregion

    }
}
