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
    public partial class menu : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreMenuView";
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
            Grid1.FindCommand("Edit").Enabled = CheckPower("CoreMenuEdit");
            Grid1.FindCommand("Delete").Enabled = CheckPower("CoreMenuDelete");
        }

        private void LoadData()
        {
            var powerCoreMenuNew = CheckPower("CoreMenuNew");

            // 根据用户权限控制页面控件的可用状态
            btnNew.Enabled = powerCoreMenuNew;







            BindGrid();
        }

        private void BindGrid()
        {
            Grid1.DataSource = DB.Query<Menu>("SELECT * FROM Menus ORDER BY SortIndex ASC");
            Grid1.DataBind();
        }


        #endregion

        #region Events


        protected void Grid1_RowCommand(object sender, GridCommandEventArgs e)
        {
            int menuID = Convert.ToInt32(Grid1.SelectedRowID);

            if (e.CommandName == "Delete")
            {
                // 在操作之前进行权限检查
                if (!CheckPower("CoreMenuDelete"))
                {
                    CheckPowerFailWithAlert();
                    return;
                }

                int childCount = DB.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM menus WHERE ParentID = @ParentID",
                    new { ParentID = menuID });

                if (childCount > 0)
                {
                    Alert.ShowInTop("删除失败！请先删除子菜单！");
                    return;
                }

                // 从数据库中删除
                DB.Execute("DELETE FROM menus WHERE ID = @ID", new { ID = menuID });

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
