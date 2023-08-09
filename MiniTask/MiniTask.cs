using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.IO.Pipelines;
using System.Buffers;
using System.Collections.Concurrent;
namespace MiniAsync.MiniTask
{
    public class MiniTask
    {
        public struct AllocInfo
        {
            public IMemoryOwner<byte>buffer;
            // public int size;
            // public bool is_returned;
        }
        public class FileReadOnlyManager:ET.Singleton<FileReadOnlyManager>
        {
            private MemoryPool<byte> memoryPool=MemoryPool<byte>.Shared;
            // public override void BeginInit()
            // {
            //     base.BeginInit();

            // }
            
            public async ET.ETTask<AllocInfo> ReadFile(string path)
            {
                var allocInfo=new AllocInfo();
                
                // ET.ETTask task=ET.ETTask.Create();
                var loadingtask=Task.Run(async delegate
                {
                    using (var stream = new FileStream(path, FileMode.Open))
                    {

                        
                        int length=(int)stream.Length;

                        
                        var pipeReader = PipeReader.Create(stream);

                        while (true)
                        {
                            var pipeReadResult = await pipeReader.ReadAsync().ConfigureAwait(false);
                            var buffer = pipeReadResult.Buffer;

                            try
                            {
                                //process data in buffer
                                // Console.WriteLine(buffer.Length.ToString());
                                
                                
                                 ET.Log.Console($"当前线程 {Thread.CurrentThread.ManagedThreadId} 读取长度 {buffer.Length}");
                                
                                if (pipeReadResult.IsCompleted)
                                {
                                    break;
                                }
                            }
                            finally
                            {
                                pipeReader.AdvanceTo(buffer.End);
                            }
                       }
                }
                });

                await loadingtask.ConfigureAwait(true);
                
                // task.SetResult();
                // await task;

                ET.Log.Console($"当前线程 {Thread.CurrentThread.ManagedThreadId} AwaitTask");


                return allocInfo;
            }
            
            
             
            
            protected override void Destroy()
            {
                base.Destroy();
                memoryPool.Dispose();
                memoryPool=null;
            }
        }
        public class TimerInfo
        {
            // public long begin_time;
            public long wait_time;
            public ET.ETTask task;
            public bool IsCanceled=false;
            
        }
        public class TimerManager:ET.Singleton<TimerManager>
        {
            private DateTime dt1970=new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            private DateTime dt=new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            private long lastcurrent_time;
            
            
            private readonly ConcurrentQueue<TimerInfo> storequeue = new();
            public void Init()
            {
                lastcurrent_time=GetCurrentDateTime();
            }
            public void Update()
            {

                long current_time=GetCurrentDateTime();

                int delta_short_time=(int)(current_time-lastcurrent_time);
                int toCheckCount=storequeue.Count;
                int hasCheckedCount=0;

                lastcurrent_time=current_time;
                while(hasCheckedCount<toCheckCount&&!storequeue.IsEmpty)
                {
                    
                    if(storequeue.TryDequeue(out TimerInfo timerinfo))
                    {
                        timerinfo.wait_time-=delta_short_time;
                        ET.Log.Console($"Delta time {delta_short_time}");

                        if(timerinfo.IsCanceled||timerinfo.task.IsCompleted)
                        {
                            // Console.WriteLine($"timerinfo.IsCanceled||timerinfo.task.IsCompleted");
                        }
                        //超时
                        //currenttime-timerinfo.begin_time>timerinfo.wait_time
                        else if(timerinfo.wait_time<0)
                        {
                            // Console.WriteLine($"Set result");
                            timerinfo.task.SetResult();
                            
                        }
                        //继续下一轮
                        else
                        {
                            
                            storequeue.Enqueue(timerinfo);
                        }
                        hasCheckedCount++;
                    };
                }

      
            }

            public long GetCurrentDateTime()
            {
                return (DateTime.UtcNow.Ticks - dt1970.Ticks) / 10000;
            }
     
            public void AddToTimerStore(TimerInfo timerInfo)
            {
                 storequeue.Enqueue(timerInfo);
            }
            public async ET.ETTask Delay(int miliseconds,ET.ETCancellationToken cancellationToken=null)
            {
                
                ET.ETTask task=ET.ETTask.Create();
                TimerInfo timerInfo=new TimerInfo()
                {
                    // begin_time=GetCurrentDateTime(),
                    wait_time=miliseconds,
                    task=task
                };
                AddToTimerStore(timerInfo);
                void CanCelltask()
                {
                    timerInfo.IsCanceled=true;
                }
                try
                {
                    cancellationToken?.Add(CanCelltask);
                    await task;
                }
                finally
                {
                    Console.WriteLine("执行完成");
                    cancellationToken?.Remove(CanCelltask);
                }
                    
            }
        }
    }
}