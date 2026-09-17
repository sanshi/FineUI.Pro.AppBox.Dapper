using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;

using FineUI.Pro;
using System.Data;
using Newtonsoft.Json.Linq;
using AspNet = System.Web.UI.WebControls;
using Dapper;
using System.Transactions;


namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class role_power : PageBase
    {
        #region ViewPower

        /// <summary>
        /// 本页面的浏览权限，空字符串表示本页面不受权限控制
        /// </summary>
        public override string ViewPower
        {
            get
            {
                return "CoreRolePowerView";
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
            var powerCoreRolePowerEdit = CheckPower("CoreRolePowerEdit");

            // 根据用户权限控制页面控件的可用状态
            btnGroupUpdate.Enabled = powerCoreRolePowerEdit;


            // 每页记录数
            Grid1.PageSize = ConfigHelper.PageSize;
            BindGrid();

            // 默认选中第一个角色
            GridSelectionUtil.SelectFirstRow(Grid1);

            // 每页记录数
            Grid2.PageSize = ConfigHelper.PageSize;
            BindGrid2();
        }

        private void BindGrid()
        {
            // 全部的角色列表
            var q = new QueryableBuilder<Role>();
            var roles = q.Sort(Grid1);

            Grid1.DataSource = roles;
            Grid1.DataBind();
        }

        private Dictionary<string, bool> _currentRolePowers = new Dictionary<string, bool>();

        private void BindGrid2()
        {
            // 左侧表格选中的行
            if (String.IsNullOrEmpty(Grid1.SelectedRowID))
            {
                Grid2.DataSource = null;
                Grid2.DataBind();

                return;
            }

            var roleID = Convert.ToInt32(Grid1.SelectedRowID);

            // 当前选中角色拥有的权限列表
            _currentRolePowers.Clear();

            var powerNames = DB.Query<string>("SELECT Name FROM Powers INNER JOIN RolePowers ON Powers.ID = RolePowers.PowerID WHERE RolePowers.RoleID = @RoleID", new { RoleID = roleID });

            foreach (var power in powerNames)
            {
                if (!_currentRolePowers.ContainsKey(power))
                {
                    _currentRolePowers.Add(power, true);
                }
            }

            var sql = "SELECT * FROM Powers";
            if (Grid2.SortField == "GroupName")
            {
                sql += " ORDER BY GroupName " + (Grid2.SortDirection == "ASC" ? "asc" : "desc");
            }

            var powerList = new List<string>();
            var powerDictionary = new Dictionary<string, List<Power>>();
            foreach (var p in DB.Query<Power>(sql).ToList())
            {
                List<Power> powers;
                if (!powerDictionary.TryGetValue(p.GroupName, out powers))
                {
                    powerList.Add(p.GroupName);

                    powers = new List<Power>();
                    powerDictionary[p.GroupName] = powers;
                }

                powers.Add(p);
            }

            var powerData = powerList.Select(u => new GroupPowerViewModel
            {
                GroupName = u,
                Powers = powerDictionary[u]
            });

            Grid2.DataSource = powerData;
            Grid2.DataBind();


        }



        #endregion

        #region Grid1 Events

        protected void Grid1_Sort(object sender, GridSortEventArgs e)
        {
            BindGrid();

            // 默认选中第一个角色
            GridSelectionUtil.SelectFirstRow(Grid1);

            BindGrid2();
        }

        protected void Grid1_RowClick(object sender, FineUI.Pro.GridRowClickEventArgs e)
        {
            BindGrid2();
        }

        #endregion

        #region Grid2 Events

        protected void Grid2_RowDataBound(object sender, FineUI.Pro.GridRowEventArgs e)
        {
            AspNet.CheckBoxList ddlPowers = (AspNet.CheckBoxList)Grid2.Rows[e.RowIndex].FindControl("ddlPowers");

            var powers = e.DataItem as GroupPowerViewModel;

            foreach (Power power in powers.Powers)
            {
                AspNet.ListItem item = new AspNet.ListItem();
                item.Value = power.ID.ToString();
                // 权限标题是可编辑数据，ListItem.Text 会原样输出到 <label>，这里必须编码
                item.Text = HttpUtility.HtmlEncode(power.Title);
                item.Attributes["data-qtip"] = power.Name;

                if (_currentRolePowers.ContainsKey(power.Name))
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }

                ddlPowers.Items.Add(item);
            }
        }



        protected void Grid2_Sort(object sender, GridSortEventArgs e)
        {
            BindGrid2();
        }

        protected void btnGroupUpdate_Click(object sender, EventArgs e)
        {
            // 在操作之前进行权限检查
            if (!CheckPower("CoreRolePowerEdit"))
            {
                CheckPowerFailWithAlert();
                return;
            }

            if (String.IsNullOrEmpty(Grid1.SelectedRowID))
            {
                return;
            }
            var roleID = Convert.ToInt32(Grid1.SelectedRowID);


            // 当前角色新的权限列表
            List<int> newPowerIDs = new List<int>();
            for (int i = 0; i < Grid2.Rows.Count; i++)
            {
                AspNet.CheckBoxList ddlPowers = (AspNet.CheckBoxList)Grid2.Rows[i].FindControl("ddlPowers");
                foreach (AspNet.ListItem item in ddlPowers.Items)
                {
                    if (item.Selected)
                    {
                        newPowerIDs.Add(Convert.ToInt32(item.Value));
                    }
                }
            }


            using (var transactionScope = new TransactionScope())
            {
                DB.Execute("DELETE FROM rolepowers WHERE RoleID = @RoleID", new { RoleID = roleID });
                DB.Execute("INSERT rolepowers (RoleID, PowerID) VALUES (@RoleID, @PowerID)", newPowerIDs.Select(u => new { PowerID = u, RoleID = roleID }).ToList());

                transactionScope.Complete();
            }


            Alert.ShowInTop("当前角色的权限更新成功！");
        }


        #endregion

    }

}
