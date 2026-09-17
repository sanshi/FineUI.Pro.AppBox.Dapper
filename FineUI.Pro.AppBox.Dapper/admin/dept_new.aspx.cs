using Dapper;
using FineUI.Pro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class dept_new : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreDeptNew";
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

            // 绑定下拉树表格
            BindParentDDB();
        }
		
		private void BindParentDDB()
        {
            Grid1.DataSource = DB.Query<Dept>("SELECT * FROM Depts ORDER BY SortIndex ASC");
            Grid1.DataBind();
        }


        #endregion

        #region Events

        private void SaveItem()
        {
            Dept item = new Dept();
            item.Name = tbxName.Text.Trim();
            item.SortIndex = Convert.ToInt32(tbxSortIndex.Text.Trim());
            item.Remark = tbxRemark.Text.Trim();
			
			// 设置父部门
            if (!String.IsNullOrEmpty(ddbParent.Value))
            {
                int parentID = Convert.ToInt32(ddbParent.Value);
                item.ParentID = parentID;
            }
            else
            {
                item.ParentID = null;
            }
			
			

            ExecuteInsert<Dept>(item);
        }

        protected void btnSaveClose_Click(object sender, EventArgs e)
        {
            SaveItem();

            ActiveWindow.HidePostBack();
        }

        #endregion

    }
}
