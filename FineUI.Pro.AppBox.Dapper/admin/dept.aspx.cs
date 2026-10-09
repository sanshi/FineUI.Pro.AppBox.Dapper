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
    public partial class dept : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreDeptView";
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
            Grid1.FindCommand("Edit").Enabled = CheckPower("CoreDeptEdit");
            Grid1.FindCommand("Delete").Enabled = CheckPower("CoreDeptDelete");
        }

        private void LoadData()
        {
            var powerCoreDeptNew = CheckPower("CoreDeptNew");

            // 根据用户权限控制页面控件的可用状态
            btnNew.Enabled = powerCoreDeptNew;




            BindGrid();
        }

        private void BindGrid()
        {
            Grid1.DataSource = DB.Query<Dept>("SELECT * FROM Depts ORDER BY SortIndex ASC");
            Grid1.DataBind();
        }

        #endregion

        #region Events

        protected void Grid1_RowCommand(object sender, GridCommandEventArgs e)
        {
            int deptID = Convert.ToInt32(e.RowID);

            if (e.CommandName == "Delete")
            {
                // 在操作之前进行权限检查
                if (!CheckPower("CoreDeptDelete"))
                {
                    CheckPowerFailWithAlert();
                    return;
                }

                int userCount = DB.QuerySingle<int>("SELECT COUNT(*) FROM Users WHERE DeptID = @DeptID", new { DeptID = deptID });
                if (userCount > 0)
                {
                    Alert.ShowInTop("删除失败！需要先清空属于此部门的用户！");
                    return;
                }

                int childCount = DB.QuerySingle<int>("SELECT COUNT(*) FROM Depts WHERE ParentID = @DeptID", new { DeptID = deptID });
                if (childCount > 0)
                {
                    Alert.ShowInTop("删除失败！请先删除子部门！");
                    return;
                }

                DB.Execute("DELETE FROM Depts WHERE ID = @DeptID", new { DeptID = deptID });

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
