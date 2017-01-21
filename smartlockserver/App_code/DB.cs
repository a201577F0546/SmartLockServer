using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
namespace smartlockserver
{
    class DB//用来封装一些连接数据库的操作
    {
        public static SqlConnection Camcon()
        {
            string constr = "Data Source = DESKTOP-1T6S6EO\\SQLEXPRESS; Initial Catalog=MainDatabase;Persist Security Info=True;User ID=sa;Password=s321SQL1234";
            return new SqlConnection(constr);
        }
    }
}
