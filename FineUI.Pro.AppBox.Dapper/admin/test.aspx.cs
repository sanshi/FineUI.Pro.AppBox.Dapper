using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


using Dapper;

namespace FineUI.Pro.AppBox.Dapper.admin
{
    public partial class test : PageBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IEnumerable<User> users;
            using (var conn = GetDbConnection())
            {

                users = conn.Query<User>("SELECT * FROM Users");

                //var parameters = new List<DynamicParameters>();
                //foreach (var user in users)
                //{
                //    var p = new DynamicParameters();

                //    p.Add("Password", PasswordUtil.CreateDbPassword(user.Name));
                //    p.Add("ID", user.ID);

                //    q.AddParameter(p);
                //}

                //var affectedRowCount = conn.Execute("UPDATE users SET Password = @Password WHERE ID = @ID", parameters);

                //tbxUserName.Text = "更新的行数：" + affectedRowCount.ToString();

            }


        }
    }
}