using Sitecore.Configuration;
using Sitecore.Events.Hooks;

namespace Sitecore.Support.Analytics
{
  public class BackgroundServiceLoader : IHook
  {
    public void Initialize()
    {
      StartSubmitQueueService();
    }

    private static void StartSubmitQueueService()
    {
      SubmitQueueService submitQueueService = Factory.CreateObject("submitQueue/backgroundService", false) as SubmitQueueService;
      if (submitQueueService != null)
      {
        submitQueueService.Start();
      }
    }
  }
}
