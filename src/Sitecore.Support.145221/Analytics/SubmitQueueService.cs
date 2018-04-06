using System;
using Sitecore.Analytics.Data.DataAccess.SubmitQueue;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Services;
using Sitecore.XConnect.Operations;
using Sitecore.Xdb.Configuration;

namespace Sitecore.Support.Analytics
{
  public class SubmitQueueService : IDisposable
  {
    private readonly SubmitQueue submitQueue;

    private AlarmClock alarm;

    public int Interval
    {
      get;
      set;
    }

    public SubmitQueueService()
    {
      this.submitQueue = (Factory.CreateObject("submitQueue/queue", true) as SubmitQueue);
    }

    public void Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    public bool Start()
    {
      if (!XdbSettings.Enabled)
      {
        Log.Info("SubmitQueueService was not started as xDB is disabled.", this);
        return false;
      }
      this.alarm = new AlarmClock(TimeSpan.FromSeconds((double)this.Interval));
      this.alarm.Ring += delegate (object o, EventArgs args)
      {
        this.WakeUp();
      };
      return true;
    }

    public void Stop()
    {
      if (this.alarm != null)
      {
        this.alarm.Dispose();
        this.alarm = null;
      }
    }

    public void WakeUp()
    {
      Log.Debug("[Analytics]: SubmitQueueService has woken up");
      if (!XdbSettings.Enabled)
      {
        Log.Info("SubmitQueueService was not processed as xDB is disabled.", this);
        return;
      }
      SubmitQueueEntry submitQueueEntry;
      while ((submitQueueEntry = this.submitQueue.Dequeue()) != null)
      {
        try
        {
          Log.Debug("[Analytics]: Pending item is submitted into live db");
          submitQueueEntry.Submit();
        }
        catch (Exception arg)
        {
          if (arg is AggregateException)
          {
            var exception = arg.InnerException as EntityOperationException;
            if (exception != null && exception.Result == SaveResultStatus.AlreadyExists)
            {
              Log.Debug("[Analytics]: Sitecore.Support.145221 Cannot submit pending item since it already exists. Item is skipped.");
              continue;
            }
          }

          Log.Debug("[Analytics]: Cannot submit pending item: " + arg);
          this.submitQueue.Enqueue(submitQueueEntry);
          break;
        }
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        this.Stop();
      }
    }
  }
}
