
namespace ET
{
    public class Fiber
    {
        public ILog Log { get; }
        
        internal Fiber(int id, string name)
        {
             this.Log = new NLogger(name, 0, id, "./Config/NLog/NLog.config");
        }
    }
}