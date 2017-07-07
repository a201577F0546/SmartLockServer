using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
namespace smartlockserver
{
    class Heartbeat
    {
        public  Heartbeat() {
            Timer t = new Timer(10000);
            t.Elapsed += new ElapsedEventHandler(theout);//到达时间的时候执行事件；
            t.AutoReset = true;//设置是执行一次（false）还是一直执行(true)；
            t.Enabled = true;//是否执行System.Timers.Timer.Elapsed事件；
        }
       
        

    public void theout(object source, ElapsedEventArgs e)

        {
           

        }
    }
}
