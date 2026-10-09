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
    public partial class power : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CorePowerView";
            }
        }

        #endregion

        #region Page_Load

        protected void Page_Load(object sender, EventArgs e)
        {
            ApplyCommandPermissions();

            if (!IsPostBack)
            {
                LoadData();
            }
        }

        // 行命令的启用状态不随 ViewState 保存；每次请求重新检查，使伪造的禁用命令也在操作前被拒绝。
        private void ApplyCommandPermissions()
        {
            Grid1.FindCommand("Edit").Enabled = CheckPower("CorePowerEdit");
            Grid1.FindCommand("Delete").Enabled = CheckPower("CorePowerDelete");
        }

        private void LoadData()
        {
            var powerCorePowerNew = CheckPower("CorePowerNew");

            // 根据用户权限控制页面控件的可用状态
            btnNew.Enabled = powerCorePowerNew;







            // 每页记录数
            Grid1.PageSize = ConfigHelper.PageSize;
            ddlGridPageSize.SelectedValue = ConfigHelper.PageSize.ToString();


            BindGrid();
        }

        private void BindGrid()
        {
            // 查询条件
            var q = new QueryableBuilder<Power>();

            string searchText = ttbSearchMessage.Text.Trim();
            if (!String.IsNullOrEmpty(searchText))
            {
                q.AddWhere("(Powers.Name like @SearchText or Powers.Title like @SearchText)");
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
            int powerID = Convert.ToInt32(Grid1.SelectedRowID);

            if (e.CommandName == "Delete")
            {
                // 在操作之前进行权限检查
                if (!CheckPower("CorePowerDelete"))
                {
                    CheckPowerFailWithAlert();
                    return;
                }

                int roleCount = DB.QuerySingle<int>("SELECT COUNT(*) FROM rolepowers WHERE PowerID = @PowerID", new { PowerID = powerID });
                if (roleCount > 0)
                {
                    Alert.ShowInTop("删除失败！需要先清空使用此权限的角色！");
                    return;
                }

                DB.Execute("DELETE FROM Powers WHERE ID = @PowerID", new { PowerID = powerID });


                BindGrid();
            }
        }


        protected void Window1_Close(object sender, EventArgs e)
        {
            BindGrid();
        }

        protected void ddlGridPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            Grid1.PageSize = Convert.ToInt32(ddlGridPageSize.SelectedValue);

            BindGrid();
        }

        #endregion

    }
}
