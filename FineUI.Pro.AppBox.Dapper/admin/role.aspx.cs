using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;

using FineUI.Pro;

using Dapper;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class role : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreRoleView";
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
            var powerCoreRoleNew = CheckPower("CoreRoleNew");
            var powerCoreRoleEdit = CheckPower("CoreRoleEdit");
            var powerCoreRoleDelete = CheckPower("CoreRoleDelete");

            // 根据用户权限控制页面控件的可用状态
            btnNew.Enabled = powerCoreRoleNew;

            // 行内编辑按钮的权限
            Grid1.FindColumn("editField").Enabled = powerCoreRoleEdit;
            // 行内删除按钮的权限
            Grid1.FindColumn("deleteField").Enabled = powerCoreRoleDelete;




            
            // 每页记录数
            Grid1.PageSize = ConfigHelper.PageSize;

            
            BindGrid();
        }

        private void BindGrid()
        {
            // 查询条件
            var q = new QueryableBuilder<Role>();
            
            string searchText = ttbSearchMessage.Text.Trim();
            if (!String.IsNullOrEmpty(searchText))
            {
                q.AddWhere("Name like @SearchText");
                q.AddParameter("SearchText", "%" + searchText + "%");
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

        protected void Grid1_RowCommand(object sender, GridCommandEventArgs e)
        {
            int roleID = Convert.ToInt32(Grid1.SelectedRowID);

            if (e.CommandName == "Delete")
            {
                // 在操作之前进行权限检查
                if (!CheckPower("CoreRoleDelete"))
                {
                    CheckPowerFailWithAlert();
                    return;
                }

                int userCount = DB.QuerySingle<int>("SELECT COUNT(*) FROM RoleUsers WHERE RoleID = @RoleID", new { RoleID = roleID });
                if (userCount > 0)
                {
                    Alert.ShowInTop("删除失败！需要先清空属于此角色的用户！");
                    return;
                }

                DB.Execute("DELETE FROM roles WHERE ID = @RoleID", new { RoleID = roleID });


                BindGrid();
            }
        }

        protected void Window1_Close(object sender, EventArgs e)
        {
            BindGrid();
        }

        #endregion

    }
}
