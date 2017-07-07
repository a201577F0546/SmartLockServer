using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
namespace smartlockserver
{
    class DB//用来封装一些连接数据库的操作
    {
        private int i ;
        public static SqlConnection GetCon()
        {
            string constr = ConfigurationManager.AppSettings["constring"];;
            return new SqlConnection(constr);
        }
        /// <summary>
        /// 执行SQL查询语句
        /// </summary>
        /// <param name="str">查询语句</param>
        /// <returns>返回SqlDataReader对象dr</returns>
        public SqlDataReader reDr(string str)
        {
            SqlConnection conn = GetCon();//连接数据库
            conn.Open();//并打开了连接
            SqlCommand com = new SqlCommand(str, conn);
            SqlDataReader dr = com.ExecuteReader(CommandBehavior.CloseConnection);
            return dr;//返回SqlDataReader对象dr
        }
        public int sqlEx(string cmdstr)
        {
            SqlConnection con = GetCon();//连接数据库
            con.Open();//打开连接
            SqlCommand cmd = new SqlCommand(cmdstr, con);
            try
            {
                i= cmd.ExecuteNonQuery();//执行SQL 语句并返回受影响的行数
                return 1;//成功返回１
            }
            catch (Exception e)
            {
                return i;//失败返回０
            }
            finally
            {
                con.Dispose();//释放连接对象资源
            }
        }
    }
}
