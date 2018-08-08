namespace Sitecore.Support.Analytics.Pipelines.StartTracking
{
  using System;
  using System.Web;

  using Sitecore.Analytics;
  using Sitecore.Analytics.Configuration;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Analytics.Pipelines.StartTracking;
  using Sitecore.Data;
  using Sitecore.Diagnostics;

  /// <summary>Defines the process query string class.</summary>
  public class ProcessQueryStringCampaign : StartTrackingProcessor
  {
    /// <summary>Processes this instance.</summary>
    /// <param name="args">The arguments.</param>
    public override void Process([NotNull] StartTrackingArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");
      Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session is not initialized");
      Assert.IsNotNull(Tracker.Current.Session.Interaction, "Tracker.Current.Session.Interaction is not initialized");
      Assert.IsNotNull(Tracker.Current.Session.Interaction.CurrentPage, "Tracker.Current.Session.Interaction.CurrentPage is not initialized");

      Assert.IsNotNull(args.HttpContext, "The HttpContext is not set.");
      Assert.IsNotNull(args.HttpContext.Request, "The HttpRequest is not set.");

      this.TriggerCampaign(args.HttpContext.Request);
    }

    /// <summary>
    /// Triggers the campaign from the query string.
    /// </summary>
    private void TriggerCampaign([NotNull] HttpRequestBase request)
    {
      Debug.ArgumentNotNull(request, "request");

      string campaignKey = AnalyticsSettings.CampaignQueryStringKey;
      string campaign = GetQueryStringValue(request, campaignKey);

      if (campaign != null)
      {
        campaign = campaign.Trim();

        this.TriggerCampaign(campaign);
      }
    }

    /// <summary>Triggers the campaign event.</summary>
    /// <param name="campaign">The campaign event id.</param>
    /// <exception cref="Exception">Could not find campaign event.</exception>
    private void TriggerCampaign([NotNull] string campaign)
    {
      Debug.ArgumentNotNull(campaign, "campaign");

      CampaignItem campaignItem;

      if (ShortID.IsShortID(campaign))
      {
        ID id = ShortID.DecodeID(campaign);
        campaignItem = Tracker.DefinitionItems.Campaigns[id];
      }
      else
      {
        if (ID.IsID(campaign))
        {
          ID id = new ID(campaign);
          campaignItem = Tracker.DefinitionItems.Campaigns[id];
        }
        else
        {
          campaignItem = Tracker.DefinitionItems.Campaigns[campaign];
        }
      }

      if (campaignItem == null)
      {
        Log.Error("Campaign not found: " + campaign, typeof(ProcessQueryStringCampaign));
        return;
      }

      Guid? prevCampaignID = Tracker.Current.Session.Interaction.CampaignId;
      int prevTraficType = Tracker.Current.Session.Interaction.TrafficType;

      Tracker.Current.CurrentPage.TriggerCampaign(campaignItem);

      Guid? currentCampaignId = Tracker.Current.Session.Interaction.CampaignId;
      int currentTraficType = Tracker.Current.Session.Interaction.TrafficType;

      if (prevCampaignID != Tracker.Current.Session.Interaction.CampaignId)
      {
        TrackerEvents.OnCurrentPageCancelled += (sender, args) => Tracker.Current.Session.Interaction.CampaignId = currentCampaignId;
      }

      if (prevTraficType != currentTraficType)
      {
        TrackerEvents.OnCurrentPageCancelled += (sender, args) => Tracker.Current.Session.Interaction.TrafficType = currentTraficType;
      }
    }
  }
}