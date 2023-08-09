// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");
// using NLog;
var logger=new ET.Logger();
logger.Log=new ET.NLogger("DEMO", 0, -1, "./Config/NLog/NLog.config");
logger.Register();
ET.Options options=new ET.Options();
options.Console=1;
options.LogLevel=0;
options.Register();

var Reader=new MiniAsync.MiniTask.MiniTask.FileReadOnlyManager();
Reader.Register();

var Timer=new MiniAsync.MiniTask.MiniTask.TimerManager();

Timer.Register();

var context=new ET.ThreadSynchronizationContext();
SynchronizationContext.SetSynchronizationContext(context);


async void  TestRead(string path)
{
   await Reader.ReadFile(path);
   var thread=Thread.CurrentThread.ManagedThreadId;
   ET.Log.Console($"Continue末 线程:{thread}");
}


async void TestTimer(int miliseconds)
{
   await Timer.Delay(miliseconds);
   var thread=Thread.CurrentThread.ManagedThreadId;
   ET.Log.Console($"计时器回调 Continue末 线程:{thread}");
}

try
{
    ET.Log.Console("test");
    // TestRead("./TestObj/Test.dll");
    TestTimer(10000);
    Timer.Init();
    while(Console.ReadLine()!="exit")
    {
        ET.Log.Console("Begin context handling");
        Timer.Update();
        context.Update();
        ET.Log.Console("End context handling");
    }
}
finally{
logger.Dispose();
options.Dispose();
Reader.Dispose();
Timer.Dispose();
}
//dispose
