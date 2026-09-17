using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;

using FineUI.Pro;
using Newtonsoft.Json.Linq;
using Dapper;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class title_user : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreTitleUserView";
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
            if (e.EventName == "Grid2_DeleteRows")
            {
                int[] rowIDs = e.EventArgumentsAsJObject.Value<JArray>("rowIDs").ToObject<int[]>();
                DeleteRows(rowIDs);
            }
        }

        private void LoadData()
        {
            var powerCoreTitleUserNew = CheckPower("CoreTitleUserNew");
            var powerCoreTitleUserDelete = CheckPower("CoreTitleUserDelete");

            // 根据用户权限控制页面控件的可用状态
            btnNew.Enabled = powerCoreTitleUserNew;
            btnDeleteSelected.Enabled = powerCoreTitleUserDelete;

            // 行内删除按钮的权限
            Grid2.FindColumn("deleteField").Enabled = powerCoreTitleUserDelete;



            BindGrid1();

            // 默认选中第一个职称
            GridSelectionUtil.SelectFirstRow(Grid1);

            // 每页记录数
            Grid2.PageSize = ConfigHelper.PageSize;
            ddlGridPageSize.SelectedValue = ConfigHelper.PageSize.ToString();

            BindGrid2();
        }

        private void BindGrid1()
        {
            // 全部的职称列表
            var q = new QueryableBuilder<Title>();
            var titles = q.Sort(Grid1);

            Grid1.DataSource = titles;
            Grid1.DataBind();
        }

        private void BindGrid2()
        {
            // 左侧表格选中的行
            if (String.IsNullOrEmpty(Grid1.SelectedRowID))
            {
                Grid2.DataSource = null;
                Grid2.DataBind();

                return;
            }

            var titleID = Convert.ToInt32(Grid1.SelectedRowID);

            // 查询条件
            var q = new QueryableBuilder<User>();


            string searchText = ttbSearchMessage.Text.Trim();
            if (!String.IsNullOrEmpty(searchText))
            {
                q.AddWhere("(Users.Name like @SearchText or Users.ChineseName like @SearchText)");
                q.AddParameter("SearchText", "%" + searchText + "%");
            }

            q.AddWhere("Users.Name != 'admin'");

            q.AddWhere("TitleUsers.TitleID = @TitleID");
            q.AddParameter("TitleID", titleID);

            q.FromSql = "Users INNER JOIN TitleUsers ON Users.ID = TitleUsers.UserID";

            // 获取总记录数（在添加条件之后，排序和分页之前）
            Grid2.RecordCount = q.Count();

            // 排列和数据库分页
            Grid2.DataSource = q.SortAndPage(Grid2);
            Grid2.DataBind();

        }


        #endregion

        #region Events

        protected void ddlGridPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            Grid2.PageSize = Convert.ToInt32(ddlGridPageSize.SelectedValue);

            BindGrid2();
        }


        #endregion

        #region Grid1 Events

        protected void Grid1_Sort(object sender, GridSortEventArgs e)
        {
            BindGrid1();

            // 默认选中第一个职称
            GridSelectionUtil.SelectFirstRow(Grid1);

            BindGrid2();
        }

        protected void Grid1_RowClick(object sender, FineUI.Pro.GridRowClickEventArgs e)
        {
            BindGrid2();
        }

        #endregion

        #region Grid2 Events

        protected void ttbSearchMessage_Trigger2Click(object sender, EventArgs e)
        {
            ttbSearchMessage.ShowTrigger1 = true;
            BindGrid2();
        }

        protected void ttbSearchMessage_Trigger1Click(object sender, EventArgs e)
        {
            ttbSearchMessage.Text = String.Empty;
            ttbSearchMessage.ShowTrigger1 = false;
            BindGrid2();
        }

        protected void Grid2_Sort(object sender, GridSortEventArgs e)
        {
            BindGrid2();
        }

        protected void Grid2_PageIndexChange(object sender, GridPageEventArgs e)
        {
            BindGrid2();
        }


        protected void Grid2_RowCommand(object sender, GridCommandEventArgs e)
        {
            if (e.CommandName == "Delete")
            {
                var rowID = Convert.ToInt32(e.RowID);

                DeleteRows(new int[] { rowID });
            }
        }

        protected void Window1_Close(object sender, EventArgs e)
        {
            BindGrid2();
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            int titleID = Convert.ToInt32(Grid1.SelectedRowID);
            string addUrl = String.Format("~/admin/title_user_addnew.aspx?id={0}", titleID);

            PageContext.RegisterStartupScript(Window1.GetShowReference(addUrl, "添加用户到当前职称"));
        }

        #endregion

        #region DeleteRows

        private void DeleteRows(int[] rowIDs)
        {
            // 左侧表格选中的行
            if (String.IsNullOrEmpty(Grid1.SelectedRowID))
            {
                return;
            }
            var titleID = Convert.ToInt32(Grid1.SelectedRowID);


            // 在操作之前进行权限检查
            if (!CheckPower("CoreTitleUserDelete"))
            {
                CheckPowerFailWithAlert();
                return;
            }

            // 执行数据库操作
            DB.Execute("DELETE FROM TitleUsers WHERE TitleID = @TitleID AND UserID IN @UserIDs", new { TitleID = titleID, UserIDs = rowIDs });


            // 重新绑定表格
            BindGrid2();
        }



        #endregion

    }
}
