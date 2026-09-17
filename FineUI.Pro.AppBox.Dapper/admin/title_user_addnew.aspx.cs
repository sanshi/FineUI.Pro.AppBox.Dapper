using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;

using FineUI.Pro;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Dapper;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class title_user_addnew : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreTitleUserNew";
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
            Title current = FindByID<Title>(id);
            if (current == null)
            {
                // 参数错误，首先弹出Alert对话框然后关闭弹出窗口
                Alert.Show("参数错误！", String.Empty, ActiveWindow.GetHideReference());
                return;
            }

            // 每页记录数
            Grid1.PageSize = ConfigHelper.PageSize;
            ddlGridPageSize.SelectedValue = ConfigHelper.PageSize.ToString();


            BindGrid();
        }


        private void BindGrid()
        {
            int titleID = GetQueryIntValue("id");

            // 查询条件
            var q = new QueryableBuilder<User>();
            
            string searchText = ttbSearchMessage.Text.Trim();
            if (!String.IsNullOrEmpty(searchText))
            {
                q.AddWhere("(Users.Name like @SearchText or Users.ChineseName like @SearchText or Users.EnglishName like @SearchText)");
                q.AddParameter("SearchText", "%" + searchText + "%");
            }

            q.AddWhere("Users.Name != 'admin'");

            // 排除已经属于本职称的用户
            q.AddWhere("Users.ID not in (SELECT ID FROM Users INNER JOIN TitleUsers ON Users.ID = TitleUsers.UserID WHERE TitleUsers.TitleID = @TitleID)");
            q.AddParameter("TitleID", titleID);


            // 获取总记录数（在添加条件之后，排序和分页之前）
            Grid1.RecordCount = q.Count();

            // 排列和数据库分页
            Grid1.DataSource = q.SortAndPage(Grid1);
            Grid1.DataBind();

        }

        #endregion

        #region Events

        protected void btnSaveClose_Click(object sender, EventArgs e)
        {
            int titleID = GetQueryIntValue("id");
			
			// 选中的用户ID列表（跨页保持选中行）
			var selectedRowIDs = Grid1.SelectedRowIDArray.Select(u => Convert.ToInt32(u)).ToArray();
			if (selectedRowIDs.Length == 0)
			{
				Alert.Show("请至少选择一项！");
				return;
			}

            DB.Execute("INSERT TitleUsers (UserID, TitleID) VALUES (@UserID, @TitleID)", selectedRowIDs.Select(u => new { UserID = u, TitleID = titleID }).ToList());



            ActiveWindow.HidePostBack();
        }




        

        protected void Grid1_Sort(object sender, GridSortEventArgs e)
        {
            BindGrid();
        }

        protected void Grid1_PageIndexChange(object sender, GridPageEventArgs e)
        {
            BindGrid();
        }


        protected void ddlGridPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 设置每页显示的项数
            Grid1.PageSize = Convert.ToInt32(ddlGridPageSize.SelectedValue);

            BindGrid();
        }
		
		protected void ttbSearchMessage_Trigger1Click(object sender, EventArgs e)
        {
            ttbSearchMessage.Text = String.Empty;
            ttbSearchMessage.ShowTrigger1 = false;
            BindGrid();
        }
		
		protected void ttbSearchMessage_Trigger2Click(object sender, EventArgs e)
        {
            ttbSearchMessage.ShowTrigger1 = true;
            BindGrid();
        }

        

        #endregion


    }
}
