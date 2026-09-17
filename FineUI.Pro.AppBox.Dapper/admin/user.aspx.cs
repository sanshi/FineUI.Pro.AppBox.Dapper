using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using FineUI.Pro;
using Newtonsoft.Json.Linq;
using System.Linq;


using Dapper;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class user : PageBase
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

        protected void Page_CustomEvent(object sender, CustomEventArgs e)
        {
            if (e.EventName == "Grid1_DeleteRows")
            {
                int[] rowIDs = e.EventArgumentsAsJObject.Value<JArray>("rowIDs").ToObject<int[]>();
                DeleteRows(rowIDs);
            }
            else if (e.EventName == "Grid1_EnableRows")
            {
                JObject eventArguments = e.EventArgumentsAsJObject;
                int[] rowIDs = eventArguments.Value<JArray>("rowIDs").ToObject<int[]>();
                SetSelectedUsersEnableStatus(eventArguments.Value<string>("action") == "enable", rowIDs);
            }
        }

        private void LoadData()
        {
            var powerCoreUserNew = CheckPower("CoreUserNew");
            var powerCoreUserEdit = CheckPower("CoreUserEdit");
            var powerCoreUserDelete = CheckPower("CoreUserDelete");
            var powerCoreUserChangePassword = CheckPower("CoreUserChangePassword");

            // 根据用户权限控制页面控件的可用状态
            btnNew.Enabled = powerCoreUserNew;
            btnChangeEnableUsers.Enabled = powerCoreUserEdit;
            btnDeleteSelected.Enabled = powerCoreUserDelete;


            // 行内编辑按钮的权限
            Grid1.FindColumn("editField").Enabled = powerCoreUserEdit;
            // 行内删除按钮的权限
            Grid1.FindColumn("deleteField").Enabled = powerCoreUserDelete;
            // 行内删除按钮的权限
            Grid1.FindColumn("changePasswordField").Enabled = powerCoreUserChangePassword;

            // 每页记录数
            Grid1.PageSize = ConfigHelper.PageSize;
            ddlGridPageSize.SelectedValue = ConfigHelper.PageSize.ToString();

            BindGrid();
        }

        private void BindGrid()
        {
            // 查询条件
            var q = new QueryableBuilder<User>();
            
            string searchText = ttbSearchMessage.Text.Trim();
            if (!String.IsNullOrEmpty(searchText))
            {
                q.AddWhere("(Users.Name like @SearchText or Users.ChineseName like @SearchText or Users.EnglishName like @SearchText)");
                q.AddParameter("SearchText", "%" + searchText + "%");
            }

            if (GetIdentityName() != "admin")
            {
                q.AddWhere("Users.Name != 'admin'");
            }

            // 过滤启用状态
            if (rblEnableStatus.SelectedValue != "all")
            {
                q.AddWhere("Users.Enabled = " + (rblEnableStatus.SelectedValue == "enabled" ? 1 : 0));
            }


            // 获取总记录数（在添加条件之后，排序和分页之前）
            Grid1.RecordCount = q.Count();

            // 排列和数据库分页
            Grid1.DataSource = q.SortAndPage(Grid1);
            Grid1.DataBind();

        }

        #endregion

        #region Events

        protected void ttbSearchMessage_Trigger2Click(object sender, EventArgs e)
        {
            ttbSearchMessage.ShowTrigger1 = true;
            BindGrid();
        }

        protected void ttbSearchMessage_Trigger1Click(object sender, EventArgs e)
        {
            ttbSearchMessage.Text = String.Empty;
            ttbSearchMessage.ShowTrigger1 = false;
            BindGrid();
        }

        protected void Grid1_Sort(object sender, GridSortEventArgs e)
        {
            BindGrid();
        }

        protected void Grid1_PageIndexChange(object sender, GridPageEventArgs e)
        {
            BindGrid();
        }


        // 选中行的标识由客户端随自定义回发送上来
        private void SetSelectedUsersEnableStatus(bool enabled, int[] rowIDs)
        {
            // 在操作之前进行权限检查
            if (!CheckPower("CoreUserEdit"))
            {
                CheckPowerFailWithAlert();
                return;
            }

            // 执行数据库操作
            // 列表里看不到 admin，但回传的主键不可信：超级管理员不能被禁用
            if (DB.ExecuteScalar<int>("SELECT COUNT(*) FROM Users WHERE ID in @IDs AND Name = 'admin'", new { IDs = rowIDs }) > 0)
            {
                Alert.ShowInTop("不能修改超级管理员（admin）的启用状态！");
                return;
            }

            DB.Execute("UPDATE Users SET Enabled = " + (enabled ? 1 : 0) + " WHERE ID in @IDs", new { IDs = rowIDs });


            // 重新绑定表格
            BindGrid();
        }

        protected void Grid1_RowCommand(object sender, GridCommandEventArgs e)
        {
            int userID = Convert.ToInt32(Grid1.SelectedRowID);
            
            if (e.CommandName == "Delete")
            {
                DeleteRows(new int[] { userID });
            }
        }

        protected void Window1_Close(object sender, EventArgs e)
        {
            BindGrid();
        }

        protected void rblEnableStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid();
        }


        protected void ddlGridPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            Grid1.PageSize = Convert.ToInt32(ddlGridPageSize.SelectedValue);

            BindGrid();
        }

        #endregion

        #region DeleteRows

        private void DeleteRows(int[] rowIDs)
        {
            if (!CheckPower("CoreUserDelete"))
            {
                CheckPowerFailWithAlert();
                return;
            }

            var usersToDelete = DB.Query<User>("SELECT * FROM Users WHERE ID IN @IDs", new { IDs = rowIDs });

            if (usersToDelete.Any(u => u.Name == "admin"))
            {
                Alert.ShowInTop("不能删除超级管理员（admin）！");
                return;
            }


            DB.Execute("DELETE FROM Users WHERE ID IN @IDs", new { IDs = rowIDs });

            BindGrid();
        }

        #endregion
    }
}
